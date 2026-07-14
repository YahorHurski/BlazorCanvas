namespace BlazorCanvas.Geometry;

/// <summary>
/// Maps <see cref="FigureType"/> to and from the exact lowercase literals the database's
/// <c>type</c> CHECK constraint accepts (D-46). Deliberately not <c>Enum.ToString()</c>, which
/// yields PascalCase and would silently fail every CHECK written as <c>type &lt;&gt; 'circle'</c>.
/// </summary>
public static class FigureTypeNames
{
    public static string ToDbValue(FigureType type) => type switch
    {
        FigureType.Line => "line",
        FigureType.Rectangle => "rectangle",
        FigureType.Circle => "circle",
        FigureType.Triangle => "triangle",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown figure type.")
    };

    public static FigureType Parse(string value) => value switch
    {
        "line" => FigureType.Line,
        "rectangle" => FigureType.Rectangle,
        "circle" => FigureType.Circle,
        "triangle" => FigureType.Triangle,
        _ => throw new ArgumentException($"Unknown figure type literal: '{value}'.", nameof(value))
    };
}
