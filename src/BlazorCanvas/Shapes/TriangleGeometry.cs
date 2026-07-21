namespace BlazorCanvas.Shapes;

/// <summary>
/// A three-point triangle in SVG polygon render order. Downward or sideways triangles use
/// different supplied points, not a different formula. Record equality compares the point-list
/// reference; callers and tests must compare <see cref="Points"/> sequences rather than record equality.
/// </summary>
public sealed record TriangleGeometry(IReadOnlyList<LocalPoint> Points) : IFigureGeometry;
