namespace BlazorCanvas.Geometry;

/// <summary>
/// The per-type minimum-size guard (D-50). Since D-59 the server is the sole guarantor of
/// geometry well-formedness — no database CHECK mirrors this any more (D-59 item 9). Each arm
/// reads the exact C# integer <see cref="GeometryCodec"/> will serialise into <c>geometry</c>,
/// before any JSON is produced: the line's <c>{dx,dy}</c> pair, the rectangle/triangle
/// <c>{w,h}</c> pair, and the circle's <c>{r}</c>. Rejected only when a gesture's extent is
/// strictly zero, never merely small (D-59 item 7, D-32). Takes a NORMALISED box. A rejection is
/// silent — no dialog, toast, hint, or log line (D-50).
/// </summary>
public static class MinSizeGuard
{
    public static bool IsDrawable(FigureType type, Box b) => type switch
    {
        // Rejected only when both endpoints are the identical point — i.e. the {dx,dy} pair the
        // codec would emit is {0,0}. Horizontal (dy zero) and vertical (dx zero) lines stay
        // legal. dx >= 0 is guaranteed upstream by Normalisation and is not re-checked here.
        FigureType.Line => b.Width != 0 || b.Height != 0,

        // Rejected when the {w,h} pair the codec would emit has a zero-or-negative member.
        FigureType.Rectangle or FigureType.Triangle => b.Width > 0 && b.Height > 0,

        // Rejected when the integer radius {r} is zero or negative. Read through
        // CircleEncoding.ToCentreRadius so the guard and GeometryCodec.Encode's {r} can never
        // disagree (T-10-02) — a naive width-greater-than-zero rule would let a width-1 box
        // through as {"r":0}, an invisible, unselectable poison row. The retired square-ness and
        // even-side terms mirrored the circle_is_a_circle CHECK that D-59 deleted (D-59 item 9);
        // a circle is well-formed by construction via CircleEncoding.FromCentreRadius now.
        FigureType.Circle => CircleEncoding.ToCentreRadius(b).R > 0,

        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown figure type.")
    };
}
