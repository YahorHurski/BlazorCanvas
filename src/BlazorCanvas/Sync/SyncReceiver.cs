using BlazorCanvas.Data;

namespace BlazorCanvas.Sync;

/// <summary>
/// The D-40 / D-53 receiver rules, extracted from <c>Home.razor</c> so they are unit-testable
/// without a component-test harness (D-49, PA-6). This class owns no UI state: the echo filter
/// (D-11) and the mid-drag blanket discard (D-54) are per-circuit state and stay in the component.
/// <c>draw</c> is the only kind that may create a figure; <c>move</c> and <c>rollback</c> are
/// UPDATE-ONLY, so a message for an unknown id is ignored entirely — never inserted — closing D-40's
/// resurrection hole. <c>delete</c> is idempotent.
/// </summary>
public static class SyncReceiver
{
    /// <summary>
    /// Applies one <see cref="SyncMessage"/> to the tab's figure list in place. Returns the id of a
    /// figure REMOVED by this message, or null — the caller uses this to clear its own selection,
    /// which is per-circuit state this method does not touch.
    /// </summary>
    public static Guid? Apply(List<Figure> figures, SyncMessage message, int userId)
    {
        switch (message.Kind)
        {
            case "draw":
                ApplyDraw(figures, message, userId);
                return null;
            case "move":
            case "rollback":
                ApplyMove(figures, message);
                return null;
            case "delete":
                return ApplyDelete(figures, message);
            default:
                return null;
        }
    }

    private static void ApplyDraw(List<Figure> figures, SyncMessage message, int userId)
    {
        if (figures.Any(f => f.Id == message.Id))
        {
            return;
        }

        figures.Add(new Figure
        {
            Id = message.Id,
            UserId = userId,
            Type = message.Type!,
            X = message.X!.Value,
            Y = message.Y!.Value,
            Geometry = message.Geometry!,
            Z = NextZ(figures),
        });
    }

    private static void ApplyMove(List<Figure> figures, SyncMessage message)
    {
        var existing = figures.FirstOrDefault(f => f.Id == message.Id);
        if (existing is null)
        {
            return;
        }

        existing.X = message.X!.Value;
        existing.Y = message.Y!.Value;
    }

    private static Guid? ApplyDelete(List<Figure> figures, SyncMessage message)
    {
        var removed = figures.RemoveAll(f => f.Id == message.Id);
        return removed > 0 ? message.Id : null;
    }

    private static decimal NextZ(List<Figure> figures) =>
        figures.Count == 0 ? 1m : figures.Max(f => f.Z) + 1m;
}
