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
    public void Circle_OddSide_IsAccepted()
    {
        // The even-side rule mirrored the circle_is_a_circle CHECK that D-59 deleted; it is gone
        // (D-59 item 9). Only a strictly-zero radius is rejected now.
        Assert.True(MinSizeGuard.IsDrawable(FigureType.Circle, new Box(0, 0, 9, 9)));
    }

    [Fact]
    public void Circle_NotSquare_IsAccepted()
    {
        // The square-ness rule also mirrored the deleted circle_is_a_circle CHECK; it is gone
        // (D-59 item 9). DrawGesture/CircleEncoding never produce a non-square box in practice —
        // this pins that the guard itself no longer enforces it.
        Assert.True(MinSizeGuard.IsDrawable(FigureType.Circle, new Box(0, 0, 10, 8)));
    }

    [Fact]
    public void Circle_ZeroRadius_IsRejected()
    {
        Assert.False(MinSizeGuard.IsDrawable(FigureType.Circle, new Box(0, 0, 0, 0)));
    }

    [Fact]
    public void Circle_WidthRoundsRadiusToZero_IsRejected()
    {
        // T-10-02: a width-1 box would serialise as {"r":0} — an invisible, unselectable poison
        // row. The guard reads the radius the same way GeometryCodec.Encode reads it, so a naive
        // width-greater-than-zero rule can never let this slip through.
        Assert.False(MinSizeGuard.IsDrawable(FigureType.Circle, new Box(10, 10, 11, 11)));
    }

    public static IEnumerable<object[]> ZeroAndOnePixelExtentCases()
    {
        // (type, exactly-zero-extent box, one-step-beyond box that must be accepted)
        yield return new object[] { FigureType.Line, new Box(10, 10, 10, 10), new Box(10, 10, 11, 10) };
        yield return new object[] { FigureType.Rectangle, new Box(10, 10, 10, 10), new Box(10, 10, 11, 11) };
        yield return new object[] { FigureType.Triangle, new Box(10, 10, 10, 10), new Box(10, 10, 11, 11) };
        yield return new object[] { FigureType.Circle, new Box(10, 10, 10, 10), new Box(10, 10, 12, 12) };
    }

    [Theory]
    [MemberData(nameof(ZeroAndOnePixelExtentCases))]
    public void PerType_ExactlyZeroExtent_IsRejected_OneStepBeyond_IsAccepted(
        FigureType type, Box zeroExtent, Box oneStepBeyond)
    {
        // STOR-03 boundary probe: exactly-zero extent rejected, one step either side accepted,
        // for every type (D-50).
        Assert.False(MinSizeGuard.IsDrawable(type, zeroExtent));
        Assert.True(MinSizeGuard.IsDrawable(type, oneStepBeyond));
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
