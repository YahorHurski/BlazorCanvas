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
        var rounded = (int)Math.Round(distance, MidpointRounding.AwayFromZero);

        return Math.Min(
            rounded,
            Math.Min(
                Math.Min(cx, cy),
                Math.Min(CanvasBounds.Width - cx, CanvasBounds.Height - cy)));
    }
}
