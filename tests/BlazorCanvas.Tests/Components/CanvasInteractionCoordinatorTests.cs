using BlazorCanvas.Components.Pages;
using BlazorCanvas.Data.V11;
using BlazorCanvas.Shapes;
using BlazorCanvas.Sync;
using System.Text.RegularExpressions;

namespace BlazorCanvas.Tests.Components;

public class CanvasInteractionCoordinatorTests
{
    [Fact]
    public async Task CanonicalDrawAndUpdateOnlyProtocol_ConvergeTwoCircuits()
    {
        var notifier = new CanvasSyncNotifier();
        var rows = new List<FigureRow>();
        var a = Create(notifier, rows, out var aSubscription);
        var b = Create(notifier, rows, out var bSubscription);
        using (aSubscription) using (bSubscription)
        {
            await a.DrawAsync("rectangle", new CanvasPoint(10, 20), new CanvasPoint(30, 40));
            var first = Assert.Single(a.Figures);
            var received = Assert.Single(b.Figures);
            Assert.Equal(first, received);

            notifier.Publish(7, SyncMessage.Draw(first, Guid.NewGuid()));
            notifier.Publish(7, SyncMessage.Move(Guid.NewGuid(), 99m, 99m, Guid.NewGuid()));
            notifier.Publish(7, SyncMessage.Rollback(Guid.NewGuid(), 1m, 1m, Guid.NewGuid()));

            Assert.Single(b.Figures);
            Assert.Equal(first.Id, b.Figures[0].Id);
        }
    }

    [Fact]
    public async Task Draw_PublishesCreateWithoutPositionMessage()
    {
        var notifier = new CanvasSyncNotifier();
        var rows = new List<FigureRow>();
        var publications = new List<SyncMessage>();
        using var observer = notifier.Subscribe(7, publications.Add);
        var coordinator = Create(notifier, rows, out var subscription);
        using (subscription)
        {
            await coordinator.DrawAsync("rectangle", new CanvasPoint(10, 20), new CanvasPoint(30, 40));
        }

        Assert.Single(publications);
        Assert.Equal("draw", publications.Single().Kind);
        Assert.DoesNotContain(publications, message => message.Kind == "move");
    }

    [Fact]
    public async Task Drag_ThrottlesThenTrailingEdgePrecedesSinglePersistence()
    {
        var notifier = new CanvasSyncNotifier();
        var rows = new List<FigureRow> { Row() };
        var clock = 1L;
        var moveCalls = 0;
        var publications = new List<SyncMessage>();
        using var observer = notifier.Subscribe(7, publications.Add);
        var coordinator = Create(notifier, rows, out var subscription, () => clock, (_, x, y, _) => { moveCalls++; return Task.FromResult(1); });
        using (subscription)
        {
            coordinator.BeginDrag(rows[0].Id, new CanvasPoint(0, 0));
            coordinator.ContinueDrag(new CanvasPoint(4, 0));
            coordinator.ContinueDrag(new CanvasPoint(8, 0));
            await coordinator.CommitDragAsync();
        }

        Assert.Equal(1, moveCalls);
        var moves = publications.Where(message => message.Kind == "move").ToList();
        Assert.Equal(2, moves.Count); // initial throttle publication plus forced trailing edge
        Assert.Equal(8m, moves.Last().X);
    }

    [Fact]
    public async Task RemoteMessagesAreDiscardedDuringDrag_AndFailureRollsPeersBack()
    {
        var notifier = new CanvasSyncNotifier();
        var row = Row();
        var rows = new List<FigureRow> { row };
        var a = Create(notifier, rows, out var aSubscription, move: (_, _, _, _) => throw new InvalidOperationException());
        var b = Create(notifier, rows, out var bSubscription);
        using (aSubscription) using (bSubscription)
        {
            b.BeginDrag(row.Id, new CanvasPoint(0, 0));
            notifier.Publish(7, SyncMessage.Delete(row.Id, Guid.NewGuid()));
            Assert.Single(b.Figures); // D-54 discard precedes every kind
            await b.CommitDragAsync();
            notifier.Publish(7, SyncMessage.Draw(row, Guid.NewGuid())); // restore A's stale row for failure exercise

            a.BeginDrag(row.Id, new CanvasPoint(0, 0));
            a.ContinueDrag(new CanvasPoint(10, 0));
            await a.CommitDragAsync();

            Assert.True(a.ShowSaveFailedModal);
            Assert.Equal(row.X, a.Figures.Single().X);
            Assert.Equal(row.X, b.Figures.Single().X);
        }
    }

    [Theory]
    [InlineData("draw")]
    [InlineData("move")]
    [InlineData("delete")]
    [InlineData("rollback")]
    public async Task ReceiptDuringDrag_IsNeverAuthorizedEvenWhenCommitPrecedesDeferredCallback(string kind)
    {
        var notifier = new CanvasSyncNotifier();
        var row = Row();
        var rows = new List<FigureRow> { row };
        var coordinator = Create(notifier, rows, out var subscription);
        using (subscription)
        {
            coordinator.BeginDrag(row.Id, new CanvasPoint(0, 0));
            var message = kind switch
            {
                "draw" => SyncMessage.Draw(Row(), Guid.NewGuid()),
                "move" => SyncMessage.Move(row.Id, 99m, 99m, Guid.NewGuid()),
                "delete" => SyncMessage.Delete(row.Id, Guid.NewGuid()),
                _ => SyncMessage.Rollback(row.Id, 99m, 99m, Guid.NewGuid())
            };

            var deferred = coordinator.TryAuthorizeRemoteDelivery(message);
            Assert.Null(deferred);

            await coordinator.CommitDragAsync(); // models pointer-up before a queued InvokeAsync callback drains
            Assert.Single(coordinator.Figures);
            Assert.Equal(row, coordinator.Figures.Single());
            Assert.Equal(row.Id, coordinator.SelectedId);
        }
    }

    [Fact]
    public void AuthorizedQueuedDelivery_AppliesTheReceivedMessageOnce()
    {
        var notifier = new CanvasSyncNotifier();
        var rows = new List<FigureRow>();
        var coordinator = Create(notifier, rows, out var subscription);
        using (subscription)
        {
            var remote = Row();
            var delivery = coordinator.TryAuthorizeRemoteDelivery(SyncMessage.Draw(remote, Guid.NewGuid()));

            Assert.NotNull(delivery);
            coordinator.ApplyAuthorizedRemoteDelivery(delivery!);

            Assert.Equal(remote, Assert.Single(coordinator.Figures));
        }
    }

    [Fact]
    public void HomeAuthorizesRemoteDeliveryBeforeQueuingInvokeAsync()
    {
        var home = FindFromRepositoryRoot("src", "BlazorCanvas", "Components", "Pages", "Home.razor");
        var source = File.ReadAllText(home);
        var method = Regex.Match(source, @"private void HandleRemoteMessage\(SyncMessage message\)(?<body>.*?)\n    }\n\n    private static", RegexOptions.Singleline).Groups["body"].Value;

        Assert.NotEmpty(method);
        var authorization = method.IndexOf("TryAuthorizeRemoteDelivery", StringComparison.Ordinal);
        var invokeAsync = method.IndexOf("InvokeAsync", StringComparison.Ordinal);
        var apply = method.IndexOf("ApplyAuthorizedRemoteDelivery", StringComparison.Ordinal);
        Assert.True(authorization >= 0 && authorization < invokeAsync, "Home must authorize D-54 receipt before InvokeAsync queues UI work.");
        Assert.True(apply > invokeAsync, "The queued callback must apply only the pre-authorized delivery token.");
        Assert.DoesNotContain("ApplyRemoteMessage(message)", method, StringComparison.Ordinal);
    }

    [Fact]
    public void HomeDrawingPreview_IsCircuitLocalAndUsesCompletedGestureForCommit()
    {
        var home = FindFromRepositoryRoot("src", "BlazorCanvas", "Components", "Pages", "Home.razor");
        var source = File.ReadAllText(home);
        var commit = Regex.Match(source, @"private async Task CommitDrawAsync\(\)(?<body>.*?)\n    }\n\n    private Task HandleDeleteAsync", RegexOptions.Singleline).Groups["body"].Value;

        Assert.Contains("DrawingPreviewSession", source, StringComparison.Ordinal);
        Assert.Contains("PreviewPlacement=\"preview.Placement\" PreviewType=\"preview.Type\"", source, StringComparison.Ordinal);
        Assert.Contains("preview.Begin", source, StringComparison.Ordinal);
        Assert.Contains("preview.Update", source, StringComparison.Ordinal);
        Assert.Contains("await InvokeAsync(StateHasChanged)", source, StringComparison.Ordinal);
        Assert.NotEmpty(commit);
        Assert.Contains("preview?.Complete", commit, StringComparison.Ordinal);
        Assert.Contains("coordinator.DrawAsync(completed.Type, completed.Press, completed.Cursor)", commit, StringComparison.Ordinal);
        Assert.DoesNotContain("Notifier.Publish", source, StringComparison.Ordinal);
    }

    private static CanvasInteractionCoordinator Create(
        CanvasSyncNotifier notifier,
        List<FigureRow> rows,
        out IDisposable subscription,
        Func<long>? clock = null,
        Func<Guid, decimal, decimal, CancellationToken, Task<int>>? move = null)
    {
        var registry = DefaultShapes.CreateRegistry();
        var gateway = new FigureInputGateway(registry);
        var coordinator = new CanvasInteractionCoordinator(
            gateway, notifier, 7, Guid.NewGuid(),
            _ => Task.FromResult<IReadOnlyList<FigureRow>>(rows.ToList()),
            (input, x, y, _) =>
            {
                var row = new FigureRow(Guid.NewGuid(), Guid.NewGuid(), input.Type, x, y, 0, input.GeometryJson, input.StyleJson, rows.Count + 1, input.Bounds.X, input.Bounds.Y, input.Bounds.W, input.Bounds.H);
                rows.Add(row);
                return Task.FromResult(row);
            },
            move ?? ((id, x, y, _) => Task.FromResult(1)),
            (id, _) => { rows.RemoveAll(row => row.Id == id); return Task.FromResult(1); },
            clock);
        coordinator.LoadAsync().GetAwaiter().GetResult();
        subscription = notifier.Subscribe(7, coordinator.ApplyRemoteMessage);
        return coordinator;
    }

    private static FigureRow Row() => new(Guid.NewGuid(), Guid.NewGuid(), "rectangle", 0, 0, 0,
        "{\"w\":20,\"h\":10}", "{}", 1, 0, 0, 20, 10);

    private static string FindFromRepositoryRoot(params string[] parts)
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var candidate = Path.Combine([directory.FullName, .. parts]);
            if (File.Exists(candidate)) return candidate;
        }

        throw new FileNotFoundException("Could not locate the repository Home.razor source contract.");
    }
}
