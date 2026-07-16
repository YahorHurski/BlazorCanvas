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
        var (x, y) = CanvasCoordinates.FromPage(1280, 768);

        Assert.Equal(1280, x);
        Assert.Equal(720, y);
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
        // Matches the rounding convention already established by CircleEncoding.ClampDrawRadius.
        var (x, y) = CanvasCoordinates.FromPage(100.5, 148.5);

        Assert.Equal(101, x);
        Assert.Equal(101, y);
    }

    [Fact]
    public void FromPage_AboveTheCanvas_MapsToNegativeY_AndIsNotClamped()
    {
        // Deliberately NOT clamped: mapping and clamping are different responsibilities.
        // The clamp belongs to DrawGesture (Task 2). Pin this so nobody "helpfully" adds a
        // clamp here and hides an off-canvas press.
        var (x, y) = CanvasCoordinates.FromPage(0, 0);

        Assert.Equal(0, x);
        Assert.Equal(-48, y);
    }
}
