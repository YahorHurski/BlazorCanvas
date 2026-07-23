namespace BlazorCanvas.Data.V11;

/// <summary>
/// A v1.11 figure row as read from PostgreSQL. Positions and <see cref="Z"/> are decimals because
/// their <c>numeric</c> columns must retain exact translations and layer subdivisions. Bounds are
/// doubles because <c>bbox_*</c> is deliberately a coarse <c>double precision</c> cache. Geometry
/// and style remain JSON text: parsing belongs to the shape registry, not to this data-access layer.
/// </summary>
public sealed record FigureRow(
    Guid Id,
    Guid CanvasId,
    string Type,
    decimal X,
    decimal Y,
    decimal Rotation,
    string GeometryJson,
    string StyleJson,
    decimal Z,
    double BboxX,
    double BboxY,
    double BboxW,
    double BboxH);
