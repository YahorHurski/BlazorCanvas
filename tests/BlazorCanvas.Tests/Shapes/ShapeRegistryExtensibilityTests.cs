using System.Text.Json;
using BlazorCanvas.Shapes;

namespace BlazorCanvas.Tests.Shapes;

// This class uses no database fixture or Database collection: it must pass with PostgreSQL stopped.
public class ShapeRegistryExtensibilityTests
{
    [Theory]
    [InlineData(100, 100, 300, 300)]
    [InlineData(300, 300, 100, 100)]
    [InlineData(-10, 20, 100, 200)]
    [InlineData(1300, 700, 1500, 900)]
    [InlineData(20, 700, 200, 780)]
    public void Register_TestOnlyPentagon_RoundTripsThroughTheShapeDefinitionInterface(
        int pressX, int pressY, int cursorX, int cursorY)
    {
        var registry = DefaultShapes.CreateRegistry();
        registry.Register(new PentagonShape());

        Assert.Equal(5, registry.All.Count);
        Assert.Equal(new[] { "line", "rectangle", "circle", "triangle", "pentagon" }, registry.Names);

        IShapeDefinition definition = registry.Get("pentagon");
        var placement = definition.FromGesture(new CanvasPoint(pressX, pressY), new CanvasPoint(cursorX, cursorY));
        var originalBounds = definition.BoundsOf(placement.Geometry);
        var json = definition.ToJson(placement.Geometry);

        using var document = JsonDocument.Parse(json);
        Assert.True(definition.TryParseGeometry(document.RootElement, out var reparsed));
        Assert.Equal(originalBounds, definition.BoundsOf(reparsed));
        Assert.Equal(json, definition.ToJson(reparsed));
        Assert.True(definition.IsDrawable(reparsed));

        var originalPoints = ReadPoints(json);
        var reparsedPoints = ReadPoints(definition.ToJson(reparsed));
        Assert.Equal(originalPoints, reparsedPoints);
        Assert.Equal(5, originalPoints.Distinct().Count());

        var absoluteBounds = new Bbox(
            placement.X + originalBounds.X,
            placement.Y + originalBounds.Y,
            originalBounds.W,
            originalBounds.H);
        Assert.InRange(absoluteBounds.X, Math.Min(Math.Clamp(pressX, 0, 1472), Math.Clamp(cursorX, 0, 1472)), Math.Max(Math.Clamp(pressX, 0, 1472), Math.Clamp(cursorX, 0, 1472)));
        Assert.InRange(absoluteBounds.Y, Math.Min(Math.Clamp(pressY, 0, 828), Math.Clamp(cursorY, 0, 828)), Math.Max(Math.Clamp(pressY, 0, 828), Math.Clamp(cursorY, 0, 828)));
        Assert.InRange(absoluteBounds.X + absoluteBounds.W, Math.Min(Math.Clamp(pressX, 0, 1472), Math.Clamp(cursorX, 0, 1472)), Math.Max(Math.Clamp(pressX, 0, 1472), Math.Clamp(cursorX, 0, 1472)));
        Assert.InRange(absoluteBounds.Y + absoluteBounds.H, Math.Min(Math.Clamp(pressY, 0, 828), Math.Clamp(cursorY, 0, 828)), Math.Max(Math.Clamp(pressY, 0, 828), Math.Clamp(cursorY, 0, 828)));
    }

    [Fact]
    public void Register_TestOnlyPentagon_DoesNotChangeAFreshDefaultRegistryOrShippedDefinitions()
    {
        var registry = DefaultShapes.CreateRegistry();
        registry.Register(new PentagonShape());

        var fresh = DefaultShapes.CreateRegistry();
        Assert.Equal(4, fresh.All.Count);
        Assert.Equal(new[] { "line", "rectangle", "circle", "triangle" }, fresh.Names);

        AssertShippedDefinition(
            fresh.Get("line"),
            new CanvasPoint(10, 20),
            new CanvasPoint(110, 60),
            "{\"points\":[[0,0],[100,40]]}",
            new Bbox(0, 0, 100, 40));
        AssertShippedDefinition(
            fresh.Get("rectangle"),
            new CanvasPoint(10, 20),
            new CanvasPoint(110, 60),
            "{\"w\":100,\"h\":40}",
            new Bbox(0, 0, 100, 40));
        AssertShippedDefinition(
            fresh.Get("circle"),
            new CanvasPoint(300, 300),
            new CanvasPoint(350, 300),
            "{\"r\":50}",
            new Bbox(0, 0, 100, 100));
        AssertShippedDefinition(
            fresh.Get("triangle"),
            new CanvasPoint(10, 20),
            new CanvasPoint(110, 60),
            "{\"points\":[[50,0],[0,40],[100,40]]}",
            new Bbox(0, 0, 100, 40));
    }

    private static void AssertShippedDefinition(
        IShapeDefinition definition,
        CanvasPoint press,
        CanvasPoint cursor,
        string expectedJson,
        Bbox expectedBounds)
    {
        var placement = definition.FromGesture(press, cursor);
        Assert.Equal(expectedJson, definition.ToJson(placement.Geometry));
        Assert.Equal(expectedBounds, definition.BoundsOf(placement.Geometry));
    }

    private static IReadOnlyList<LocalPoint> ReadPoints(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.GetProperty("points")
            .EnumerateArray()
            .Select(point =>
            {
                var coordinates = point.EnumerateArray().ToArray();
                return new LocalPoint(coordinates[0].GetDouble(), coordinates[1].GetDouble());
            })
            .ToArray();
    }
}
