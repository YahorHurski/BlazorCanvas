using BlazorCanvas.Data;
using BlazorCanvas.Geometry;

namespace BlazorCanvas.Sync;

/// <summary>
/// D-53's canonical broadcast contract. D-53 supersedes D-11, D-22, and D-40's partial
/// descriptions of the live-sync payload: one flat record covers draw, move, delete, and rollback.
/// <c>Sender</c> exists solely for D-11's echo filter, where a circuit ignores messages from its own
/// per-circuit <see cref="Guid"/>. The type cannot express the most important receiver rule:
/// <c>move</c> and <c>rollback</c> are UPDATE-ONLY by D-40, so an unknown id is ignored entirely and
/// never inserted.
/// </summary>
public sealed record SyncMessage(string Kind, Guid Sender, Guid Id, string? Type, int? X1, int? Y1, int? X2, int? Y2)
{
    public static SyncMessage Draw(Figure f, Guid sender) =>
        new("draw", sender, f.Id, f.Type, FigureBox(f).X1, FigureBox(f).Y1, FigureBox(f).X2, FigureBox(f).Y2);

    /// <summary>
    /// Carries coordinates without <c>Type</c> because D-53 fixes that a figure's type never changes
    /// and the receiver already knows it.
    /// </summary>
    public static SyncMessage Move(Guid id, Box box, Guid sender) =>
        new("move", sender, id, null, box.X1, box.Y1, box.X2, box.Y2);

    public static SyncMessage Delete(Guid id, Guid sender) =>
        new("delete", sender, id, null, null, null, null, null);

    public static SyncMessage Rollback(Guid id, Box originalBox, Guid sender) =>
        new("rollback", sender, id, null, originalBox.X1, originalBox.Y1, originalBox.X2, originalBox.Y2);

    private static Box FigureBox(Figure f) =>
        GeometryCodec.DecodeToBox(FigureTypeNames.Parse(f.Type), f.X, f.Y, f.Geometry);
}
