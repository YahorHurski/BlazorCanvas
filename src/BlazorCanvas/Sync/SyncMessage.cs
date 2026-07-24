using BlazorCanvas.Data;

namespace BlazorCanvas.Sync;

/// <summary>
/// D-53's canonical broadcast contract, as amended by D-59 for the anchor+geometry storage model.
/// D-53 supersedes D-11, D-22, and D-40's partial descriptions of the live-sync payload: one flat
/// record covers draw, move, delete, and rollback. <c>draw</c> carries the anchor, the type, and the
/// stored geometry JSON — it is the only kind that may create a figure. <c>move</c> and
/// <c>rollback</c> carry the anchor alone: a figure's type and geometry never change on a move, and
/// the receiver already holds both. There is still no <c>drop</c> kind — a drag's final position is
/// the last <c>move</c> message. <c>delete</c> carries only the id. <c>Sender</c> exists solely for
/// D-11's echo filter, where a circuit ignores messages from its own per-circuit <see cref="Guid"/>.
/// The type cannot express the most important receiver rule: <c>move</c> and <c>rollback</c> are
/// UPDATE-ONLY by D-40, so a message for an unknown id is ignored entirely and never inserted.
/// </summary>
public sealed record SyncMessage(string Kind, Guid Sender, Guid Id, string? Type, int? X, int? Y, string? Geometry)
{
    public static SyncMessage Draw(Figure f, Guid sender) =>
        new("draw", sender, f.Id, f.Type, f.X, f.Y, f.Geometry);

    /// <summary>
    /// Carries the anchor without <c>Type</c> or <c>Geometry</c> because D-53 fixes that a figure's
    /// type and shape never change on a move, and the receiver already knows both.
    /// </summary>
    public static SyncMessage Move(Guid id, int x, int y, Guid sender) =>
        new("move", sender, id, null, x, y, null);

    public static SyncMessage Delete(Guid id, Guid sender) =>
        new("delete", sender, id, null, null, null, null);

    public static SyncMessage Rollback(Guid id, int x, int y, Guid sender) =>
        new("rollback", sender, id, null, x, y, null);
}
