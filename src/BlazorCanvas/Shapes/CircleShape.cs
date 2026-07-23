using System.Text.Json;
using BlazorCanvas.Geometry;

namespace BlazorCanvas.Shapes;

/// <summary>
/// Defines the bounding-square-local geometry and centre-out draw gesture for a circle.
/// </summary>
public sealed class CircleShape : IShapeDefinition
{
    public string Name => "circle";

    public bool TryParseGeometry(JsonElement json, out IFigureGeometry geometry)
    {
        geometry = null!;
        if (json.ValueKind != JsonValueKind.Object
            || !GeometryJson.TryReadFiniteDouble(json, "r", out var radius)
            || radius <= 0)
        {
            return false;
        }

        geometry = new CircleGeometry(radius);
        return true;
    }

    public string ToJson(IFigureGeometry geometry)
    {
        var circle = geometry as CircleGeometry
            ?? throw new ArgumentException("CircleShape requires CircleGeometry.", nameof(geometry));

        return GeometryJson.Serialise(writer =>
        {
            writer.WriteStartObject();
            GeometryJson.WriteNumber(writer, "r", circle.R);
            writer.WriteEndObject();
        });
    }

    public bool IsDrawable(IFigureGeometry geometry) =>
        geometry is CircleGeometry circle && double.IsFinite(circle.R) && circle.R > 0;

    public Bbox BoundsOf(IFigureGeometry geometry)
    {
        var circle = geometry as CircleGeometry
            ?? throw new ArgumentException("CircleShape requires CircleGeometry.", nameof(geometry));

        // D-60 migration stores x,y=x1,y1 and {"r":(x2-x1)/2}: local origin is the square's top-left.
        return new Bbox(0, 0, 2 * circle.R, 2 * circle.R);
    }

    public ShapePlacement FromGesture(CanvasPoint press, CanvasPoint cursor)
    {
        var (centreX, centreY) = NormaliseGesturePoint(press);
        var (cursorX, cursorY) = NormaliseGesturePoint(cursor);
        var dx = cursorX - centreX;
        var dy = cursorY - centreY;
        var roundedDistance = Math.Round(Math.Sqrt((dx * dx) + (dy * dy)), MidpointRounding.AwayFromZero);
        var radius = Math.Max(0, Math.Min(roundedDistance, Math.Min(Math.Min(centreX, centreY), Math.Min(CanvasBounds.Width - centreX, CanvasBounds.Height - centreY))));

        // CircleEncoding.cs provenance: a near-edge press deliberately caps the radius; circles never become ovals.
        return new ShapePlacement(centreX - radius, centreY - radius, new CircleGeometry(radius));
    }

    private static (double X, double Y) NormaliseGesturePoint(CanvasPoint point)
    {
        // DrawGesture.cs provenance: browser circuit coordinates are untrusted, so round then clamp first.
        var x = Math.Round(point.X, MidpointRounding.AwayFromZero);
        var y = Math.Round(point.Y, MidpointRounding.AwayFromZero);
        return (Math.Clamp(x, 0, CanvasBounds.Width), Math.Clamp(y, 0, CanvasBounds.Height));
    }
}
