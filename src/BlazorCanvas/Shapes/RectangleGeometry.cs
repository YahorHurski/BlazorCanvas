namespace BlazorCanvas.Shapes;

/// <summary>
/// Rectangle geometry whose local origin is the top-left corner and whose extent is (0, 0, W, H).
/// </summary>
public sealed record RectangleGeometry(double W, double H) : IFigureGeometry;
