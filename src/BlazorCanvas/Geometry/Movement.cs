namespace BlazorCanvas.Geometry;

/// <summary>
/// The move clamp (D-36). Clamps the movement DELTA, then translates all four coordinates
/// uniformly. Bounds are INCLUSIVE: 0..1280 x 0..720. Never clamp X2/Y2 independently of
/// X1/Y1 — that resizes the figure instead of moving it.
/// </summary>
public static class Movement
{
    // lo > hi is the degenerate case: a box wider than the canvas (lo = -bx1, hi = W - bx2, so
    // lo > hi reduces to width > W) or already partly out of bounds. Left unguarded, Min/Max
    // order silently returns hi (which is < lo), producing a nonzero "clamped delta" for a
    // zero-input delta and teleporting the figure (CR-02). An oversized/out-of-bounds box that
    // cannot legally fit inside the canvas simply does not move.
    public static int ClampDelta(int v, int lo, int hi) =>
        lo > hi ? 0 : Math.Min(Math.Max(v, lo), hi);

    public static Box ClampMove(Box b, int dx, int dy)
    {
        // Recompute the bounding box rather than trust X1<=X2/Y1<=Y2: a normalised line may
        // legally have Y1 > Y2 (D-41). Trusting raw Y1/Y2 here would silently mis-clamp every
        // down-and-right line.
        var bx1 = Math.Min(b.X1, b.X2);
        var by1 = Math.Min(b.Y1, b.Y2);
        var bx2 = Math.Max(b.X1, b.X2);
        var by2 = Math.Max(b.Y1, b.Y2);

        // Per-axis independence: dx' is computed from x-terms only, dy' from y-terms only.
        var dxPrime = ClampDelta(dx, -bx1, CanvasBounds.Width - bx2);
        var dyPrime = ClampDelta(dy, -by1, CanvasBounds.Height - by2);

        // Translate all four coordinates uniformly — a move is a move, never a resize.
        return new Box(b.X1 + dxPrime, b.Y1 + dyPrime, b.X2 + dxPrime, b.Y2 + dyPrime);
    }
}
