namespace BlazorCanvas.Shapes;

/// <summary>
/// The position and local geometry produced by a draw gesture.
/// </summary>
public sealed record ShapePlacement(double X, double Y, IFigureGeometry Geometry);
