namespace BlazorCanvas.Tools;

/// <summary>
/// The five armable toolbar tools. Shape names are selected through the registry rather than a
/// second legacy geometry type, so the toolbar cannot drift from the persisted shape catalog.
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
/// Maps an armable tool directly to its registered v1.11 shape name.
/// </summary>
public static class ToolMap
{
    /// <summary>
    /// Returns the registered shape name that <paramref name="tool"/> draws, or
    /// <c>null</c> for <see cref="Tool.Pointer"/>. Null means "this tool does not draw", which is how
    /// the pointer handlers decide to do nothing.
    /// </summary>
    public static string? ToShapeName(Tool tool) => tool switch
    {
        Tool.Pointer => null,
        Tool.Line => "line",
        Tool.Rectangle => "rectangle",
        Tool.Circle => "circle",
        Tool.Triangle => "triangle",
        _ => throw new ArgumentOutOfRangeException(nameof(tool), tool, "Unknown tool.")
    };
}
