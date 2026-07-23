using BlazorCanvas.Components.Pages;
using BlazorCanvas.Data.V11;
using BlazorCanvas.Geometry;
using BlazorCanvas.Shapes;
using BlazorCanvas.Sync;
using BlazorCanvas.Tools;
using System.Text.Json;
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
        var previewScript = FindFromRepositoryRoot("src", "BlazorCanvas", "Components", "Pages", "Home.razor.js");
        var source = File.ReadAllText(home);
        var script = File.ReadAllText(previewScript);
        var commit = Regex.Match(source, @"private async Task CommitDrawAsync\(\)(?<body>.*?)\n    }\n\n    private Task HandleDeleteAsync", RegexOptions.Singleline).Groups["body"].Value;

        Assert.Contains("DrawingPreviewSession", source, StringComparison.Ordinal);
        Assert.Contains("data-preview-tool", source, StringComparison.Ordinal);
        Assert.Contains("Home.razor.js", source, StringComparison.Ordinal);
        Assert.Contains("preview.Begin", source, StringComparison.Ordinal);
        Assert.Contains("preview.Update", source, StringComparison.Ordinal);
        Assert.Contains("PreviewPlacement=\"preview.Placement\"", source, StringComparison.Ordinal);
        Assert.Contains("PreviewType=\"@preview.Type\"", source, StringComparison.Ordinal);
        Assert.DoesNotContain("PreviewType=\"preview.Type\"", source, StringComparison.Ordinal);
        Assert.Contains("<FigureShape", source, StringComparison.Ordinal);
        Assert.DoesNotContain("document.createElementNS", script, StringComparison.Ordinal);
        Assert.DoesNotContain("setAttribute(\"points\"", script, StringComparison.Ordinal);
        Assert.Contains("setPointerCapture", script, StringComparison.Ordinal);
        Assert.Contains("removePreview", script, StringComparison.Ordinal);
        Assert.NotEmpty(commit);
        Assert.Contains("preview?.Complete", commit, StringComparison.Ordinal);
        Assert.Contains("coordinator.DrawAsync(completed.Type, completed.Press, completed.Cursor)", commit, StringComparison.Ordinal);
        Assert.DoesNotContain("Notifier.Publish", source, StringComparison.Ordinal);
    }

    [Fact]
    public async Task StarToolDraw_D70D71D29D36_CommitsSelectedStar5ThroughRegistryGatewayAndClampsToCanvas()
    {
        var notifier = new CanvasSyncNotifier();
        var rows = new List<FigureRow>();
        var publications = new List<SyncMessage>();
        using var observer = notifier.Subscribe(7, publications.Add);
        var coordinator = Create(notifier, rows, out var subscription);
        using (subscription)
        {
            var starType = ToolMap.ToShapeName(Tool.Star);

            await coordinator.DrawAsync(starType!, new CanvasPoint(-10, -20), new CanvasPoint(CanvasBounds.Width + 100, CanvasBounds.Height + 100));
        }

        var row = Assert.Single(coordinator.Figures);
        Assert.Equal("star5", row.Type);
        Assert.Equal(row.Id, coordinator.SelectedId);
        Assert.Equal(0m, row.X);
        Assert.Equal(0m, row.Y);
        Assert.Equal(0, row.BboxX);
        Assert.Equal(0, row.BboxY);
        Assert.Equal(CanvasBounds.Width, row.BboxW);
        Assert.Equal(CanvasBounds.Height, row.BboxH);

        using var geometry = JsonDocument.Parse(row.GeometryJson);
        var root = geometry.RootElement;
        Assert.Equal(10, root.GetProperty("points").GetArrayLength());
        Assert.Equal(Star5Shape.DefaultInnerRatio, root.GetProperty("innerRatio").GetDouble());

        var published = Assert.Single(publications, message => message.Kind == "draw");
        Assert.Equal(row, published.Figure);
        Assert.DoesNotContain(publications, message => message.Kind == "move");
    }

    [Theory]
    [InlineData(20, 20, 20, 40)]
    [InlineData(20, 20, 40, 20)]
    public async Task StarDraw_D57D67_RejectsZeroExtentSilentlyWithoutRowsOrPublications(double pressX, double pressY, double cursorX, double cursorY)
    {
        var notifier = new CanvasSyncNotifier();
        var rows = new List<FigureRow>();
        var publications = new List<SyncMessage>();
        using var observer = notifier.Subscribe(7, publications.Add);
        var coordinator = Create(notifier, rows, out var subscription);
        using (subscription)
        {
            await coordinator.DrawAsync("star5", new CanvasPoint(pressX, pressY), new CanvasPoint(cursorX, cursorY));
        }

        Assert.Empty(rows);
        Assert.Empty(coordinator.Figures);
        Assert.Empty(publications);
        Assert.Null(coordinator.SelectedId);
    }

    [Fact]
    public async Task StarDraw_D32_AcceptsPositiveOneCanvasUnitSliver()
    {
        var notifier = new CanvasSyncNotifier();
        var rows = new List<FigureRow>();
        var publications = new List<SyncMessage>();
        using var observer = notifier.Subscribe(7, publications.Add);
        var coordinator = Create(notifier, rows, out var subscription);
        using (subscription)
        {
            await coordinator.DrawAsync("star5", new CanvasPoint(10, 10), new CanvasPoint(11, 11));
        }

        var row = Assert.Single(coordinator.Figures);
        Assert.Equal("star5", row.Type);
        Assert.Equal(row.Id, coordinator.SelectedId);
        Assert.Equal(1, row.BboxW);
        Assert.Equal(1, row.BboxH);
        Assert.Single(publications, message => message.Kind == "draw");
    }

    [Fact]
    public async Task Star5_D31D48_BeginDragSelectsPersistedStarAndClickWritesNothing()
    {
        var notifier = new CanvasSyncNotifier();
        var star = StarRow();
        var rows = new List<FigureRow> { star };
        var moveCalls = 0;
        var publications = new List<SyncMessage>();
        using var observer = notifier.Subscribe(7, publications.Add);
        var coordinator = Create(notifier, rows, out var subscription, move: (_, _, _, _) =>
        {
            moveCalls++;
            return Task.FromResult(1);
        });
        using (subscription)
        {
            coordinator.BeginDrag(star.Id, new CanvasPoint(10, 10));
            await coordinator.CommitDragAsync();
        }

        Assert.Equal(star.Id, coordinator.SelectedId);
        Assert.Equal(0, moveCalls);
        Assert.DoesNotContain(publications, message => message.Kind is "move" or "delete");
    }

    [Fact]
    public void Star5_D48_ReSelectingSamePersistedStarIsIdempotent()
    {
        var notifier = new CanvasSyncNotifier();
        var star = StarRow();
        var rows = new List<FigureRow> { star };
        var publications = new List<SyncMessage>();
        using var observer = notifier.Subscribe(7, publications.Add);
        var coordinator = Create(notifier, rows, out var subscription);
        using (subscription)
        {
            coordinator.BeginDrag(star.Id, new CanvasPoint(10, 10));
            coordinator.BeginDrag(star.Id, new CanvasPoint(10, 10));
        }

        Assert.Equal(star.Id, coordinator.SelectedId);
        Assert.Single(coordinator.Figures);
        Assert.Single(rows);
        Assert.Empty(publications);
    }

    [Fact]
    public async Task Star5_D33_DeleteSelectedStarRemovesAndBroadcastsOnce()
    {
        var notifier = new CanvasSyncNotifier();
        var star = StarRow();
        var rows = new List<FigureRow> { star };
        var publications = new List<SyncMessage>();
        using var observer = notifier.Subscribe(7, publications.Add);
        var coordinator = Create(notifier, rows, out var subscription);
        using (subscription)
        {
            coordinator.BeginDrag(star.Id, new CanvasPoint(10, 10));
            await coordinator.DeleteAsync();
        }

        Assert.Empty(coordinator.Figures);
        Assert.Null(coordinator.SelectedId);
        var delete = Assert.Single(publications);
        Assert.Equal("delete", delete.Kind);
        Assert.Equal(star.Id, delete.Id);
        Assert.Null(delete.Figure);
        Assert.Null(delete.X);
        Assert.Null(delete.Y);
    }

    [Fact]
    public async Task Star5_D33D58_DeleteWithNoSelectionIsSilentNoOp()
    {
        var notifier = new CanvasSyncNotifier();
        var star = StarRow();
        var rows = new List<FigureRow> { star };
        var publications = new List<SyncMessage>();
        using var observer = notifier.Subscribe(7, publications.Add);
        var coordinator = Create(notifier, rows, out var subscription);
        using (subscription)
        {
            await coordinator.DeleteAsync();
        }

        Assert.Equal(star, Assert.Single(coordinator.Figures));
        Assert.Null(coordinator.SelectedId);
        Assert.Empty(publications);
    }

    [Fact]
    public async Task Star5_D24D36_DragClampsToEdgeSlidesAndPersistsSingleUpdate()
    {
        var notifier = new CanvasSyncNotifier();
        var star = StarRow(x: CanvasBounds.Width - 20, y: CanvasBounds.Height - 20, bboxW: 20, bboxH: 20);
        var rows = new List<FigureRow> { star };
        var clock = 1L;
        var moveCalls = new List<(Guid Id, decimal X, decimal Y)>();
        var publications = new List<SyncMessage>();
        using var observer = notifier.Subscribe(7, publications.Add);
        var coordinator = Create(notifier, rows, out var subscription, () => clock, (id, x, y, _) =>
        {
            moveCalls.Add((id, x, y));
            return Task.FromResult(1);
        });
        using (subscription)
        {
            coordinator.BeginDrag(star.Id, new CanvasPoint(0, 0));
            coordinator.ContinueDrag(new CanvasPoint(50, -15));
            await coordinator.CommitDragAsync();
        }

        var moved = Assert.Single(coordinator.Figures);
        Assert.Equal(CanvasBounds.Width - (decimal)star.BboxW, moved.X);
        Assert.Equal(CanvasBounds.Height - 35m, moved.Y);
        Assert.Equal(CanvasBounds.Width, moved.X + (decimal)moved.BboxX + (decimal)moved.BboxW);
        Assert.InRange(moved.Y + (decimal)moved.BboxY, 0m, CanvasBounds.Height);
        Assert.InRange(moved.Y + (decimal)moved.BboxY + (decimal)moved.BboxH, 0m, CanvasBounds.Height);

        var call = Assert.Single(moveCalls);
        Assert.Equal(star.Id, call.Id);
        Assert.Equal(moved.X, call.X);
        Assert.Equal(moved.Y, call.Y);
        var moves = publications.Where(message => message.Kind == "move").ToList();
        Assert.Equal(2, moves.Count);
        Assert.Equal(moved.X, moves.Last().X);
        Assert.Equal(moved.Y, moves.Last().Y);
    }

    [Fact]
    public async Task Star5_D40_ZeroRowMoveBroadcastsDeleteWithoutResurrection()
    {
        var notifier = new CanvasSyncNotifier();
        var star = StarRow();
        var rows = new List<FigureRow> { star };
        var publications = new List<SyncMessage>();
        using var observer = notifier.Subscribe(7, publications.Add);
        var coordinator = Create(notifier, rows, out var subscription, move: (_, _, _, _) => Task.FromResult(0));
        using (subscription)
        {
            coordinator.BeginDrag(star.Id, new CanvasPoint(0, 0));
            coordinator.ContinueDrag(new CanvasPoint(10, 0));
            await coordinator.CommitDragAsync();
        }

        Assert.Empty(coordinator.Figures);
        Assert.Null(coordinator.SelectedId);
        var delete = Assert.Single(publications, message => message.Kind == "delete");
        Assert.Equal(star.Id, delete.Id);
        Assert.DoesNotContain(coordinator.Figures, figure => figure.Id == star.Id);
    }

    [Fact]
    public void Star5_D40_RemoteMoveForUnknownIdDoesNotInsert()
    {
        var notifier = new CanvasSyncNotifier();
        var rows = new List<FigureRow>();
        var coordinator = Create(notifier, rows, out var subscription);
        using (subscription)
        {
            coordinator.ApplyRemoteMessage(SyncMessage.Move(Guid.NewGuid(), 10m, 20m, Guid.NewGuid()));
        }

        Assert.Empty(coordinator.Figures);
        Assert.Null(coordinator.SelectedId);
    }

    [Theory]
    [InlineData("draw")]
    [InlineData("move")]
    [InlineData("delete")]
    [InlineData("rollback")]
    public void Star5_D54_DiscardsEveryIncomingKindWhileDragging(string kind)
    {
        var notifier = new CanvasSyncNotifier();
        var star = StarRow();
        var rows = new List<FigureRow> { star };
        var coordinator = Create(notifier, rows, out var subscription);
        using (subscription)
        {
            coordinator.BeginDrag(star.Id, new CanvasPoint(0, 0));
            var message = kind switch
            {
                "draw" => SyncMessage.Draw(StarRow(id: Guid.NewGuid()), Guid.NewGuid()),
                "move" => SyncMessage.Move(star.Id, 99m, 99m, Guid.NewGuid()),
                "delete" => SyncMessage.Delete(star.Id, Guid.NewGuid()),
                _ => SyncMessage.Rollback(star.Id, 99m, 99m, Guid.NewGuid())
            };

            coordinator.ApplyRemoteMessage(message);
        }

        Assert.Equal(star, Assert.Single(coordinator.Figures));
        Assert.Equal(star.Id, coordinator.SelectedId);
    }

    [Fact]
    public async Task Star5_D53_IgnoresOwnDrawAndMoveEchoes()
    {
        var notifier = new CanvasSyncNotifier();
        var rows = new List<FigureRow>();
        var clock = 1L;
        var coordinator = Create(notifier, rows, out var subscription, () => clock);
        using (subscription)
        {
            await coordinator.DrawAsync("star5", new CanvasPoint(10, 10), new CanvasPoint(40, 50));
            var star = Assert.Single(coordinator.Figures);

            coordinator.BeginDrag(star.Id, new CanvasPoint(0, 0));
            coordinator.ContinueDrag(new CanvasPoint(10, 0));
            await coordinator.CommitDragAsync();
        }

        var echoedStar = Assert.Single(coordinator.Figures);
        Assert.Equal("star5", echoedStar.Type);
        Assert.Equal(20m, echoedStar.X);
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

    private static FigureRow StarRow(
        Guid? id = null,
        decimal x = 10,
        decimal y = 10,
        double bboxW = 30,
        double bboxH = 40) => new(
            id ?? Guid.NewGuid(),
            Guid.NewGuid(),
            "star5",
            x,
            y,
            0,
            "{\"points\":[{\"x\":15,\"y\":0},{\"x\":18,\"y\":14},{\"x\":30,\"y\":14},{\"x\":20,\"y\":23},{\"x\":24,\"y\":40},{\"x\":15,\"y\":30},{\"x\":6,\"y\":40},{\"x\":10,\"y\":23},{\"x\":0,\"y\":14},{\"x\":12,\"y\":14}],\"innerRatio\":0.382}",
            "{}",
            1,
            0,
            0,
            bboxW,
            bboxH);

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
