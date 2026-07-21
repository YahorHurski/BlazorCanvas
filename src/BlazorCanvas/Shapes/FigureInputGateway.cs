using System.Text.Json;

namespace BlazorCanvas.Shapes;

/// <summary>
/// The application-wide trust boundary for client-supplied figure type, geometry, and style.
/// Rejections are deliberately silent: they return <see langword="false"/> without exposing
/// attacker-controlled input through exceptions, logs, or a failure message.
/// </summary>
public sealed class FigureInputGateway
{
    private static readonly JsonDocumentOptions GeometryParseOptions = new()
    {
        MaxDepth = 32,
        AllowTrailingCommas = false,
        CommentHandling = JsonCommentHandling.Disallow,
    };

    private readonly ShapeRegistry _registry;

    public FigureInputGateway(ShapeRegistry registry)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }

    /// <summary>
    /// Validates a wire payload and returns only canonical JSON generated from the accepted records.
    /// There is intentionally no reason code or text: v1.1 rejected degenerate drawing silently.
    /// </summary>
    public bool TryValidate(
        string? typeName,
        string? geometryJson,
        string? styleJson,
        out ValidatedFigureInput? result)
    {
        result = null;

        if (!_registry.TryGet(typeName, out var definition)
            || string.IsNullOrWhiteSpace(geometryJson))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(geometryJson, GeometryParseOptions);
            if (!definition.TryParseGeometry(document.RootElement, out var geometry))
            {
                return false;
            }

            return TryCreateValidatedInput(definition, geometry, styleJson, out result);
        }
        catch (JsonException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    /// <summary>
    /// Validates a draw gesture using the same drawability, bounds, style, and serialisation path
    /// as a JSON payload, returning the placement separately from its local geometry.
    /// </summary>
    public bool TryValidateGesture(
        string? typeName,
        CanvasPoint press,
        CanvasPoint cursor,
        string? styleJson,
        out ValidatedFigureInput? result,
        out double x,
        out double y)
    {
        result = null;
        x = default;
        y = default;

        if (!_registry.TryGet(typeName, out var definition))
        {
            return false;
        }

        var placement = definition.FromGesture(press, cursor);
        if (!TryCreateValidatedInput(definition, placement.Geometry, styleJson, out result))
        {
            return false;
        }

        x = placement.X;
        y = placement.Y;
        return true;
    }

    private static bool TryCreateValidatedInput(
        IShapeDefinition definition,
        IFigureGeometry geometry,
        string? styleJson,
        out ValidatedFigureInput? result)
    {
        result = null;

        if (!definition.IsDrawable(geometry))
        {
            return false;
        }

        // Bounds are computed only after parsing and drawability so hostile geometry cannot reach
        // BoundsOf. The post-condition is defensive: shape parsers already reject non-positive
        // extents, but NaN here would disable the Phase 11 edge clamp.
        var bounds = definition.BoundsOf(geometry);
        if (!double.IsFinite(bounds.X)
            || !double.IsFinite(bounds.Y)
            || !double.IsFinite(bounds.W)
            || !double.IsFinite(bounds.H)
            || bounds.W < 0
            || bounds.H < 0)
        {
            return false;
        }

        // Style is never a rejection reason: the gateway clamps or replaces it with the fixed
        // appearance rather than dropping an otherwise drawable figure.
        var style = StyleGateway.Parse(styleJson);

        // VALID-01: return only canonical JSON produced from typed records; never copy client text.
        var geometryJson = definition.ToJson(geometry);
        var canonicalStyleJson = StyleGateway.ToJson(style);
        result = new ValidatedFigureInput(
            definition.Name,
            geometry,
            geometryJson,
            style,
            canonicalStyleJson,
            bounds);
        return true;
    }
}
