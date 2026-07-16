using System.Collections.Concurrent;

namespace BlazorCanvas.Sync;

/// <summary>
/// D-11's DI singleton notifier for live cross-tab sync. Subscribers are keyed by <c>user_id</c>
/// (D-11 rule 7), matching the database and the cookie claim instead of inventing a second identity.
/// That key is the whole cross-user isolation boundary: a publish for one user never enumerates the
/// outer dictionary and therefore cannot reach another user's bucket. This service deliberately owns
/// only CONSTRAINT-sync-core rules 1 and 7; echo filtering, mid-drag discard, throttling, and message
/// application are per-circuit state owned by the caller.
/// </summary>
public sealed class CanvasSyncNotifier
{
    private readonly ConcurrentDictionary<int, ConcurrentDictionary<Guid, Action<SyncMessage>>> _subscribers = new();

    public IDisposable Subscribe(int userId, Action<SyncMessage> handler)
    {
        var subscriptionId = Guid.NewGuid();
        var bucket = _subscribers.GetOrAdd(userId, _ => new ConcurrentDictionary<Guid, Action<SyncMessage>>());
        bucket[subscriptionId] = handler;

        return new Subscription(() => bucket.TryRemove(subscriptionId, out _));
    }

    /// <summary>
    /// Delivers to every subscriber for the user, including the sender's own tab. That is intentional:
    /// the per-circuit D-11 echo filter needs the sender GUID, and this singleton does not know which
    /// subscription belongs to which tab.
    /// </summary>
    public void Publish(int userId, SyncMessage message)
    {
        if (!_subscribers.TryGetValue(userId, out var bucket))
        {
            return;
        }

        foreach (var handler in bucket.Values.ToArray())
        {
            handler(message);
        }
    }

    private sealed class Subscription(Action onDispose) : IDisposable
    {
        private Action? _onDispose = onDispose;

        public void Dispose() => Interlocked.Exchange(ref _onDispose, null)?.Invoke();
    }
}
