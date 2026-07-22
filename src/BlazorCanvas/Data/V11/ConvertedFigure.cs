using BlazorCanvas.Shapes;

namespace BlazorCanvas.Data.V11;

/// <summary>
/// The v1.11 placement and local geometry converted from a v1.1 row. Coordinates remain decimal,
/// rather than double: integer v1.1 values and numeric(12,3) are exact decimal arithmetic (D-61),
/// so a binary floating-point round trip would be avoidable coordinate drift.
/// </summary>
public sealed record ConvertedFigure(decimal X, decimal Y, IFigureGeometry Geometry);
