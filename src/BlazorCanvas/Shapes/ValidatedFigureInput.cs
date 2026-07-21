namespace BlazorCanvas.Shapes;

/// <summary>
/// A figure input accepted by <see cref="FigureInputGateway"/>. Its JSON fields were produced by
/// re-serialising the typed records and are the only values safe to persist. Constructing this
/// record outside the gateway defeats that trust boundary. Phase 10 stores <see cref="Bounds"/>,
/// offset by the figure position, in the sole <c>bbox_*</c> cache path.
/// </summary>
public sealed record ValidatedFigureInput(
    string Type,
    IFigureGeometry Geometry,
    string GeometryJson,
    FigureStyle Style,
    string StyleJson,
    Bbox Bounds);
