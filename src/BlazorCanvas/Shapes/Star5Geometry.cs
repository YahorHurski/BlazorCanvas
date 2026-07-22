namespace BlazorCanvas.Shapes;

/// <summary>
/// A five-pointed star in SVG polygon render order. The point list is authoritative for rendering
/// and bounds; <see cref="InnerRatio"/> records the ratio used to derive the initial gesture shape.
/// </summary>
public sealed record Star5Geometry(IReadOnlyList<LocalPoint> Points, double InnerRatio) : IFigureGeometry;
