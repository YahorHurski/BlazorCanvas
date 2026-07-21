using System.Text.Json;
using BlazorCanvas.Shapes;

namespace BlazorCanvas.Tests.Shapes;

public class StyleGatewayTests
{
    [Fact]
    public void ToJson_DefaultStyle_WritesTheFixedFourKeyPayload()
    {
        Assert.Equal(
            "{\"stroke\":\"#000000\",\"stroke_width\":2,\"fill\":\"#FFFFFF\",\"opacity\":1}",
            StyleGateway.ToJson(new FigureStyle()));
    }

    [Fact]
    public void Parse_JsonWrittenFromSanitisedStyle_RoundTrips()
    {
        var style = new FigureStyle("#12ab34", 7.5, "#FFEEDD", 0.5).Sanitised();

        Assert.Equal(style, StyleGateway.Parse(StyleGateway.ToJson(style)));
    }

    // D-66 / VALID-03: the style boundary is a whitelist, not an escaping convention.
    [Theory]
    [InlineData("#abc")]
    [InlineData("#aabbccdd")]
    [InlineData("red")]
    [InlineData("000000")]
    [InlineData("#000000;")]
    [InlineData("#000000\" onload=\"alert(1)")]
    [InlineData("#000\" /><script>x</script><rect fill=\"#000")]
    [InlineData("url(#x)")]
    [InlineData(" #000000 ")]
    [InlineData("")]
    public void Sanitised_HostileStroke_ReplacesItAndNeverSerialisesIt(string hostileStroke)
    {
        var sanitised = new FigureStyle(Stroke: hostileStroke).Sanitised();

        Assert.Equal("#000000", sanitised.Stroke);
        AssertHostileValueNotSerialised(hostileStroke, StyleGateway.ToJson(sanitised));
    }

    // D-66 / VALID-03 applies equally to fill because it also reaches an SVG attribute.
    [Theory]
    [InlineData("#abc")]
    [InlineData("#aabbccdd")]
    [InlineData("red")]
    [InlineData("000000")]
    [InlineData("#000000;")]
    [InlineData("#000000\" onload=\"alert(1)")]
    [InlineData("#000\" /><script>x</script><rect fill=\"#000")]
    [InlineData("url(#x)")]
    [InlineData(" #000000 ")]
    [InlineData("")]
    public void Sanitised_HostileFill_ReplacesItAndNeverSerialisesIt(string hostileFill)
    {
        var sanitised = new FigureStyle(Fill: hostileFill).Sanitised();

        Assert.Equal("#FFFFFF", sanitised.Fill);
        AssertHostileValueNotSerialised(hostileFill, StyleGateway.ToJson(sanitised));
    }

    [Theory]
    [InlineData(0.5, 0.5)]
    [InlineData(0.49, 0.5)]
    [InlineData(0, 0.5)]
    [InlineData(-10, 0.5)]
    [InlineData(64, 64)]
    [InlineData(64.01, 64)]
    [InlineData(1000000000, 64)]
    public void Sanitised_StrokeWidthAtOrOutsideBounds_ClampsToTheAllowedRange(
        double strokeWidth, double expected)
    {
        Assert.Equal(expected, new FigureStyle(StrokeWidth: strokeWidth).Sanitised().StrokeWidth);
    }

    [Fact]
    public void Sanitised_NonFiniteStrokeWidth_ReplacesItWithTheDefaultBeforeClamping()
    {
        // D-66 / VALID-03: Math.Clamp(NaN, ...) returns NaN, so replacement must come first.
        Assert.Equal(2, new FigureStyle(StrokeWidth: double.NaN).Sanitised().StrokeWidth);
        Assert.Equal(2, new FigureStyle(StrokeWidth: double.PositiveInfinity).Sanitised().StrokeWidth);
        Assert.Equal(2, new FigureStyle(StrokeWidth: double.NegativeInfinity).Sanitised().StrokeWidth);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(-0.1, 0)]
    [InlineData(1.1, 1)]
    public void Sanitised_OpacityAtOrOutsideBounds_ClampsToTheAllowedRange(double opacity, double expected)
    {
        Assert.Equal(expected, new FigureStyle(Opacity: opacity).Sanitised().Opacity);
    }

    [Fact]
    public void Sanitised_NaNOpacity_ReplacesItWithTheDefaultBeforeClamping()
    {
        Assert.Equal(1, new FigureStyle(Opacity: double.NaN).Sanitised().Opacity);
    }

    [Fact]
    public void Parse_UnknownHostileKeys_DropsThemBeforeSerialisation()
    {
        var style = StyleGateway.Parse(
            "{\"stroke\":\"#000000\",\"onload\":\"alert(1)\",\"__proto__\":{\"x\":1},\"stroke_width\":2}");
        var json = StyleGateway.ToJson(style);

        Assert.Equal(
            "{\"stroke\":\"#000000\",\"stroke_width\":2,\"fill\":\"#FFFFFF\",\"opacity\":1}",
            json);
        Assert.DoesNotContain("onload", json);
        Assert.DoesNotContain("__proto__", json);
    }

    [Fact]
    public void ToJson_AnyInput_WritesExactlyTheFourKnownPropertiesInFixedOrder()
    {
        using var document = JsonDocument.Parse(StyleGateway.ToJson(new FigureStyle("#12ab34", 7.5, "#FFEEDD", 0.5)));

        Assert.Equal(
            new[] { "stroke", "stroke_width", "fill", "opacity" },
            document.RootElement.EnumerateObject().Select(property => property.Name));
    }

    [Theory]
    [MemberData(nameof(UnparseableStyleJson))]
    public void Parse_UnparseableOrNonObjectInput_ReturnsDefaults(string? json)
    {
        Assert.Equal(new FigureStyle(), StyleGateway.Parse(json));
    }

    [Theory]
    [InlineData("#abc", 0.49, "url(#x)", -0.1)]
    [InlineData("#000000\" onload=\"alert(1)", 1000000000, " #FFFFFF ", 1.1)]
    public void Sanitised_HostileStyle_IsIdempotent(string stroke, double strokeWidth, string fill, double opacity)
    {
        var sanitised = new FigureStyle(stroke, strokeWidth, fill, opacity).Sanitised();

        Assert.Equal(sanitised, sanitised.Sanitised());
    }

    public static IEnumerable<object?[]> UnparseableStyleJson()
    {
        yield return new object?[] { null };
        yield return new object?[] { "" };
        yield return new object?[] { "   " };
        yield return new object?[] { "not json" };
        yield return new object?[] { "[1,2,3]" };
        yield return new object?[] { "\"a string\"" };
        yield return new object?[] { "123" };
        yield return new object?[] { "{" };
        yield return new object?[] { NestedObject(40) };
    }

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
        // Substrings cannot prove this boundary: "000000" occurs inside the safe default
        // "#000000", and an empty string occurs inside every .NET string. The encoded JSON string
        // literal is the stored wire representation, so its absence proves the hostile value itself
        // was not re-emitted.
        Assert.DoesNotContain(JsonSerializer.Serialize(hostileValue), json);
    }
}
