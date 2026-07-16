namespace BlazorCanvas.Tools;

/// <summary>
/// The five armable toolbar tools (D-16/D-30/D-33). Deliberately NOT <see cref="Geometry.FigureType"/>:
/// <see cref="Geometry.FigureTypeNames"/>.ToDbValue switches over <see cref="Geometry.FigureType"/> and
/// throws on an unknown value, and the database's <c>figures_type_is_known</c> CHECK enumerates exactly
/// the four shape literals (line, rectangle, circle, triangle). Adding a sixth or seventh member
/// to that enum would compile and then fail at runtime, or worse, produce a literal no CHECK accepts.
/// This enum exists so the toolbar's five armable modes can be represented without touching the
/// database-backed type at all.
/// <c>Pointer</c> is deliberately the first member so <c>default(Tool)</c> is <c>Tool.Pointer</c>,
/// matching D-31: the pointer tool is armed on page load, so a stray first click cannot create a figure.
/// There is no removal-action member here: removing the selected figure is an action button, not an
/// armable mode (D-33) — you click it to act on the selected figure, you do not arm it. CANV-02's
/// "exactly six buttons" is a claim about the toolbar strip, not about this enum; the six are the five
/// armable tools plus that one action button.
/// </summary>
public enum Tool
{
    Pointer,
    Line,
    Rectangle,
    Circle,
    Triangle
}

/// <summary>
/// Maps an armable <see cref="Tool"/> to the <see cref="Geometry.FigureType"/> it draws.
/// </summary>
public static class ToolMap
{
    /// <summary>
    /// Returns the <see cref="Geometry.FigureType"/> that <paramref name="tool"/> draws, or
    /// <c>null</c> for <see cref="Tool.Pointer"/>. Null means "this tool does not draw", which is how
    /// the pointer handlers decide to do nothing.
    /// </summary>
    public static Geometry.FigureType? ToFigureType(Tool tool) => tool switch
    {
        Tool.Pointer => null,
        Tool.Line => Geometry.FigureType.Line,
        Tool.Rectangle => Geometry.FigureType.Rectangle,
        Tool.Circle => Geometry.FigureType.Circle,
        Tool.Triangle => Geometry.FigureType.Triangle,
        _ => throw new ArgumentOutOfRangeException(nameof(tool), tool, "Unknown tool.")
    };
}
