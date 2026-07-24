using BlazorCanvas.Geometry;

namespace BlazorCanvas.Tests.Geometry;

public class CanvasCoordinatesTests
{
    [Fact]
    public void ToolbarHeight_IsFortyEight()
    {
        // The C# half of the D-43 constant. The CSS half lives in Home.razor.css's
        // `.toolbar { height: 48px }` rule — the two must never disagree.
        Assert.Equal(48, CanvasCoordinates.ToolbarHeight);
    }

    [Fact]
    public void FromPage_CanvasOrigin_MapsToZeroZero()
    {
        // The canvas sits at document position (0, 48) — D-43.
        var (x, y) = CanvasCoordinates.FromPage(0, 48);

        Assert.Equal(0, x);
        Assert.Equal(0, y);
    }

    [Fact]
    public void FromPage_InclusiveFarCorner_MapsToWidthHeight()
    {
        var (x, y) = CanvasCoordinates.FromPage(1472, 876);

        Assert.Equal(1472, x);
        Assert.Equal(828, y);
    }

    [Fact]
    public void FromPage_FractionalCoordinates_RoundToNearestInteger()
    {
        var (x, y) = CanvasCoordinates.FromPage(100.4, 148.4);

        Assert.Equal(100, x);
        Assert.Equal(100, y);
    }

    [Fact]
    public void FromPage_Midpoint_RoundsAwayFromZero()
    {
        // Rounds away from zero at the midpoint — the same convention used throughout the
        // geometry core (e.g. DrawGesture's circle radius rounding).
        var (x, y) = CanvasCoordinates.FromPage(100.5, 148.5);

        Assert.Equal(101, x);
        Assert.Equal(101, y);
    }

    [Fact]
    public void FromPage_AboveTheCanvas_MapsToNegativeY_AndIsNotClamped()
    {
        // Deliberately NOT clamped: mapping and bounding are different responsibilities, and
        // D-59 dropped the canvas-edge clamp everywhere — nothing in DrawGesture or Movement
        // clamps any more either. Pin this so nobody "helpfully" adds a clamp here and hides an
        // off-canvas press.
        var (x, y) = CanvasCoordinates.FromPage(0, 0);

        Assert.Equal(0, x);
        Assert.Equal(-48, y);
    }

    [Theory]
    [InlineData(double.NaN, double.NaN)]
    [InlineData(double.NaN, 48)]
    [InlineData(0, double.NaN)]
    [InlineData(double.PositiveInfinity, double.PositiveInfinity)]
    [InlineData(double.PositiveInfinity, 48)]
    [InlineData(0, double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity, double.NegativeInfinity)]
    [InlineData(double.NegativeInfinity, 48)]
    [InlineData(0, double.NegativeInfinity)]
    public void FromPage_NonFiniteInput_MapsToZeroZero(double pageX, double pageY)
    {
        // T-10-01: with no downstream clamp left to catch it, FromPage is the last place a
        // crafted non-finite circuit coordinate can be turned into a defined int rather than
        // reaching an unchecked (int) cast, which is undefined for NaN/Infinity in C#.
        var (x, y) = CanvasCoordinates.FromPage(pageX, pageY);

        Assert.Equal(0, x);
        Assert.Equal(0, y);
    }
}
