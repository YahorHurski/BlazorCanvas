using BlazorCanvas.Geometry;

namespace BlazorCanvas.Tests.Geometry;

public class MinSizeGuardTests
{
    [Fact]
    public void Line_ZeroLength_IsRejected()
    {
        Assert.False(MinSizeGuard.IsDrawable(FigureType.Line, new Box(10, 10, 10, 10)));
    }

    [Fact]
    public void Line_Horizontal_IsAccepted()
    {
        Assert.True(MinSizeGuard.IsDrawable(FigureType.Line, new Box(10, 10, 90, 10)));
    }

    [Fact]
    public void Line_Vertical_IsAccepted()
    {
        Assert.True(MinSizeGuard.IsDrawable(FigureType.Line, new Box(10, 10, 10, 90)));
    }

    [Fact]
    public void Rectangle_ZeroHeight_IsRejected()
    {
        Assert.False(MinSizeGuard.IsDrawable(FigureType.Rectangle, new Box(10, 10, 90, 10)));
    }

    [Fact]
    public void Rectangle_ZeroWidth_IsRejected()
    {
        Assert.False(MinSizeGuard.IsDrawable(FigureType.Rectangle, new Box(10, 10, 10, 90)));
    }

    [Fact]
    public void Rectangle_PositiveWidthAndHeight_IsAccepted()
    {
        Assert.True(MinSizeGuard.IsDrawable(FigureType.Rectangle, new Box(10, 10, 90, 90)));
    }

    [Fact]
    public void Triangle_ZeroHeight_IsRejected()
    {
        Assert.False(MinSizeGuard.IsDrawable(FigureType.Triangle, new Box(10, 10, 90, 10)));
    }

    [Fact]
    public void Triangle_ZeroWidth_IsRejected()
    {
        Assert.False(MinSizeGuard.IsDrawable(FigureType.Triangle, new Box(10, 10, 10, 90)));
    }

    [Fact]
    public void Triangle_PositiveWidthAndHeight_IsAccepted()
    {
        Assert.True(MinSizeGuard.IsDrawable(FigureType.Triangle, new Box(10, 10, 90, 90)));
    }

    [Fact]
    public void Circle_SquarePositiveEvenSide_IsAccepted()
    {
        Assert.True(MinSizeGuard.IsDrawable(FigureType.Circle, new Box(0, 0, 10, 10)));
    }

    [Fact]
    public void Circle_OddSide_IsRejected()
    {
        Assert.False(MinSizeGuard.IsDrawable(FigureType.Circle, new Box(0, 0, 9, 9)));
    }

    [Fact]
    public void Circle_NotSquare_IsRejected()
    {
        Assert.False(MinSizeGuard.IsDrawable(FigureType.Circle, new Box(0, 0, 10, 8)));
    }

    [Fact]
    public void Circle_ZeroRadius_IsRejected()
    {
        Assert.False(MinSizeGuard.IsDrawable(FigureType.Circle, new Box(0, 0, 0, 0)));
    }

    [Fact]
    public void PerTypeGuard_HorizontalLineLegal_ButZeroHeightRectangleIllegal()
    {
        // This pair proves the guard is per-type (D-50), not shared (D-23, retracted):
        // a shared guard cannot accept a horizontal line while also rejecting a zero-height
        // rectangle.
        Assert.True(MinSizeGuard.IsDrawable(FigureType.Line, new Box(10, 10, 90, 10)));
        Assert.False(MinSizeGuard.IsDrawable(FigureType.Rectangle, new Box(10, 10, 90, 10)));
    }
}
