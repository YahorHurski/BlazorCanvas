using System.Text.Json;
using BlazorCanvas.Shapes;

namespace BlazorCanvas.Tests.Shapes;

public class PointListPrimacyTests
{
    public static IEnumerable<object[]> RegisteredDefinitions()
    {
        var registry = CreateRegistryWithPentagon();
        foreach (var definition in registry.All)
        {
            yield return new object[] { definition.Name };
        }
    }

    [Theory]
    [MemberData(nameof(RegisteredDefinitions))]
    public void EveryRegisteredDefinition_GeometryRoundTripsWithStableBoundsAndDrawability(string name)
    {
        var definition = CreateRegistryWithPentagon().Get(name);
        var geometry = definition.FromGesture(new CanvasPoint(100, 100), new CanvasPoint(300, 220)).Geometry;
        var originalJson = definition.ToJson(geometry);

        var reparsed = Parse(definition, originalJson);

        Assert.Equal(definition.BoundsOf(geometry), definition.BoundsOf(reparsed));
        Assert.Equal(originalJson, definition.ToJson(reparsed));
        Assert.Equal(definition.IsDrawable(geometry), definition.IsDrawable(reparsed));
    }

    [Fact]
    public void Line_TwoDifferentPointListsWithTheSameBounds_SerialiseDifferently()
    {
        var definition = DefaultShapes.CreateRegistry().Get("line");
        var descending = Parse(definition, "{\"points\":[[0,0],[100,80]]}");
        var ascending = Parse(definition, "{\"points\":[[0,80],[100,0]]}");

        // If these collapse, the bbox is the source of truth again and the point list was demoted.
        Assert.Equal(new Bbox(0, 0, 100, 80), definition.BoundsOf(descending));
        Assert.Equal(definition.BoundsOf(descending), definition.BoundsOf(ascending));
        Assert.NotEqual(definition.ToJson(descending), definition.ToJson(ascending));
    }

    [Fact]
    public void Triangle_TwoDifferentPointListsWithTheSameBounds_SerialiseDifferently()
    {
        var definition = DefaultShapes.CreateRegistry().Get("triangle");
        var apexUp = Parse(definition, "{\"points\":[[50,0],[0,80],[100,80]]}");
        var apexDown = Parse(definition, "{\"points\":[[0,0],[100,0],[50,80]]}");

        // If these collapse, the bbox is the source of truth again and the point list was demoted.
        Assert.Equal(new Bbox(0, 0, 100, 80), definition.BoundsOf(apexUp));
        Assert.Equal(definition.BoundsOf(apexUp), definition.BoundsOf(apexDown));
        Assert.NotEqual(definition.ToJson(apexUp), definition.ToJson(apexDown));
    }

    [Fact]
    public void Triangle_DownwardJson_ParsesDrawsBoundsAndRoundTripsWithoutANewFormula()
    {
        IShapeDefinition triangle = DefaultShapes.CreateRegistry().Get("triangle");
        const string json = "{\"points\":[[0,0],[100,0],[50,80]]}";

        var geometry = Parse(triangle, json);

        // Different data, not different code: no formula or new type is needed for a downward triangle.
        Assert.True(triangle.IsDrawable(geometry));
        Assert.Equal(new Bbox(0, 0, 100, 80), triangle.BoundsOf(geometry));
        Assert.Equal(json, triangle.ToJson(geometry));
    }

    [Fact]
    public void Triangle_SidewaysJson_ParsesDrawsBoundsAndRoundTripsWithoutANewFormula()
    {
        IShapeDefinition triangle = DefaultShapes.CreateRegistry().Get("triangle");
        const string json = "{\"points\":[[0,0],[80,50],[0,100]]}";

        var geometry = Parse(triangle, json);

        // Different data, not different code: no formula or new type is needed for a sideways triangle.
        Assert.True(triangle.IsDrawable(geometry));
        Assert.Equal(new Bbox(0, 0, 80, 100), triangle.BoundsOf(geometry));
        Assert.Equal(json, triangle.ToJson(geometry));
    }

    [Theory]
    [InlineData("{\"points\":[[0,0],[80,50],[0,100]]}", 80, 100)]
    [InlineData("{\"points\":[[0,0],[150,0],[75,60]]}", 150, 60)]
    [InlineData("{\"points\":[[0,0],[120,10],[60,90]]}", 120, 90)]
    public void Triangle_BoundsAreAPureFunctionOfThePointList_NotStoredAlongsideIt(string changedJson, double width, double height)
    {
        var triangle = DefaultShapes.CreateRegistry().Get("triangle");
        var original = Parse(triangle, "{\"points\":[[50,0],[0,80],[100,80]]}");
        var changed = Parse(triangle, changedJson);

        var originalBounds = triangle.BoundsOf(original);
        Assert.Equal(originalBounds, triangle.BoundsOf(original));
        Assert.NotEqual(originalBounds, triangle.BoundsOf(changed));
        Assert.Equal(new Bbox(0, 0, width, height), triangle.BoundsOf(changed));
    }

    private static ShapeRegistry CreateRegistryWithPentagon()
    {
        var registry = DefaultShapes.CreateRegistry();
        registry.Register(new PentagonShape());
        return registry;
    }

    private static IFigureGeometry Parse(IShapeDefinition definition, string json)
    {
        using var document = JsonDocument.Parse(json);
        Assert.True(definition.TryParseGeometry(document.RootElement, out var geometry));
        return geometry;
    }
}
