using BlazorCanvas.Data.V11;
using BlazorCanvas.Geometry;

namespace BlazorCanvas.Tests.Geometry;

public class V11MovementTests
{
    [Theory]
    [InlineData(-100, -100, 0, 0)]
    [InlineData(2000, 2000, 1462, 818)]
    [InlineData(12.5, 13.5, 12.5, 13.5)]
    public void ClampPosition_UsesLocalBboxAndPreservesDecimalPosition(decimal x, decimal y, decimal expectedX, decimal expectedY)
    {
        var result = V11Movement.ClampPosition(Row(0, 0, 10, 10), x, y);
        Assert.Equal((expectedX, expectedY), result);
    }

    [Fact]
    public void ClampPosition_AllowsZeroExtentLinesAtBothEdges()
    {
        Assert.Equal((1472m, 828m), V11Movement.ClampPosition(Row(0, 0, 0, 0), 1472, 828));
    }

    [Fact]
    public void ClampPosition_DoesNotTeleportOversizeGeometry()
    {
        var row = Row(0, 0, 2000, 10) with { X = 9m, Y = 11m };
        Assert.Equal((9m, 20m), V11Movement.ClampPosition(row, 100m, 20m));
    }

    private static FigureRow Row(double bx, double by, double bw, double bh) => new(Guid.NewGuid(), Guid.NewGuid(), "line", 0, 0, 0,
        "{\"points\":[{\"x\":0,\"y\":0},{\"x\":0,\"y\":0}]}", "{}", 1, bx, by, bw, bh);
}
