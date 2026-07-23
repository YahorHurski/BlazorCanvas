using System.Globalization;
using System.Text.Json;
using BlazorCanvas.Shapes;

namespace BlazorCanvas.Tests.Shapes;

public class GeometryModelTests
{
    public static IEnumerable<object[]> InvalidNumberProperties()
    {
        yield return new object[] { "{}" };
        yield return new object[] { "{\"value\":\"50\"}" };
        yield return new object[] { "{\"value\":null}" };
        yield return new object[] { "{\"value\":true}" };
        yield return new object[] { "{\"value\":{}}" };
        yield return new object[] { "{\"value\":1e400}" };
    }

    [Theory]
    [MemberData(nameof(InvalidNumberProperties))]
    public void TryReadFiniteDouble_InvalidProperty_ReturnsFalse(string json)
    {
        Assert.False(GeometryJson.TryReadFiniteDouble(Root(json), "value", out _));
    }

    [Theory]
    [InlineData("{\"value\":50}", 50)]
    [InlineData("{\"value\":-12.5}", -12.5)]
    [InlineData("{\"value\":0}", 0)]
    public void TryReadFiniteDouble_FiniteNumber_ReturnsExactValue(string json, double expected)
    {
        Assert.True(GeometryJson.TryReadFiniteDouble(Root(json), "value", out var actual));
        Assert.Equal(expected, actual);
    }

    public static IEnumerable<object[]> InvalidPointLists()
    {
        yield return new object[] { "{}" };
        yield return new object[] { "{\"points\":[]}" };
        yield return new object[] { "{\"points\":[[0,0]]}" };
        yield return new object[] { "{\"points\":[[0,0],[1,1],[2,2]]}" };
        yield return new object[] { "{\"points\":[[0],[1,1]]}" };
        yield return new object[] { "{\"points\":[[0,\"a\"],[1,1]]}" };
        yield return new object[] { "{\"points\":{}}" };
    }

    [Theory]
    [MemberData(nameof(InvalidPointLists))]
    public void TryReadPointList_StructurallyInvalidInput_ReturnsFalse(string json)
    {
        Assert.False(GeometryJson.TryReadPointList(Root(json), "points", 2, out _));
    }

    [Fact]
    public void TryReadPointList_ExactFinitePoints_ReturnsPointsInOrder()
    {
        Assert.True(GeometryJson.TryReadPointList(Root("{\"points\":[[0,0],[100,40]]}"), "points", 2, out var points));
        Assert.Equal(new[] { new LocalPoint(0, 0), new LocalPoint(100, 40) }, points);
    }

    [Fact]
    public void TryReadPointList_DifferentOrderedPoints_PreservesSequence()
    {
        Assert.True(GeometryJson.TryReadPointList(Root("{\"points\":[[10,20],[30,40]]}"), "points", 2, out var points));
        Assert.Equal(new[] { new LocalPoint(10, 20), new LocalPoint(30, 40) }, points);
    }

    [Fact]
    public void Serialise_WriteNumber_ProducesMinifiedObject()
    {
        var result = GeometryJson.Serialise(writer =>
        {
            writer.WriteStartObject();
            GeometryJson.WriteNumber(writer, "w", 200);
            GeometryJson.WriteNumber(writer, "h", 100);
            writer.WriteEndObject();
        });

        Assert.Equal("{\"w\":200,\"h\":100}", result);
    }

    [Fact]
    public void Serialise_WritePoints_ProducesOrderedPointArray()
    {
        var result = GeometryJson.Serialise(writer =>
        {
            writer.WriteStartObject();
            GeometryJson.WritePoints(writer, "points", new[] { new LocalPoint(0, 0), new LocalPoint(100, 40) });
            writer.WriteEndObject();
        });

        Assert.Equal("{\"points\":[[0,0],[100,40]]}", result);
    }

    [Fact]
    public void Serialise_CommaDecimalCulture_UsesPeriodDecimalSeparator()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUiCulture = CultureInfo.CurrentUICulture;

        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("ru-RU");
            CultureInfo.CurrentUICulture = new CultureInfo("ru-RU");

            var result = GeometryJson.Serialise(writer =>
            {
                writer.WriteStartObject();
                GeometryJson.WriteNumber(writer, "value", 13.5);
                writer.WriteEndObject();
            });

            Assert.Equal("{\"value\":13.5}", result);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }

    private static JsonElement Root(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}
