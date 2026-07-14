namespace BlazorCanvas.Geometry;

/// <summary>
/// The per-type minimum-size guard (D-50). Each arm is a literal C# transcription of the
/// matching database CHECK constraint, so the app can never construct a figure the database
/// would refuse. Takes a NORMALISED box. D-23's single shared guard is retracted: one shared
/// rule either lets a zero-height rectangle through or rejects a legal horizontal line.
/// </summary>
public static class MinSizeGuard
{
    public static bool IsDrawable(FigureType type, Box b) => type switch
    {
        // Mirrors line_is_a_line. Rejected only when both endpoints are identical.
        // Horizontal and vertical lines are legal.
        FigureType.Line => b.X2 >= b.X1 && (b.X2 > b.X1 || b.Y2 != b.Y1),

        // Mirrors box_is_a_box. Zero width or zero height is rejected.
        FigureType.Rectangle or FigureType.Triangle => b.X2 > b.X1 && b.Y2 > b.Y1,

        // Mirrors circle_is_a_circle: square, positive, even side.
        FigureType.Circle => b.Width == b.Height && b.X2 > b.X1 && b.Width % 2 == 0,

        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown figure type.")
    };
}
