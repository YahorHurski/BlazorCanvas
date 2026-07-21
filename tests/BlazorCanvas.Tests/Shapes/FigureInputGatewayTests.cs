using System.Reflection;
using System.Text.Json;
using BlazorCanvas.Shapes;

namespace BlazorCanvas.Tests.Shapes;

public class FigureInputGatewayTests
{
    // VALID-02 / ROADMAP criterion 4: rejected geometry never reaches BoundsOf.
    [Theory]
    [MemberData(nameof(HostileGeometryCases))]
    public void TryValidate_HostileGeometry_ReturnsFalseWithNoResult(string? typeName, string? geometryJson)
    {
        var accepted = CreateGateway().TryValidate(typeName, geometryJson, null, out var result);

        Assert.False(accepted);
        Assert.Null(result);
    }

    // VALID-03: styles are a sanitising boundary, not a second reason to drop a figure.
    [Theory]
    [MemberData(nameof(HostileStyleCases))]
    public void TryValidate_HostileStyle_ClampsAndDoesNotReEmitTheHostileValue(
        string hostileStyleJson,
        string hostileValue,
        FigureStyle expected)
    {
        var accepted = CreateGateway().TryValidate("circle", "{\"r\":50}", hostileStyleJson, out var result);

        Assert.True(accepted);
        Assert.NotNull(result);
        Assert.Equal(expected, result.Style);
        AssertHostileValueNotSerialised(hostileValue, result.StyleJson);
        using var document = JsonDocument.Parse(result.StyleJson);
        Assert.Equal(
            new[] { "stroke", "stroke_width", "fill", "opacity" },
            document.RootElement.EnumerateObject().Select(property => property.Name));
    }

    // VALID-01: unknown client content is not carried across the typed-record boundary.
    [Theory]
    [MemberData(nameof(HostileExtraContentCases))]
    public void TryValidate_ValidGeometryWithHostileExtraContent_ReturnsOnlyCanonicalGeometry(
        string typeName,
        string geometryJson,
        string expectedJson,
        string[] hostileSubstrings)
    {
        var accepted = CreateGateway().TryValidate(
            typeName,
            geometryJson,
            "{\"stroke\":\"#000000\\\" onload=\\\"alert(1)\",\"extra\":\"<script>x</script>\"}",
            out var result);

        Assert.True(accepted);
        Assert.NotNull(result);
        Assert.Equal(expectedJson, result.GeometryJson);
        foreach (var hostile in hostileSubstrings)
        {
            Assert.DoesNotContain(hostile, result.GeometryJson);
            Assert.DoesNotContain(hostile, result.StyleJson);
        }
    }

    [Fact]
    public void TryValidate_UsesRegistryTypeLiteral_AndDoesNotNormaliseNames()
    {
        var registry = DefaultShapes.CreateRegistry();
        var gateway = new FigureInputGateway(registry);

        Assert.True(gateway.TryValidate("circle", "{\"r\":50}", null, out var result));
        Assert.NotNull(result);
        Assert.Equal(registry.Get("circle").Name, result.Type);

        Assert.False(gateway.TryValidate("Circle", "{\"r\":50}", null, out _));
        Assert.False(gateway.TryValidate(" circle ", "{\"r\":50}", null, out _));
    }

    [Theory]
    [MemberData(nameof(ValidGeometryCases))]
    public void TryValidate_ValidGeometry_UsesDefinitionBoundsExactly(string typeName, string geometryJson)
    {
        var registry = DefaultShapes.CreateRegistry();
        var gateway = new FigureInputGateway(registry);

        Assert.True(gateway.TryValidate(typeName, geometryJson, null, out var result));
        Assert.NotNull(result);
        Assert.Equal(registry.Get(typeName).BoundsOf(result.Geometry), result.Bounds);
        Assert.True(double.IsFinite(result.Bounds.W));
        Assert.True(double.IsFinite(result.Bounds.H));
        Assert.True(result.Bounds.W >= 0);
        Assert.True(result.Bounds.H >= 0);
    }

    // A line's zero-height or zero-width bounding box is legal when its endpoints differ.
    [Theory]
    [InlineData("{\"points\":[[0,10],[100,10]]}", 0d, 100d)]
    [InlineData("{\"points\":[[10,0],[10,100]]}", 100d, 0d)]
    public void TryValidate_HorizontalAndVerticalLines_AreLegal(string geometryJson, double expectedHeight, double expectedWidth)
    {
        Assert.True(CreateGateway().TryValidate("line", geometryJson, null, out var result));
        Assert.NotNull(result);
        Assert.Equal(expectedHeight, result.Bounds.H);
        Assert.Equal(expectedWidth, result.Bounds.W);
    }

    // Compatibility floor: these are the four documented v1.1 box-to-geometry conversions.
    [Theory]
    [MemberData(nameof(V11LegalConversionCases))]
    public void TryValidate_V11LegalConvertedGeometry_IsNeverRejected(string typeName, string geometryJson)
    {
        Assert.True(CreateGateway().TryValidate(typeName, geometryJson, null, out var result));
        Assert.NotNull(result);
    }

    // Silence is intentional: no exception escapes, and no public failure-reason surface exists.
    [Fact]
    public void TryValidate_EveryHostileCaseIsSilent_AndGatewayExposesNoFailureReason()
    {
        var gateway = CreateGateway();

        foreach (var item in HostileGeometryCases())
        {
            var typeName = (string?)item[0];
            var geometryJson = (string?)item[1];
            var exception = Record.Exception(() => gateway.TryValidate(typeName, geometryJson, null, out _));
            Assert.Null(exception);
        }

        var stringLeak = typeof(FigureInputGateway)
            .GetMembers(BindingFlags.Instance | BindingFlags.Public)
            .OfType<MethodInfo>()
            .Where(method => method.DeclaringType == typeof(FigureInputGateway))
            .Any(method => method.ReturnType == typeof(string)
                || method.GetParameters().Any(parameter => parameter.IsOut && parameter.ParameterType == typeof(string).MakeByRefType()));
        Assert.False(stringLeak);
    }

    [Theory]
    [MemberData(nameof(GestureCases))]
    public void TryValidateGesture_MatchesTheSharedValidationPath(
        string typeName,
        CanvasPoint press,
        CanvasPoint cursor,
        string styleJson)
    {
        var registry = DefaultShapes.CreateRegistry();
        var definition = registry.Get(typeName);
        var placement = definition.FromGesture(press, cursor);
        var gateway = new FigureInputGateway(registry);

        Assert.True(gateway.TryValidateGesture(typeName, press, cursor, styleJson, out var fromGesture, out var x, out var y));
        Assert.True(gateway.TryValidate(typeName, definition.ToJson(placement.Geometry), styleJson, out var fromJson));
        Assert.NotNull(fromGesture);
        Assert.NotNull(fromJson);
        Assert.Equal(placement.X, x);
        Assert.Equal(placement.Y, y);
        Assert.Equal(fromJson.GeometryJson, fromGesture.GeometryJson);
        Assert.Equal(fromJson.StyleJson, fromGesture.StyleJson);
        Assert.Equal(fromJson.Bounds, fromGesture.Bounds);
    }

    public static IEnumerable<object?[]> HostileGeometryCases()
    {
        foreach (var json in new[] { "{\"r\":-5}", "{\"r\":0}", "{\"r\":null}", "{\"r\":\"50\"}", "{}", "{\"radius\":50}" })
            yield return new object?[] { "circle", json };
        foreach (var json in new[] { "{\"w\":0,\"h\":100}", "{\"w\":100,\"h\":0}", "{\"w\":-200,\"h\":100}", "{\"w\":1e400,\"h\":10}" })
            yield return new object?[] { "rectangle", json };
        foreach (var json in new[] { "{\"points\":[[10,10],[10,10]]}", "{\"points\":[]}", "{\"points\":[[0,0]]}", "{\"points\":[[0,0],[1,1],[2,2]]}", "{\"points\":[[0,\"x\"],[1,1]]}" })
            yield return new object?[] { "line", json };
        foreach (var json in new[] { "{\"points\":[[0,0],[0,0],[0,0]]}", "{\"points\":[[0,0],[1,1],[0,0]]}", "{\"points\":[[0,0],[50,40],[100,80]]}", "{\"points\":[[0,0],[1,1]]}", "{\"points\":[[0,0],[1,1],[2,2],[3,3]]}" })
            yield return new object?[] { "triangle", json };
        foreach (var json in new[] { "[1,2,3]", "\"text\"", "42", "null", "", "   ", "{", "{\"w\":1,}", "{/*c*/\"w\":1,\"h\":1}", NestedObject(40) })
            yield return new object?[] { "circle", json };
        foreach (var name in new string?[] { "hexagon", "Circle", "", "   ", null })
            yield return new object?[] { name, "{\"r\":50}" };
    }

    public static IEnumerable<object[]> HostileStyleCases()
    {
        yield return StyleCase("{\"stroke\":\"red\"}", "red", new FigureStyle());
        yield return StyleCase("{\"stroke\":\"#abc\"}", "#abc", new FigureStyle());
        yield return StyleCase("{\"fill\":\"#000000\\\" onload=\\\"alert(1)\"}", "#000000\" onload=\"alert(1)", new FigureStyle());
        yield return StyleCase("{\"fill\":\"<script>x</script>\"}", "<script>x</script>", new FigureStyle());
        yield return StyleCase("{\"stroke_width\":0}", "0", new FigureStyle(StrokeWidth: 0.5));
        yield return StyleCase("{\"stroke_width\":1000000000}", "1000000000", new FigureStyle(StrokeWidth: 64));
        yield return StyleCase("{\"stroke_width\":1e400}", "1e400", new FigureStyle());
        yield return StyleCase("{\"unknown\":\"hostile-extra\"}", "hostile-extra", new FigureStyle());
    }

    public static IEnumerable<object[]> HostileExtraContentCases()
    {
        yield return new object[]
        {
            "circle", "{\"r\":50,\"onload\":\"alert(1)\",\"__proto__\":{\"x\":1},\"note\":\"<script>x</script>\"}",
            "{\"r\":50}", new[] { "onload", "__proto__", "alert(1)", "<script>" },
        };
        yield return new object[]
        {
            "line", "{\"points\":[[0,0],[100,40]],\"style\":\"hostile-line-style\"}",
            "{\"points\":[[0,0],[100,40]]}", new[] { "style", "hostile-line-style" },
        };
        yield return new object[]
        {
            "rectangle", "{\"w\":200,\"h\":100,\"points\":\"hostile-points\"}",
            "{\"w\":200,\"h\":100}", new[] { "points", "hostile-points" },
        };
    }

    public static IEnumerable<object[]> ValidGeometryCases()
    {
        yield return new object[] { "line", "{\"points\":[[0,0],[100,40]]}" };
        yield return new object[] { "rectangle", "{\"w\":200,\"h\":100}" };
        yield return new object[] { "circle", "{\"r\":50}" };
        yield return new object[] { "triangle", "{\"points\":[[50,0],[0,80],[100,80]]}" };
    }

    public static IEnumerable<object[]> V11LegalConversionCases()
    {
        yield return new object[] { "line", "{\"points\":[[0,0],[100,0]]}" };
        yield return new object[] { "line", "{\"points\":[[0,0],[0,100]]}" };
        yield return new object[] { "line", "{\"points\":[[0,100],[100,0]]}" };
        yield return new object[] { "line", "{\"points\":[[0,0],[100,100]]}" };
        yield return new object[] { "rectangle", "{\"w\":1,\"h\":1}" };
        yield return new object[] { "circle", "{\"r\":1}" };
        yield return new object[] { "triangle", "{\"points\":[[0.5,0],[0,1],[1,1]]}" };
    }

    public static IEnumerable<object[]> GestureCases()
    {
        yield return new object[] { "line", new CanvasPoint(10, 100), new CanvasPoint(110, 20), "{\"stroke\":\"#12ab34\"}" };
        yield return new object[] { "rectangle", new CanvasPoint(100, 50), new CanvasPoint(300, 250), "{\"opacity\":0.5}" };
        yield return new object[] { "circle", new CanvasPoint(400, 350), new CanvasPoint(500, 350), "{\"fill\":\"#FFEEDD\"}" };
        yield return new object[] { "triangle", new CanvasPoint(300, 200), new CanvasPoint(500, 450), "{\"stroke_width\":7.5}" };
    }

    private static FigureInputGateway CreateGateway() => new(DefaultShapes.CreateRegistry());

    private static object[] StyleCase(string json, string hostileValue, FigureStyle expected) => [json, hostileValue, expected];

    private static string NestedObject(int depth)
    {
        var json = "0";
        for (var level = 0; level < depth; level++)
        {
            json = $"{{\"x\":{json}}}";
        }

        return json;
    }

    private static void AssertHostileValueNotSerialised(string hostileValue, string json)
    {
        // Complete JSON values avoid false positives from the safe defaults and empty substrings.
        Assert.DoesNotContain(JsonSerializer.Serialize(hostileValue), json);
    }
}
