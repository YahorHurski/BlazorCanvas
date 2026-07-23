using BlazorCanvas.Data.V11;
using BlazorCanvas.Sync;

namespace BlazorCanvas.Tests.Sync;

public class CanvasSyncNotifierTests
{
    private static readonly Guid Sender = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid FigureId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly SyncMessage Message = SyncMessage.Delete(FigureId, Sender);

    [Fact]
    public void Publish_IsBucketedByOwnerAndDisposalIsSafe()
    {
        var notifier = new CanvasSyncNotifier();
        var first = new List<SyncMessage>();
        var second = new List<SyncMessage>();
        var subscription = notifier.Subscribe(1, first.Add);
        notifier.Subscribe(2, second.Add);

        notifier.Publish(1, Message);
        subscription.Dispose();
        subscription.Dispose();
        notifier.Publish(1, Message);

        Assert.Single(first);
        Assert.Empty(second);
    }

    [Fact]
    public void Draw_CarriesCanonicalRow()
    {
        var row = Row();
        var message = SyncMessage.Draw(row, Sender);

        Assert.Equal("draw", message.Kind);
        Assert.Equal(row.Id, message.Id);
        Assert.Same(row, message.Figure);
        Assert.Null(message.X);
        Assert.Null(message.Y);
    }

    [Fact]
    public void MoveAndRollback_AreIdentityAndPositionOnly()
    {
        var move = SyncMessage.Move(FigureId, 10.25m, 20.5m, Sender);
        var rollback = SyncMessage.Rollback(FigureId, 1m, 2m, Sender);

        Assert.Equal(("move", FigureId, 10.25m, 20.5m), (move.Kind, move.Id, move.X, move.Y));
        Assert.Equal(("rollback", FigureId, 1m, 2m), (rollback.Kind, rollback.Id, rollback.X, rollback.Y));
        Assert.Null(move.Figure);
        Assert.Null(rollback.Figure);
    }

    [Fact]
    public void Delete_CarriesOnlyIdentity()
    {
        var message = SyncMessage.Delete(FigureId, Sender);
        Assert.Equal("delete", message.Kind);
        Assert.Equal(FigureId, message.Id);
        Assert.Null(message.Figure);
        Assert.Null(message.X);
        Assert.Null(message.Y);
    }

    private static FigureRow Row() => new(FigureId, Guid.NewGuid(), "rectangle", 10.25m, 20.5m, 0m,
        "{\"w\":20,\"h\":10}", "{\"stroke\":\"#000000\",\"stroke_width\":2,\"fill\":\"#FFFFFF\",\"opacity\":1}", 1m, 0, 0, 20, 10);
}
