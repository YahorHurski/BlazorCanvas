namespace BlazorCanvas.Geometry;

/// <summary>
/// A circle is drawn centre-out (press = centre, drag = radius — D-13) but STORED as the
/// square it is inscribed in (D-22, REVISED — the earlier centre+rim encoding is dead).
/// A move is a uniform translation, so the radius is exactly preserved across any number of
/// drags: the offset cancels algebraically in <see cref="ToCentreRadius"/>.
/// </summary>
public static class CircleEncoding
{
    public static Box FromCentreRadius(int cx, int cy, int r) =>
        new(cx - r, cy - r, cx + r, cy + r);

    public static (int Cx, int Cy, int R) ToCentreRadius(Box b)
    {
        var r = (b.X2 - b.X1) / 2;
        var cx = b.X1 + r;
        var cy = b.Y1 + r;
        return (cx, cy, r);
    }

    /// <summary>
    /// The circle draw-clamp (D-24, D-29) — the one genuinely type-specific rule in the app.
    /// Caps the radius at the nearest canvas edge so a circle never renders as an oval.
    /// Known and accepted consequence: pressing near an edge forces a tiny circle.
    /// </summary>
    public static int ClampDrawRadius(int cx, int cy, double distance)
    {
        // The centre is the press point: it must be inside the canvas before it can cap
        // anything. An off-canvas centre would otherwise make one of the four edge-distance
        // terms below negative (CR-01).
        var clampedCx = Movement.ClampDelta(cx, 0, CanvasBounds.Width);
        var clampedCy = Movement.ClampDelta(cy, 0, CanvasBounds.Height);

        var rounded = (int)Math.Round(distance, MidpointRounding.AwayFromZero);

        var capped = Math.Min(
            rounded,
            Math.Min(
                Math.Min(clampedCx, clampedCy),
                Math.Min(CanvasBounds.Width - clampedCx, CanvasBounds.Height - clampedCy)));

        // Never negative — a negative radius normalises into a legal-looking off-canvas circle
        // that every guard and CHECK constraint accepts (CR-01).
        return Math.Max(0, capped);
    }
}
