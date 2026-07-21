using System.Text.Json;
using BlazorCanvas.Geometry;

namespace BlazorCanvas.Shapes;

/// <summary>
/// Defines the local extent and draw gesture behaviour for a rectangle.
/// </summary>
public sealed class RectangleShape : IShapeDefinition
{
    public string Name => "rectangle";

    public bool TryParseGeometry(JsonElement json, out IFigureGeometry geometry)
    {
        geometry = null!;
        if (json.ValueKind != JsonValueKind.Object
            || !GeometryJson.TryReadFiniteDouble(json, "w", out var width)
            || !GeometryJson.TryReadFiniteDouble(json, "h", out var height)
            || width <= 0
            || height <= 0)
        {
            return false;
        }

        // Non-positive extents are structurally meaningless; rejecting them makes BoundsOf total.
        geometry = new RectangleGeometry(width, height);
        return true;
    }

    public string ToJson(IFigureGeometry geometry)
    {
        var rectangle = geometry as RectangleGeometry
            ?? throw new ArgumentException("RectangleShape requires RectangleGeometry.", nameof(geometry));

        return GeometryJson.Serialise(writer =>
        {
            writer.WriteStartObject();
            GeometryJson.WriteNumber(writer, "w", rectangle.W);
            GeometryJson.WriteNumber(writer, "h", rectangle.H);
            writer.WriteEndObject();
        });
    }

    public bool IsDrawable(IFigureGeometry geometry) =>
        geometry is RectangleGeometry rectangle
        && double.IsFinite(rectangle.W)
        && double.IsFinite(rectangle.H)
        && rectangle.W > 0
        && rectangle.H > 0;

    public Bbox BoundsOf(IFigureGeometry geometry)
    {
        var rectangle = geometry as RectangleGeometry
            ?? throw new ArgumentException("RectangleShape requires RectangleGeometry.", nameof(geometry));
        return new Bbox(0, 0, rectangle.W, rectangle.H);
    }

    public ShapePlacement FromGesture(CanvasPoint press, CanvasPoint cursor)
    {
        var (px, py) = NormaliseGesturePoint(press);
        var (cx, cy) = NormaliseGesturePoint(cursor);
        var x = Math.Min(px, cx);
        var y = Math.Min(py, cy);
        return new ShapePlacement(x, y, new RectangleGeometry(Math.Abs(cx - px), Math.Abs(cy - py)));
    }

    private static (double X, double Y) NormaliseGesturePoint(CanvasPoint point)
    {
        // DrawGesture.cs provenance: browser circuit coordinates are untrusted, so round then clamp first.
        var x = Math.Round(point.X, MidpointRounding.AwayFromZero);
        var y = Math.Round(point.Y, MidpointRounding.AwayFromZero);
        return (Math.Clamp(x, 0, CanvasBounds.Width), Math.Clamp(y, 0, CanvasBounds.Height));
    }
}
