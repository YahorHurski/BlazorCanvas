namespace BlazorCanvas.Data;

/// <summary>
/// A drawn figure. `X1,Y1,X2,Y2` are ALWAYS the figure's bounding box (D-22, D-20) — a circle is
/// stored as the square it is inscribed in, never as centre + rim point. `Type` is a plain
/// `string`, deliberately (D-46): the database CHECK constraints are written as `type &lt;&gt;
/// 'circle'`, and a C# enum (PascalCase `ToString()`, or an int-mapped conversion) would silently
/// invalidate every one of them. The four lowercase literals are supplied by the geometry core's
/// `FigureTypeNames.ToDbValue` (plan 01-02).
/// </summary>
public class Figure
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string Type { get; set; } = string.Empty;

    public int X1 { get; set; }

    public int Y1 { get; set; }

    public int X2 { get; set; }

    public int Y2 { get; set; }
}
