namespace BlazorCanvas.Shapes;

/// <summary>
/// A coordinate in a figure's local frame, measured from the figure's own origin.
/// </summary>
public readonly record struct LocalPoint(double X, double Y);
