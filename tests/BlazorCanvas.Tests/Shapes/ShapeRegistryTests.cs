using System.Text.Json;
using BlazorCanvas.Shapes;

namespace BlazorCanvas.Tests.Shapes;

public class ShapeRegistryTests
{
    public static IEnumerable<object?[]> RejectedNames()
    {
        yield return new object?[] { null };
        yield return new object?[] { string.Empty };
        yield return new object?[] { "   " };
        yield return new object?[] { "hexagon" };
        yield return new object?[] { "Line" };
    }

    [Fact]
    public void Register_DuplicateName_ThrowsAndKeepsFirstDefinition()
    {
        var registry = new ShapeRegistry();
        var first = new TestShapeDefinition("line");
        var duplicate = new TestShapeDefinition("line");
        registry.Register(first);

        Assert.Throws<ArgumentException>(() => registry.Register(duplicate));
        Assert.Single(registry.All);
        Assert.Same(first, registry.All[0]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Register_BlankName_ThrowsArgumentException(string? name)
    {
        Assert.Throws<ArgumentException>(() => new ShapeRegistry().Register(new TestShapeDefinition(name!)));
    }

    [Theory]
    [MemberData(nameof(RejectedNames))]
    public void TryGet_RejectedName_ReturnsFalseAndNoDefinition(string? name)
    {
        var registry = RegistryWithLine();

        Assert.False(registry.TryGet(name, out var definition));
        Assert.Null(definition);
    }

    [Theory]
    [MemberData(nameof(RejectedNames))]
    public void Get_RejectedName_ThrowsKeyNotFoundException(string? name)
    {
        Assert.Throws<KeyNotFoundException>(() => RegistryWithLine().Get(name));
    }

    [Fact]
    public void AllAndNames_RegisteredDefinitions_ReturnInStableRegistrationOrder()
    {
        var registry = new ShapeRegistry();
        foreach (var name in new[] { "line", "rectangle", "circle", "triangle" })
        {
            registry.Register(new TestShapeDefinition(name));
        }

        var expected = new[] { "line", "rectangle", "circle", "triangle" };
        Assert.Equal(expected, registry.All.Select(definition => definition.Name));
        Assert.Equal(expected, registry.Names);
        Assert.Equal(registry.All.Select(definition => definition.Name), registry.All.Select(definition => definition.Name));
        Assert.Equal(registry.Names, registry.Names);
    }

    private static ShapeRegistry RegistryWithLine()
    {
        var registry = new ShapeRegistry();
        registry.Register(new TestShapeDefinition("line"));
        return registry;
    }

    private sealed class TestShapeDefinition(string name) : IShapeDefinition
    {
        public string Name { get; } = name;

        public bool TryParseGeometry(JsonElement json, out IFigureGeometry geometry)
        {
            geometry = null!;
            throw new NotSupportedException();
        }

        public string ToJson(IFigureGeometry geometry) => throw new NotSupportedException();

        public bool IsDrawable(IFigureGeometry geometry) => throw new NotSupportedException();

        public Bbox BoundsOf(IFigureGeometry geometry) => throw new NotSupportedException();

        public ShapePlacement FromGesture(CanvasPoint press, CanvasPoint cursor) => throw new NotSupportedException();
    }
}
