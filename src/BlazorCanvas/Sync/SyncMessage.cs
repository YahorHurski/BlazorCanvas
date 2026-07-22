using BlazorCanvas.Data.V11;

namespace BlazorCanvas.Sync;

/// <summary>
/// D-53's v1.11 wire contract. Draw carries a canonical persisted row; all other kinds are
/// deliberately position/identity-only so a move can never replace local geometry or style.
/// </summary>
public sealed record SyncMessage(string Kind, Guid Sender, Guid Id, FigureRow? Figure, decimal? X, decimal? Y)
{
    public static SyncMessage Draw(FigureRow figure, Guid sender)
    {
        ArgumentNullException.ThrowIfNull(figure);
        return new("draw", sender, figure.Id, figure, null, null);
    }

    public static SyncMessage Move(Guid id, decimal x, decimal y, Guid sender) =>
        new("move", sender, id, null, x, y);

    public static SyncMessage Delete(Guid id, Guid sender) =>
        new("delete", sender, id, null, null, null);

    public static SyncMessage Rollback(Guid id, decimal x, decimal y, Guid sender) =>
        new("rollback", sender, id, null, x, y);
}
