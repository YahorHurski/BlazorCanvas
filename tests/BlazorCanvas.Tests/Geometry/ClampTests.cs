using BlazorCanvas.Geometry;

namespace BlazorCanvas.Tests.Geometry;

public class ClampTests
{
    [Fact]
    public void FlushRightEdge_XClippedToZero_YPassesThroughAtFullDelta()
    {
        // Mandated test 1 of TEST-01 (D-36): a figure pinned to the right edge still slides
        // freely up and down. dx' must never read y.
        var result = Movement.ClampMove(new Box(1180, 300, 1280, 400), dx: 50, dy: -30);

        Assert.Equal(new Box(1180, 270, 1280, 370), result);
    }

    [Fact]
    public void FlushBottomEdge_YClippedToZero_XPassesThroughAtFullDelta()
    {
        var result = Movement.ClampMove(new Box(300, 620, 400, 720), dx: 40, dy: 60);

        Assert.Equal(new Box(340, 620, 440, 720), result);
    }

    [Fact]
    public void AlreadyTouchingBothMaxima_DoesNotMoveAtAll()
    {
        // X2 == 1280 and Y2 == 720 are legal, reachable positions, not overflow (D-19).
        var result = Movement.ClampMove(new Box(1200, 600, 1280, 720), dx: 1, dy: 1);

        Assert.Equal(new Box(1200, 600, 1280, 720), result);
    }

    [Fact]
    public void LargeDownwardDelta_LandsExactlyAtY2Equals720()
    {
        var result = Movement.ClampMove(new Box(1180, 600, 1280, 700), dx: 0, dy: 100);

        Assert.Equal(new Box(1180, 620, 1280, 720), result);
        Assert.Equal(720, result.Y2);
    }

    [Fact]
    public void AlreadyTouchingBothMinima_DoesNotMoveAtAll()
    {
        // X1 == 0 and Y1 == 0 are legal (D-19).
        var result = Movement.ClampMove(new Box(0, 0, 50, 50), dx: -1, dy: -1);

        Assert.Equal(new Box(0, 0, 50, 50), result);
    }

    [Fact]
    public void LargeLeftwardDelta_LandsExactlyAtX1EqualsZero_ClippedNotRejected()
    {
        var result = Movement.ClampMove(new Box(10, 10, 60, 60), dx: -100, dy: 0);

        Assert.Equal(new Box(0, 10, 50, 60), result);
        Assert.Equal(0, result.X1);
    }

    [Fact]
    public void LineWithY1GreaterThanY2_ClampsAgainstTheMinMaxBoundingBox()
    {
        // A normalised line may legally have Y1 > Y2 (D-41). The clamp must compute the
        // bounding box (min/max) rather than trust Y1 <= Y2, or every down-and-right line
        // mis-clamps.
        var result = Movement.ClampMove(new Box(0, 700, 100, 100), dx: 0, dy: 100);

        Assert.Equal(new Box(0, 720, 100, 120), result);
    }

    public static IEnumerable<object[]> ShapeInvarianceCases()
    {
        yield return new object[] { new Box(1180, 300, 1280, 400), 50, -30 };
        yield return new object[] { new Box(300, 620, 400, 720), 40, 60 };
        yield return new object[] { new Box(1200, 600, 1280, 720), 1, 1 };
        yield return new object[] { new Box(1180, 600, 1280, 700), 0, 100 };
        yield return new object[] { new Box(0, 0, 50, 50), -1, -1 };
        yield return new object[] { new Box(10, 10, 60, 60), -100, 0 };
        yield return new object[] { new Box(0, 700, 100, 100), 0, 100 };
        // Deltas far outside the canvas in every direction.
        yield return new object[] { new Box(500, 500, 600, 600), 100_000, 100_000 };
        yield return new object[] { new Box(500, 500, 600, 600), -100_000, -100_000 };
        yield return new object[] { new Box(0, 0, 1280, 720), 0, 0 };
    }

    [Theory]
    [MemberData(nameof(ShapeInvarianceCases))]
    public void WidthAndHeight_AreUnchangedByEveryClampMove(Box input, int dx, int dy)
    {
        var result = Movement.ClampMove(input, dx, dy);

        Assert.Equal(input.Width, result.Width);
        Assert.Equal(input.Height, result.Height);
    }

    [Fact]
    public void ClampDelta_ClampsValueBetweenLoAndHi()
    {
        Assert.Equal(5, Movement.ClampDelta(5, -10, 10));
        Assert.Equal(-10, Movement.ClampDelta(-50, -10, 10));
        Assert.Equal(10, Movement.ClampDelta(50, -10, 10));
    }

    [Fact]
    public void ClampMove_OversizedWidthBox_ZeroDelta_IsIdentity()
    {
        // CR-02: width 2000 exceeds the canvas width of 1280, so lo (-bx1) > hi (W - bx2).
        // Unguarded, ClampDelta silently inverted and returned a nonzero delta for a
        // zero-input delta, teleporting the figure. A box that cannot fit must not move.
        var result = Movement.ClampMove(new Box(0, 0, 2000, 100), dx: 0, dy: 0);

        Assert.Equal(new Box(0, 0, 2000, 100), result);
    }

    [Fact]
    public void ClampMove_OversizedHeightBox_ZeroDelta_IsIdentity()
    {
        // CR-02 y-axis mirror: height 900 exceeds the canvas height of 720.
        var result = Movement.ClampMove(new Box(0, 0, 100, 900), dx: 0, dy: 0);

        Assert.Equal(new Box(0, 0, 100, 900), result);
    }

    [Fact]
    public void ClampDelta_WhenLoGreaterThanHi_ReturnsZero()
    {
        Assert.Equal(0, Movement.ClampDelta(0, 0, -720));
        Assert.Equal(0, Movement.ClampDelta(50, 0, -720));
        Assert.Equal(0, Movement.ClampDelta(-50, 0, -720));
    }
}
