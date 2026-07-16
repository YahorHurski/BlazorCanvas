using BlazorCanvas.Data;
using BlazorCanvas.Geometry;
using BlazorCanvas.Sync;

namespace BlazorCanvas.Tests.Sync;

/// <summary>
/// Proves the one security surface Phase 5 introduces: a broadcast for user A can never reach user B's
/// tab (D-11 rule 7, T-05-01). Also proves ROADMAP criterion 1's resource guarantee that closing a
/// tab leaves the others working, because leaked subscribers and mis-keyed publishes both fail
/// silently. D-54's mid-drag blanket discard and D-11's echo filter are deliberately not here; they
/// are per-circuit component state, not notifier behavior.
/// </summary>
public class CanvasSyncNotifierTests
{
    private static readonly Guid Sender = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly SyncMessage Message = SyncMessage.Delete(42, Sender);

    [Fact]
    public void Publish_ForDifferentUser_DoesNotInvokeHandler()
    {
        var notifier = new CanvasSyncNotifier();
        var received = new List<SyncMessage>();

        notifier.Subscribe(1, received.Add);

        notifier.Publish(2, Message);

        Assert.Empty(received);

        notifier.Publish(1, Message);

        var message = Assert.Single(received);
        Assert.Same(Message, message);
    }

    [Fact]
    public void Publish_ToMultipleSubscribersOfSameUser_InvokesAll()
    {
        var notifier = new CanvasSyncNotifier();
        var first = new List<SyncMessage>();
        var second = new List<SyncMessage>();

        notifier.Subscribe(7, first.Add);
        notifier.Subscribe(7, second.Add);

        notifier.Publish(7, Message);

        Assert.Single(first);
        Assert.Single(second);
        Assert.Same(Message, first[0]);
        Assert.Same(Message, second[0]);
    }

    [Fact]
    public void Subscribe_ThenDispose_StopsReceivingMessages()
    {
        var notifier = new CanvasSyncNotifier();
        var received = new List<SyncMessage>();
        var subscription = notifier.Subscribe(3, received.Add);

        subscription.Dispose();
        notifier.Publish(3, Message);

        Assert.Empty(received);
    }

    [Fact]
    public void Dispose_CalledTwice_IsSafe()
    {
        var notifier = new CanvasSyncNotifier();
        var first = new List<SyncMessage>();
        var second = new List<SyncMessage>();
        var firstSubscription = notifier.Subscribe(4, first.Add);

        notifier.Subscribe(4, second.Add);

        firstSubscription.Dispose();
        firstSubscription.Dispose();
        notifier.Publish(4, Message);

        Assert.Empty(first);
        Assert.Single(second);
    }

    [Fact]
    public void Publish_WithNoSubscribers_DoesNotThrow()
    {
        var notifier = new CanvasSyncNotifier();

        var exception = Record.Exception(() => notifier.Publish(99, Message));

        Assert.Null(exception);
    }

    [Fact]
    public void Publish_WhenHandlerUnsubscribesDuringDelivery_DoesNotThrow()
    {
        var notifier = new CanvasSyncNotifier();
        IDisposable? secondSubscription = null;
        notifier.Subscribe(5, _ => secondSubscription?.Dispose());
        secondSubscription = notifier.Subscribe(5, _ => { });

        var exception = Record.Exception(() => notifier.Publish(5, Message));

        Assert.Null(exception);
    }

    [Fact]
    public void Draw_CarriesTypeAndCoordinates()
    {
        var figure = new Figure
        {
            Id = 12,
            UserId = 1,
            Type = "rectangle",
            X1 = 10,
            Y1 = 20,
            X2 = 30,
            Y2 = 40,
        };

        var message = SyncMessage.Draw(figure, Sender);

        Assert.Equal("draw", message.Kind);
        Assert.Equal(Sender, message.Sender);
        Assert.Equal(12, message.Id);
        Assert.Equal("rectangle", message.Type);
        Assert.Equal(10, message.X1);
        Assert.Equal(20, message.Y1);
        Assert.Equal(30, message.X2);
        Assert.Equal(40, message.Y2);
    }

    [Fact]
    public void Move_CarriesNoType_BecauseTypeNeverChanges()
    {
        var box = new Box(1, 2, 3, 4);

        var message = SyncMessage.Move(13, box, Sender);

        Assert.Equal("move", message.Kind);
        Assert.Equal(Sender, message.Sender);
        Assert.Equal(13, message.Id);
        Assert.Null(message.Type);
        Assert.Equal(1, message.X1);
        Assert.Equal(2, message.Y1);
        Assert.Equal(3, message.X2);
        Assert.Equal(4, message.Y2);
    }

    [Fact]
    public void Delete_CarriesOnlyTheId()
    {
        var message = SyncMessage.Delete(14, Sender);

        Assert.Equal("delete", message.Kind);
        Assert.Equal(Sender, message.Sender);
        Assert.Equal(14, message.Id);
        Assert.Null(message.Type);
        Assert.Null(message.X1);
        Assert.Null(message.Y1);
        Assert.Null(message.X2);
        Assert.Null(message.Y2);
    }

    [Fact]
    public void Rollback_CarriesTheOriginalCoordinates()
    {
        var originalBox = new Box(20, 30, 40, 50);

        var message = SyncMessage.Rollback(15, originalBox, Sender);

        Assert.Equal("rollback", message.Kind);
        Assert.Equal(Sender, message.Sender);
        Assert.Equal(15, message.Id);
        Assert.Null(message.Type);
        Assert.Equal(20, message.X1);
        Assert.Equal(30, message.Y1);
        Assert.Equal(40, message.X2);
        Assert.Equal(50, message.Y2);
    }
}
