using BlazorCanvas.Components.Pages;
using BlazorCanvas.Data.V11;
using BlazorCanvas.Geometry;
using BlazorCanvas.Shapes;
using BlazorCanvas.Sync;
using Npgsql;
using System.Text.Json;

namespace BlazorCanvas.Tests.Database.V11;

/// <summary>
/// End-to-end persistence proof for the final public schema. Each circuit owns its coordinator
/// and repository callbacks; only the notifier is shared, as it is in the server DI container.
/// </summary>
[Collection("Database")]
public class FinalPublicCanvasSyncIntegrationTests
{
    private readonly DatabaseFixture fixture;

    public FinalPublicCanvasSyncIntegrationTests(DatabaseFixture fixture) => this.fixture = fixture;

    [Fact]
    public async Task FinalPublicRows_PersistAndRelayCanonicalDrawMoveDeleteWithoutDuplicateInsertion()
    {
        await using var harness = await CreateHarnessAsync();
        var messages = new List<SyncMessage>();
        using var observer = harness.Notifier.Subscribe(harness.OwnerId, messages.Add);

        await harness.A.Coordinator.DrawAsync("rectangle", new CanvasPoint(10, 20), new CanvasPoint(50, 60));
        var drawn = Assert.Single(harness.A.Coordinator.Figures);
        Assert.Equal(drawn, Assert.Single(harness.B.Coordinator.Figures));
        Assert.Equal(drawn, Assert.Single(await harness.A.Repository.LoadAsync(harness.CanvasId)));
        var draw = Assert.Single(messages, message => message.Kind == "draw");
        Assert.NotNull(draw.Figure);
        Assert.Null(draw.X);
        Assert.Null(draw.Y);

        // A replay from another circuit must not make a second row either in state or persistence.
        harness.Notifier.Publish(harness.OwnerId, SyncMessage.Draw(drawn, Guid.NewGuid()));
        Assert.Single(harness.B.Coordinator.Figures);
        Assert.Single(await harness.B.Repository.LoadAsync(harness.CanvasId));

        harness.A.Coordinator.BeginDrag(drawn.Id, new CanvasPoint(10, 20));
        harness.A.Coordinator.ContinueDrag(new CanvasPoint(25, 32));
        await harness.A.Coordinator.CommitDragAsync();

        var moved = Assert.Single(harness.A.Coordinator.Figures);
        Assert.Equal(moved, Assert.Single(harness.B.Coordinator.Figures));
        Assert.Equal(moved, Assert.Single(await harness.A.Repository.LoadAsync(harness.CanvasId)));

        var unknown = Guid.NewGuid();
        harness.Notifier.Publish(harness.OwnerId, SyncMessage.Move(unknown, 99m, 99m, Guid.NewGuid()));
        harness.Notifier.Publish(harness.OwnerId, SyncMessage.Rollback(unknown, 1m, 1m, Guid.NewGuid()));
        Assert.DoesNotContain(harness.A.Coordinator.Figures, row => row.Id == unknown);
        Assert.DoesNotContain(harness.B.Coordinator.Figures, row => row.Id == unknown);
        Assert.DoesNotContain(await harness.A.Repository.LoadAsync(harness.CanvasId), row => row.Id == unknown);

        await harness.A.Coordinator.DeleteAsync();
        Assert.Empty(harness.A.Coordinator.Figures);
        Assert.Empty(harness.B.Coordinator.Figures);
        Assert.Empty(await harness.A.Repository.LoadAsync(harness.CanvasId));

        Assert.All(messages.Where(message => message.Kind is "move" or "delete" or "rollback"), message => Assert.Null(message.Figure));
        Assert.Contains(messages, message => message.Kind == "move" && message.Id == drawn.Id && message.X == moved.X && message.Y == moved.Y);
        Assert.Contains(messages, message => message.Kind == "delete" && message.Id == drawn.Id && message.X is null && message.Y is null);
    }

    [Fact]
    public async Task ThrottledMoves_PersistOnlyTrailingCoordinateThroughFinalPublicRepository()
    {
        var clock = 1L;
        await using var harness = await CreateHarnessAsync(clock: () => clock);
        var messages = new List<SyncMessage>();
        using var observer = harness.Notifier.Subscribe(harness.OwnerId, messages.Add);

        await harness.A.Coordinator.DrawAsync("rectangle", new CanvasPoint(0, 0), new CanvasPoint(30, 30));
        var row = Assert.Single(harness.A.Coordinator.Figures);
        messages.Clear();

        harness.A.Coordinator.BeginDrag(row.Id, new CanvasPoint(0, 0));
        harness.A.Coordinator.ContinueDrag(new CanvasPoint(4, 0));
        clock = 20;
        harness.A.Coordinator.ContinueDrag(new CanvasPoint(8, 0));
        clock = 40;
        harness.A.Coordinator.ContinueDrag(new CanvasPoint(12, 0));
        await harness.A.Coordinator.CommitDragAsync();

        var moves = messages.Where(message => message.Kind == "move").ToList();
        Assert.Equal(2, moves.Count); // first throttle tick plus mandatory trailing edge
        Assert.Equal(12m, moves[^1].X);
        var persisted = Assert.Single(await harness.A.Repository.LoadAsync(harness.CanvasId));
        Assert.Equal(12m, persisted.X);
        Assert.Equal(persisted, Assert.Single(harness.B.Coordinator.Figures));
    }

    [Fact]
    public async Task ZeroRowMove_RemovesStaleFigureForEveryCircuitWithoutResurrection()
    {
        await using var harness = await CreateHarnessAsync();
        await harness.A.Coordinator.DrawAsync("rectangle", new CanvasPoint(0, 0), new CanvasPoint(30, 30));
        var row = Assert.Single(harness.A.Coordinator.Figures);

        // This bypasses B's coordinator/notifier on purpose. A now holds a genuinely stale row.
        Assert.Equal(1, await harness.B.Repository.DeleteAsync(harness.CanvasId, row.Id));
        Assert.Single(harness.A.Coordinator.Figures);
        Assert.Single(harness.B.Coordinator.Figures);

        harness.A.Coordinator.BeginDrag(row.Id, new CanvasPoint(0, 0));
        harness.A.Coordinator.ContinueDrag(new CanvasPoint(10, 0));
        await harness.A.Coordinator.CommitDragAsync();

        Assert.Empty(harness.A.Coordinator.Figures);
        Assert.Empty(harness.B.Coordinator.Figures); // delete publication clears B's stale local copy
        Assert.Null(harness.A.Coordinator.SelectedId);
        Assert.Empty(await harness.A.Repository.LoadAsync(harness.CanvasId));
    }

    [Fact]
    public async Task Star5Draw_PersistsImmediatelyRelaysCommittedDrawOnlyAndReloadsUnchanged()
    {
        await using var harness = await CreateHarnessAsync();
        var messages = new List<SyncMessage>();
        using var observer = harness.Notifier.Subscribe(harness.OwnerId, messages.Add);

        await harness.A.Coordinator.DrawAsync("star5", new CanvasPoint(12, 18), new CanvasPoint(92, 138));

        var drawn = Assert.Single(harness.A.Coordinator.Figures);
        Assert.Equal(drawn, Assert.Single(harness.B.Coordinator.Figures));
        Assert.Equal(drawn, Assert.Single(await harness.A.Repository.LoadAsync(harness.CanvasId)));
        var independentLoad = await new FigureRepository(fixture.DataSource).LoadAsync(harness.CanvasId);
        var reloaded = Assert.Single(independentLoad);
        Assert.Equal(drawn, reloaded);

        Assert.Equal("star5", drawn.Type);
        using var document = JsonDocument.Parse(drawn.GeometryJson);
        var root = document.RootElement;
        Assert.Equal(10, root.GetProperty("points").GetArrayLength());
        Assert.Equal(Star5Shape.DefaultInnerRatio, root.GetProperty("innerRatio").GetDouble());

        var shape = new Star5Shape();
        Assert.True(shape.TryParseGeometry(root, out var parsed));
        var bounds = shape.BoundsOf(parsed);
        Assert.Equal(bounds.X, drawn.BboxX);
        Assert.Equal(bounds.Y, drawn.BboxY);
        Assert.Equal(bounds.W, drawn.BboxW);
        Assert.Equal(bounds.H, drawn.BboxH);
        Assert.Equal(1, drawn.Z);

        var draw = Assert.Single(messages, message => message.Kind == "draw");
        Assert.Equal(drawn, draw.Figure);
        Assert.Null(draw.X);
        Assert.Null(draw.Y);
        Assert.DoesNotContain(messages, message => message.Kind == "preview");
    }

    [Fact]
    public async Task Star5FinalPublicRows_PersistAndRelayCanonicalDrawGlideDeleteWithoutDuplicateOrUnknownInsertion()
    {
        var clock = 1L;
        await using var harness = await CreateHarnessAsync(clock: () => clock);
        var messages = new List<SyncMessage>();
        using var observer = harness.Notifier.Subscribe(harness.OwnerId, messages.Add);

        await harness.A.Coordinator.DrawAsync("star5", new CanvasPoint(100, 100), new CanvasPoint(160, 160));
        var drawn = Assert.Single(harness.A.Coordinator.Figures);
        Assert.Equal("star5", drawn.Type);
        Assert.Equal(drawn, Assert.Single(harness.B.Coordinator.Figures));
        Assert.Equal(drawn, Assert.Single(await harness.A.Repository.LoadAsync(harness.CanvasId)));
        var draw = Assert.Single(messages, message => message.Kind == "draw");
        Assert.Equal(drawn, draw.Figure);
        Assert.Null(draw.X);
        Assert.Null(draw.Y);

        // A replay from another circuit must not fork the state or public.figures.
        harness.Notifier.Publish(harness.OwnerId, SyncMessage.Draw(drawn, Guid.NewGuid()));
        Assert.Single(harness.B.Coordinator.Figures);
        Assert.Single(await harness.B.Repository.LoadAsync(harness.CanvasId));

        messages.Clear();
        harness.A.Coordinator.BeginDrag(drawn.Id, new CanvasPoint(100, 100));
        harness.A.Coordinator.ContinueDrag(new CanvasPoint(110, 104));
        clock = 60;
        harness.A.Coordinator.ContinueDrag(new CanvasPoint(126, 113));
        clock = 80;
        harness.A.Coordinator.ContinueDrag(new CanvasPoint(130, 120));
        await harness.A.Coordinator.CommitDragAsync();

        var moved = Assert.Single(harness.A.Coordinator.Figures);
        Assert.Equal(130m, moved.X);
        Assert.Equal(120m, moved.Y);
        Assert.Equal(moved, Assert.Single(harness.B.Coordinator.Figures));
        Assert.Equal(moved, Assert.Single(await harness.A.Repository.LoadAsync(harness.CanvasId)));

        var moves = messages.Where(message => message.Kind == "move").ToList();
        Assert.Equal(3, moves.Count);
        Assert.All(moves, message => Assert.Null(message.Figure));
        Assert.Equal(drawn.Id, moves[^1].Id);
        Assert.Equal(moved.X, moves[^1].X);
        Assert.Equal(moved.Y, moves[^1].Y);

        var unknown = Guid.NewGuid();
        harness.Notifier.Publish(harness.OwnerId, SyncMessage.Move(unknown, 99m, 99m, Guid.NewGuid()));
        harness.Notifier.Publish(harness.OwnerId, SyncMessage.Rollback(unknown, 1m, 1m, Guid.NewGuid()));
        Assert.DoesNotContain(harness.A.Coordinator.Figures, row => row.Id == unknown);
        Assert.DoesNotContain(harness.B.Coordinator.Figures, row => row.Id == unknown);
        Assert.DoesNotContain(await harness.A.Repository.LoadAsync(harness.CanvasId), row => row.Id == unknown);

        await harness.A.Coordinator.DeleteAsync();
        Assert.Empty(harness.A.Coordinator.Figures);
        Assert.Empty(harness.B.Coordinator.Figures);
        Assert.Empty(await harness.A.Repository.LoadAsync(harness.CanvasId));

        Assert.All(messages.Where(message => message.Kind is "move" or "delete" or "rollback"), message => Assert.Null(message.Figure));
        Assert.Contains(messages, message => message.Kind == "delete" && message.Id == drawn.Id && message.X is null && message.Y is null);
        Assert.DoesNotContain(messages, message => message.Kind == "preview");
    }

    [Fact]
    public async Task Star5ZeroRowMove_RemovesStaleFigureForEveryCircuitWithoutResurrection()
    {
        await using var harness = await CreateHarnessAsync();
        await harness.A.Coordinator.DrawAsync("star5", new CanvasPoint(0, 0), new CanvasPoint(30, 30));
        var row = Assert.Single(harness.A.Coordinator.Figures);

        // This bypasses B's coordinator/notifier on purpose. A now holds a genuinely stale row.
        Assert.Equal(1, await harness.B.Repository.DeleteAsync(harness.CanvasId, row.Id));
        Assert.Single(harness.A.Coordinator.Figures);
        Assert.Single(harness.B.Coordinator.Figures);

        harness.A.Coordinator.BeginDrag(row.Id, new CanvasPoint(0, 0));
        harness.A.Coordinator.ContinueDrag(new CanvasPoint(10, 0));
        await harness.A.Coordinator.CommitDragAsync();

        Assert.Empty(harness.A.Coordinator.Figures);
        Assert.Empty(harness.B.Coordinator.Figures); // delete publication clears B's stale local copy
        Assert.Null(harness.A.Coordinator.SelectedId);
        Assert.Empty(await harness.A.Repository.LoadAsync(harness.CanvasId));
    }

    [Fact]
    public async Task Star5PersistedSelectEdgeClampedDragAndDelete_RoundTripsThroughFinalPublicRepository()
    {
        await using var harness = await CreateHarnessAsync();
        var seed = await InsertStarAsync(harness.A.Repository, harness.CanvasId, 10, 12, 50, 52);
        await harness.A.Coordinator.LoadAsync();
        await harness.B.Coordinator.LoadAsync();

        harness.A.Coordinator.BeginDrag(seed.Id, new CanvasPoint(10, 12));
        harness.A.Coordinator.ContinueDrag(new CanvasPoint(2000, 1000));
        await harness.A.Coordinator.CommitDragAsync();

        var moved = Assert.Single(harness.A.Coordinator.Figures);
        Assert.Equal(seed.Id, moved.Id);
        Assert.Equal(CanvasBounds.Width - (decimal)seed.BboxW, moved.X);
        Assert.Equal(CanvasBounds.Height - (decimal)seed.BboxH, moved.Y);
        Assert.Equal(moved, Assert.Single(harness.B.Coordinator.Figures));

        var repositoryReload = await new FigureRepository(fixture.DataSource).LoadAsync(harness.CanvasId);
        Assert.Equal(moved, Assert.Single(repositoryReload));

        await harness.A.Coordinator.DeleteAsync();
        Assert.Empty(harness.A.Coordinator.Figures);
        Assert.Empty(harness.B.Coordinator.Figures);
        Assert.Empty(await harness.A.Repository.LoadAsync(harness.CanvasId));
    }

    [Fact]
    public async Task FailedMove_RollsBackPeerThenReloadsBothCircuitsToAuthoritativePublicSnapshot()
    {
        await using var harness = await CreateHarnessAsync(failAMove: true);
        var messages = new List<SyncMessage>();
        using var observer = harness.Notifier.Subscribe(harness.OwnerId, messages.Add);

        // Seed through the real repository, then load each independently constructed circuit.
        var seed = await InsertRectangleAsync(harness.A.Repository, harness.CanvasId, 5, 7);
        await harness.A.Coordinator.LoadAsync();
        await harness.B.Coordinator.LoadAsync();

        harness.A.Coordinator.BeginDrag(seed.Id, new CanvasPoint(5, 7));
        harness.A.Coordinator.ContinueDrag(new CanvasPoint(20, 7));
        await harness.A.Coordinator.CommitDragAsync();

        Assert.True(harness.A.Coordinator.ShowSaveFailedModal);
        Assert.Equal(seed, Assert.Single(harness.A.Coordinator.Figures));
        Assert.Equal(seed, Assert.Single(harness.B.Coordinator.Figures));
        var rollback = Assert.Single(messages, message => message.Kind == "rollback");
        Assert.Null(rollback.Figure);
        Assert.Equal(seed.Id, rollback.Id);
        Assert.Equal(seed.X, rollback.X);
        Assert.Equal(seed.Y, rollback.Y);

        await harness.A.Coordinator.ReloadAsync();
        await harness.B.Coordinator.ReloadAsync();
        var authoritative = await harness.A.Repository.LoadAsync(harness.CanvasId);
        Assert.Equal(authoritative, harness.A.Coordinator.Figures);
        Assert.Equal(authoritative, harness.B.Coordinator.Figures);
        Assert.False(harness.A.Coordinator.ShowSaveFailedModal);
        Assert.Equal(seed.X, Assert.Single(authoritative).X);
    }

    private async Task<SyncHarness> CreateHarnessAsync(Func<long>? clock = null, bool failAMove = false)
    {
        int ownerId;
        await using (var context = fixture.CreateContext())
        {
            ownerId = await DatabaseFixture.CreateTestUserAsync(context);
        }

        var canvas = await new CanvasRepository(fixture.DataSource).EnsureForOwnerAsync(ownerId);
        var notifier = new CanvasSyncNotifier();
        var aRepository = new FigureRepository(fixture.DataSource);
        var bRepository = new FigureRepository(fixture.DataSource);
        var gateway = new FigureInputGateway(DefaultShapes.CreateRegistry());
        var a = CreateCircuit(notifier, gateway, ownerId, canvas.Id, aRepository, clock,
            failAMove ? (_, _, _, _) => throw new InvalidOperationException("Injected persisted save failure.") : null);
        var b = CreateCircuit(notifier, gateway, ownerId, canvas.Id, bRepository, clock);
        await a.Coordinator.LoadAsync();
        await b.Coordinator.LoadAsync();
        return new SyncHarness(ownerId, canvas.Id, notifier, a, b);
    }

    private static Circuit CreateCircuit(
        CanvasSyncNotifier notifier,
        FigureInputGateway gateway,
        int ownerId,
        Guid canvasId,
        FigureRepository repository,
        Func<long>? clock,
        Func<Guid, decimal, decimal, CancellationToken, Task<int>>? move = null)
    {
        var coordinator = new CanvasInteractionCoordinator(
            gateway,
            notifier,
            ownerId,
            canvasId,
            ct => repository.LoadAsync(canvasId, ct),
            (input, x, y, ct) => repository.InsertAsync(canvasId, input, x, y, ct: ct),
            move ?? ((id, x, y, ct) => repository.MoveAsync(canvasId, id, x, y, ct)),
            (id, ct) => repository.DeleteAsync(canvasId, id, ct),
            clock);
        return new Circuit(coordinator, repository, notifier.Subscribe(ownerId, coordinator.ApplyRemoteMessage));
    }

    private static async Task<FigureRow> InsertRectangleAsync(FigureRepository repository, Guid canvasId, double x, double y)
    {
        var gateway = new FigureInputGateway(DefaultShapes.CreateRegistry());
        Assert.True(gateway.TryValidateGesture("rectangle", new CanvasPoint(x, y), new CanvasPoint(x + 30, y + 30), null, out var input, out var px, out var py));
        return await repository.InsertAsync(canvasId, input!, (decimal)px, (decimal)py);
    }

    private static async Task<FigureRow> InsertStarAsync(FigureRepository repository, Guid canvasId, double x, double y, double w, double h)
    {
        var gateway = new FigureInputGateway(DefaultShapes.CreateRegistry());
        Assert.True(gateway.TryValidateGesture("star5", new CanvasPoint(x, y), new CanvasPoint(x + w, y + h), null, out var input, out var px, out var py));
        return await repository.InsertAsync(canvasId, input!, (decimal)px, (decimal)py);
    }

    private sealed record Circuit(CanvasInteractionCoordinator Coordinator, FigureRepository Repository, IDisposable Subscription) : IDisposable
    {
        public void Dispose() => Subscription.Dispose();
    }

    private sealed record SyncHarness(int OwnerId, Guid CanvasId, CanvasSyncNotifier Notifier, Circuit A, Circuit B) : IAsyncDisposable
    {
        public ValueTask DisposeAsync()
        {
            A.Dispose();
            B.Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
