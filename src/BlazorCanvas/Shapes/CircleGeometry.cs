namespace BlazorCanvas.Shapes;

/// <summary>
/// Circle geometry whose local origin is the top-left of its bounding square. Its centre is (R, R)
/// and its extent is (0, 0, 2R, 2R); it stores one radius, never rx/ry.
/// </summary>
public sealed record CircleGeometry(double R) : IFigureGeometry;
