using System.Text.Json;
using BlazorCanvas.Shapes;

namespace BlazorCanvas.Tests.Shapes;

public class DefaultShapesTests
{
    [Fact]
    public void CreateRegistry_RegistersCanonicalNamesInSeedOrder()
    {
        Assert.Equal(new[] { "line", "rectangle", "circle", "triangle", "star5" }, DefaultShapes.CreateRegistry().Names);
    }

    [Fact]
    public void CreateRegistry_DefinitionsHaveUniqueLowercaseAsciiIdentifierNames()
    {
        var definitions = DefaultShapes.CreateRegistry().All;
        Assert.All(definitions, definition =>
        {
            Assert.False(string.IsNullOrWhiteSpace(definition.Name));
            Assert.All(definition.Name, character => Assert.True(
                character is >= 'a' and <= 'z' or >= '0' and <= '9',
                $"'{definition.Name}' contains unsupported character '{character}'."));
        });
        Assert.Equal(definitions.Count, definitions.Select(definition => definition.Name).Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void CreateRegistry_EachCallReturnsAnIndependentRegistry()
    {
        var first = DefaultShapes.CreateRegistry();
        var second = DefaultShapes.CreateRegistry();
        Assert.NotSame(first, second);
        first.Register(new TestOnlyLineShape());
        Assert.True(first.Contains("test-only"));
        Assert.False(second.Contains("test-only"));
    }

    [Fact]
    public void CreateRegistry_EachNameResolvesToTheSameDefinitionName()
    {
        var registry = DefaultShapes.CreateRegistry();
        foreach (var name in registry.Names)
        {
            Assert.Equal(name, registry.Get(name).Name);
        }
    }

    [Fact]
    public void CreateRegistry_EachDefinitionRoundTripsCanonicalGeometryWithStableBounds()
    {
        var registry = DefaultShapes.CreateRegistry();
        var gestures = new Dictionary<string, (CanvasPoint Press, CanvasPoint Cursor)>
        {
            ["line"] = (new CanvasPoint(10, 20), new CanvasPoint(110, 60)),
            ["rectangle"] = (new CanvasPoint(10, 20), new CanvasPoint(110, 60)),
            ["circle"] = (new CanvasPoint(300, 300), new CanvasPoint(350, 300)),
            ["triangle"] = (new CanvasPoint(10, 20), new CanvasPoint(110, 60)),
            ["star5"] = (new CanvasPoint(10, 20), new CanvasPoint(110, 60))
        };

        foreach (var name in registry.Names)
        {
            var definition = registry.Get(name);
            var placement = definition.FromGesture(gestures[name].Press, gestures[name].Cursor);
            var json = definition.ToJson(placement.Geometry);
            using var document = JsonDocument.Parse(json);
            Assert.True(definition.TryParseGeometry(document.RootElement, out var parsed));
            Assert.Equal(definition.BoundsOf(placement.Geometry), definition.BoundsOf(parsed));
        }
    }

    private sealed class TestOnlyLineShape : IShapeDefinition
    {
        private readonly LineShape _inner = new();

        public string Name => "test-only";

        public bool TryParseGeometry(JsonElement json, out IFigureGeometry geometry) => _inner.TryParseGeometry(json, out geometry);

        public string ToJson(IFigureGeometry geometry) => _inner.ToJson(geometry);

        public bool IsDrawable(IFigureGeometry geometry) => _inner.IsDrawable(geometry);

        public Bbox BoundsOf(IFigureGeometry geometry) => _inner.BoundsOf(geometry);

        public ShapePlacement FromGesture(CanvasPoint press, CanvasPoint cursor) => _inner.FromGesture(press, cursor);
    }
}
