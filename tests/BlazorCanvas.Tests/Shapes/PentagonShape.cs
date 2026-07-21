using System.Text.Json;
using BlazorCanvas.Geometry;
using BlazorCanvas.Shapes;

namespace BlazorCanvas.Tests.Shapes;

/// <summary>
/// Test-only proof that a new point-list shape needs only this class and no shared geometry type.
/// </summary>
public sealed class PentagonShape : IShapeDefinition
{
    public string Name => "pentagon";

    public bool TryParseGeometry(JsonElement json, out IFigureGeometry geometry)
    {
        geometry = null!;
        if (json.ValueKind != JsonValueKind.Object
            || !GeometryJson.TryReadPointList(json, "points", 5, out var points))
        {
            return false;
        }

        geometry = new PentagonGeometry(points);
        return true;
    }

    public string ToJson(IFigureGeometry geometry)
    {
        var pentagon = geometry as PentagonGeometry
            ?? throw new ArgumentException("PentagonShape requires PentagonGeometry.", nameof(geometry));

        return GeometryJson.Serialise(writer =>
        {
            writer.WriteStartObject();
            GeometryJson.WritePoints(writer, "points", pentagon.Points);
            writer.WriteEndObject();
        });
    }

    public bool IsDrawable(IFigureGeometry geometry)
    {
        if (geometry is not PentagonGeometry { Points.Count: 5 } pentagon
            || pentagon.Points.Any(point => !double.IsFinite(point.X) || !double.IsFinite(point.Y))
            || pentagon.Points.Distinct().Count() != 5)
        {
            return false;
        }

        var twiceArea = 0d;
        for (var index = 0; index < pentagon.Points.Count; index++)
        {
            var current = pentagon.Points[index];
            var next = pentagon.Points[(index + 1) % pentagon.Points.Count];
            twiceArea += (current.X * next.Y) - (current.Y * next.X);
        }

        return twiceArea != 0;
    }

    public Bbox BoundsOf(IFigureGeometry geometry)
    {
        var pentagon = geometry as PentagonGeometry
            ?? throw new ArgumentException("PentagonShape requires PentagonGeometry.", nameof(geometry));

        var minX = pentagon.Points.Min(point => point.X);
        var minY = pentagon.Points.Min(point => point.Y);
        var maxX = pentagon.Points.Max(point => point.X);
        var maxY = pentagon.Points.Max(point => point.Y);
        return new Bbox(minX, minY, maxX - minX, maxY - minY);
    }

    public ShapePlacement FromGesture(CanvasPoint press, CanvasPoint cursor)
    {
        var (pressX, pressY) = NormaliseGesturePoint(press);
        var (cursorX, cursorY) = NormaliseGesturePoint(cursor);
        var x = Math.Min(pressX, cursorX);
        var y = Math.Min(pressY, cursorY);
        var width = Math.Abs(cursorX - pressX);
        var height = Math.Abs(cursorY - pressY);
        var radiusX = width / 2;
        var radiusY = height / 2;

        var points = Enumerable.Range(0, 5)
            .Select(index =>
            {
                var theta = (-Math.PI / 2) + (index * ((2 * Math.PI) / 5));
                return new LocalPoint(
                    radiusX + (radiusX * Math.Cos(theta)),
                    radiusY + (radiusY * Math.Sin(theta)));
            })
            .ToArray();

        return new ShapePlacement(x, y, new PentagonGeometry(points));
    }

    private static (double X, double Y) NormaliseGesturePoint(CanvasPoint point)
    {
        var x = Math.Round(point.X, MidpointRounding.AwayFromZero);
        var y = Math.Round(point.Y, MidpointRounding.AwayFromZero);
        return (Math.Clamp(x, 0, CanvasBounds.Width), Math.Clamp(y, 0, CanvasBounds.Height));
    }

    private sealed record PentagonGeometry(IReadOnlyList<LocalPoint> Points) : IFigureGeometry;
}
