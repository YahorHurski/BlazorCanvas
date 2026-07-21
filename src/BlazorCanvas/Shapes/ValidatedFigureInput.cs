namespace BlazorCanvas.Shapes;

/// <summary>
/// A figure input accepted by <see cref="FigureInputGateway"/>. Its JSON fields were produced by
/// re-serialising the typed records and are the only values safe to persist. Constructing this
/// record outside the gateway defeats that trust boundary. Phase 10 stores <see cref="Bounds"/>,
/// offset by the figure position, in the sole <c>bbox_*</c> cache path.
/// </summary>
public sealed record ValidatedFigureInput
{
    /// <summary>The registry-owned literal identifying the accepted figure shape.</summary>
    public string Type { get; }

    /// <summary>The typed geometry produced by the registered shape definition.</summary>
    public IFigureGeometry Geometry { get; }

    /// <summary>Canonical geometry JSON generated from <see cref="Geometry"/>.</summary>
    public string GeometryJson { get; }

    /// <summary>The sanitised drawing style.</summary>
    public FigureStyle Style { get; }

    /// <summary>Canonical style JSON generated from <see cref="Style"/>.</summary>
    public string StyleJson { get; }

    /// <summary>The local bounds computed by the registered shape definition.</summary>
    public Bbox Bounds { get; }

    // This type is intentionally not publicly constructible: FigureInputGateway is the sole
    // production entry point that may attach the validated marker to client-controlled values.
    internal ValidatedFigureInput(
        string type,
        IFigureGeometry geometry,
        string geometryJson,
        FigureStyle style,
        string styleJson,
        Bbox bounds)
    {
        Type = type;
        Geometry = geometry;
        GeometryJson = geometryJson;
        Style = style;
        StyleJson = styleJson;
        Bounds = bounds;
    }
}
