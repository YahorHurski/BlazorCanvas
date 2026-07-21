using System.Text.Json;
using BlazorCanvas.Geometry;

namespace BlazorCanvas.Shapes;

/// <summary>
/// Defines the ordered point-list geometry and gesture behaviour for a line.
/// </summary>
public sealed class LineShape : IShapeDefinition
{
    public string Name => "line";

    public bool TryParseGeometry(JsonElement json, out IFigureGeometry geometry)
    {
        geometry = null!;

        if (json.ValueKind != JsonValueKind.Object
            || !GeometryJson.TryReadPointList(json, "points", 2, out var points))
        {
            return false;
        }

        // D-60: diagonal direction lives in the data. Do not reintroduce the v1.1 whole-pair swap.
        geometry = new LineGeometry(points);
        return true;
    }

    public string ToJson(IFigureGeometry geometry)
    {
        var line = geometry as LineGeometry
            ?? throw new ArgumentException("LineShape requires LineGeometry.", nameof(geometry));

        return GeometryJson.Serialise(writer =>
        {
            writer.WriteStartObject();
            GeometryJson.WritePoints(writer, "points", line.Points);
            writer.WriteEndObject();
        });
    }

    public bool IsDrawable(IFigureGeometry geometry) =>
        geometry is LineGeometry { Points.Count: 2 } line
        && AreFinite(line.Points)
        && line.Points[0] != line.Points[1];

    public Bbox BoundsOf(IFigureGeometry geometry)
    {
        var line = geometry as LineGeometry
            ?? throw new ArgumentException("LineShape requires LineGeometry.", nameof(geometry));

        var points = line.Points;
        var minX = Math.Min(points[0].X, points[1].X);
        var minY = Math.Min(points[0].Y, points[1].Y);
        var maxX = Math.Max(points[0].X, points[1].X);
        var maxY = Math.Max(points[0].Y, points[1].Y);
        return new Bbox(minX, minY, maxX - minX, maxY - minY);
    }

    public ShapePlacement FromGesture(CanvasPoint press, CanvasPoint cursor)
    {
        var (px, py) = NormaliseGesturePoint(press);
        var (cx, cy) = NormaliseGesturePoint(cursor);
        var x = Math.Min(px, cx);
        var y = Math.Min(py, cy);

        // Preserve draw order: it is information-preserving while SVG endpoint pairs render symmetrically.
        var points = new[] { new LocalPoint(px - x, py - y), new LocalPoint(cx - x, cy - y) };
        return new ShapePlacement(x, y, new LineGeometry(points));
    }

    private static bool AreFinite(IReadOnlyList<LocalPoint> points) =>
        points.All(point => double.IsFinite(point.X) && double.IsFinite(point.Y));

    private static (double X, double Y) NormaliseGesturePoint(CanvasPoint point)
    {
        // DrawGesture.cs provenance: browser circuit coordinates are untrusted, so round then clamp first.
        var x = Math.Round(point.X, MidpointRounding.AwayFromZero);
        var y = Math.Round(point.Y, MidpointRounding.AwayFromZero);
        return (Math.Clamp(x, 0, CanvasBounds.Width), Math.Clamp(y, 0, CanvasBounds.Height));
    }
}
