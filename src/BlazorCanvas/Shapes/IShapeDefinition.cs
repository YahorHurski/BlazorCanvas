using System.Text.Json;

namespace BlazorCanvas.Shapes;

/// <summary>
/// Defines the complete type-specific contract for a canvas shape.
/// </summary>
public interface IShapeDefinition
{
    /// <summary>
    /// Gets the exact lowercase literal stored by <c>figure_types.name</c> and <c>figures.type</c>.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Parses and validates geometry. Invalid or hostile input is rejected silently so
    /// attacker-controlled text cannot travel through an exception message to a log or another tab.
    /// </summary>
    bool TryParseGeometry(JsonElement json, out IFigureGeometry geometry);

    /// <summary>
    /// Serialises a correctly typed geometry record in its canonical form.
    /// </summary>
    string ToJson(IFigureGeometry geometry);

    /// <summary>
    /// Determines whether geometry is drawable.
    /// </summary>
    bool IsDrawable(IFigureGeometry geometry);

    /// <summary>
    /// Produces the local extent used by the single code path that writes the <c>bbox_*</c> cache.
    /// The extent may begin away from the local origin after a future vertex edit.
    /// </summary>
    Bbox BoundsOf(IFigureGeometry geometry);

    /// <summary>
    /// Converts a browser draw gesture into position and local geometry. Implementations must clamp
    /// both untrusted circuit-supplied inputs into CanvasBounds before type-specific processing.
    /// </summary>
    ShapePlacement FromGesture(CanvasPoint press, CanvasPoint cursor);
}
