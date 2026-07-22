using System.Text.Json;
using BlazorCanvas.Geometry;

namespace BlazorCanvas.Shapes;

/// <summary>
/// Defines the ordered ten-vertex geometry and gesture behaviour for a five-pointed star.
/// </summary>
public sealed class Star5Shape : IShapeDefinition
{
    public const double DefaultInnerRatio = 0.382;

    public string Name => "star5";

    public bool TryParseGeometry(JsonElement json, out IFigureGeometry geometry)
    {
        geometry = null!;
        if (json.ValueKind != JsonValueKind.Object
            || !GeometryJson.TryReadPointList(json, "points", 10, out var points)
            || !GeometryJson.TryReadFiniteDouble(json, "innerRatio", out var innerRatio)
            || innerRatio <= 0)
        {
            return false;
        }

        geometry = new Star5Geometry(points, innerRatio);
        return true;
    }

    public string ToJson(IFigureGeometry geometry)
    {
        var star = geometry as Star5Geometry
            ?? throw new ArgumentException("Star5Shape requires Star5Geometry.", nameof(geometry));

        return GeometryJson.Serialise(writer =>
        {
            writer.WriteStartObject();
            GeometryJson.WritePoints(writer, "points", star.Points);
            GeometryJson.WriteNumber(writer, "innerRatio", star.InnerRatio);
            writer.WriteEndObject();
        });
    }

    public bool IsDrawable(IFigureGeometry geometry)
    {
        if (geometry is not Star5Geometry { Points.Count: 10 } star
            || !double.IsFinite(star.InnerRatio)
            || star.InnerRatio <= 0
            || star.Points.Any(point => !double.IsFinite(point.X) || !double.IsFinite(point.Y))
            || star.Points.Distinct().Count() != 10)
        {
            return false;
        }

        var bounds = BoundsOf(star);
        if (bounds.W <= 0 || bounds.H <= 0)
        {
            return false;
        }

        var twiceArea = 0d;
        for (var index = 0; index < star.Points.Count; index++)
        {
            var current = star.Points[index];
            var next = star.Points[(index + 1) % star.Points.Count];
            twiceArea += (current.X * next.Y) - (current.Y * next.X);
        }

        return twiceArea != 0;
    }

    public Bbox BoundsOf(IFigureGeometry geometry)
    {
        var star = geometry as Star5Geometry
            ?? throw new ArgumentException("Star5Shape requires Star5Geometry.", nameof(geometry));

        var minX = star.Points.Min(point => point.X);
        var minY = star.Points.Min(point => point.Y);
        var maxX = star.Points.Max(point => point.X);
        var maxY = star.Points.Max(point => point.Y);
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

        var points = Enumerable.Range(0, 10)
            .Select(index =>
            {
                var theta = (-Math.PI / 2) + (index * (Math.PI / 5));
                var scale = index % 2 == 0 ? 1.0 : DefaultInnerRatio;
                return new LocalPoint(
                    radiusX + (radiusX * scale * Math.Cos(theta)),
                    radiusY + (radiusY * scale * Math.Sin(theta)));
            })
            .ToArray();

        return new ShapePlacement(x, y, new Star5Geometry(points, DefaultInnerRatio));
    }

    private static (double X, double Y) NormaliseGesturePoint(CanvasPoint point)
    {
        // DrawGesture.cs provenance: browser circuit coordinates are untrusted, so round then clamp first.
        var x = Math.Round(point.X, MidpointRounding.AwayFromZero);
        var y = Math.Round(point.Y, MidpointRounding.AwayFromZero);
        return (Math.Clamp(x, 0, CanvasBounds.Width), Math.Clamp(y, 0, CanvasBounds.Height));
    }
}
