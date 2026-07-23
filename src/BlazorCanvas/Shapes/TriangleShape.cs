using System.Text.Json;
using BlazorCanvas.Geometry;

namespace BlazorCanvas.Shapes;

/// <summary>
/// Defines the ordered three-vertex geometry and gesture behaviour for a triangle.
/// </summary>
public sealed class TriangleShape : IShapeDefinition
{
    public string Name => "triangle";

    public bool TryParseGeometry(JsonElement json, out IFigureGeometry geometry)
    {
        geometry = null!;
        if (json.ValueKind != JsonValueKind.Object
            || !GeometryJson.TryReadPointList(json, "points", 3, out var points))
        {
            return false;
        }

        geometry = new TriangleGeometry(points);
        return true;
    }

    public string ToJson(IFigureGeometry geometry)
    {
        var triangle = geometry as TriangleGeometry
            ?? throw new ArgumentException("TriangleShape requires TriangleGeometry.", nameof(geometry));

        return GeometryJson.Serialise(writer =>
        {
            writer.WriteStartObject();
            GeometryJson.WritePoints(writer, "points", triangle.Points);
            writer.WriteEndObject();
        });
    }

    public bool IsDrawable(IFigureGeometry geometry)
    {
        if (geometry is not TriangleGeometry { Points.Count: 3 } triangle
            || triangle.Points.Any(point => !double.IsFinite(point.X) || !double.IsFinite(point.Y)))
        {
            return false;
        }

        var (p0, p1, p2) = (triangle.Points[0], triangle.Points[1], triangle.Points[2]);
        if (p0 == p1 || p0 == p2 || p1 == p2)
        {
            return false;
        }

        // The v1.1 gesture is collinear only for width/height zero, which it already rejected.
        return ((p1.X - p0.X) * (p2.Y - p0.Y)) - ((p1.Y - p0.Y) * (p2.X - p0.X)) != 0;
    }

    public Bbox BoundsOf(IFigureGeometry geometry)
    {
        var triangle = geometry as TriangleGeometry
            ?? throw new ArgumentException("TriangleShape requires TriangleGeometry.", nameof(geometry));

        var minX = triangle.Points.Min(point => point.X);
        var minY = triangle.Points.Min(point => point.Y);
        var maxX = triangle.Points.Max(point => point.X);
        var maxY = triangle.Points.Max(point => point.Y);
        return new Bbox(minX, minY, maxX - minX, maxY - minY);
    }

    public ShapePlacement FromGesture(CanvasPoint press, CanvasPoint cursor)
    {
        var (px, py) = NormaliseGesturePoint(press);
        var (cx, cy) = NormaliseGesturePoint(cursor);
        var x = Math.Min(px, cx);
        var y = Math.Min(py, cy);
        var w = Math.Abs(cx - px);
        var h = Math.Abs(cy - py);

        // FigureShape.razor provenance: apex top-centre, base along the bottom; preserve .5 for odd widths.
        var points = new[] { new LocalPoint(w / 2.0, 0), new LocalPoint(0, h), new LocalPoint(w, h) };
        return new ShapePlacement(x, y, new TriangleGeometry(points));
    }

    private static (double X, double Y) NormaliseGesturePoint(CanvasPoint point)
    {
        // DrawGesture.cs provenance: browser circuit coordinates are untrusted, so round then clamp first.
        var x = Math.Round(point.X, MidpointRounding.AwayFromZero);
        var y = Math.Round(point.Y, MidpointRounding.AwayFromZero);
        return (Math.Clamp(x, 0, CanvasBounds.Width), Math.Clamp(y, 0, CanvasBounds.Height));
    }
}
