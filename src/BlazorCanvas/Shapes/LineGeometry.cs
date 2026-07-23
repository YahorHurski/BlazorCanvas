namespace BlazorCanvas.Shapes;

/// <summary>
/// A two-point line in draw order. Point order carries the diagonal direction and must never be
/// canonicalised, sorted, or swapped. Record equality compares the point-list reference; callers
/// and tests must compare <see cref="Points"/> sequences rather than record equality.
/// </summary>
public sealed record LineGeometry(IReadOnlyList<LocalPoint> Points) : IFigureGeometry;
