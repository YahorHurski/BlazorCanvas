using BlazorCanvas.Geometry;

namespace BlazorCanvas.Tests.Geometry;

public class DrawGestureTests
{
    [Theory]
    [InlineData(FigureType.Rectangle)]
    [InlineData(FigureType.Triangle)]
    public void CornerToCorner_ProducesTheBoundingBox(FigureType type)
    {
        var result = DrawGesture.Build(type, 100, 100, 300, 200);

        Assert.Equal(new Box(100, 100, 300, 200), result);
    }

    [Theory]
    [InlineData(FigureType.Rectangle)]
    [InlineData(FigureType.Triangle)]
    public void CornerToCorner_DraggedUpAndLeft_SortsAxes(FigureType type)
    {
        var result = DrawGesture.Build(type, 300, 200, 100, 100);

        Assert.Equal(new Box(100, 100, 300, 200), result);
    }

    [Theory]
    [InlineData(FigureType.Rectangle)]
    [InlineData(FigureType.Triangle)]
    public void CornerToCorner_FarCorner_IsReproducedUnclamped(FigureType type)
    {
        // The draw-clamp is gone (D-59 item 6, STOR-04): both far-corner coordinates are
        // preserved exactly, neither reduced to the canvas edge (1472, 828).
        var result = DrawGesture.Build(type, 1200, 600, 5000, 5000);

        Assert.Equal(new Box(1200, 600, 5000, 5000), result);
    }

    [Theory]
    [InlineData(FigureType.Rectangle)]
    [InlineData(FigureType.Triangle)]
    public void CornerToCorner_NegativeOrigin_IsReproducedUnclamped(FigureType type)
    {
        // Negative coordinates are preserved exactly — nothing clamps them to 0 any more.
        var result = DrawGesture.Build(type, 100, 100, -500, -500);

        Assert.Equal(new Box(-500, -500, 100, 100), result);
    }

    [Fact]
    public void Line_UpAndRightDiagonal_DoesNotFlipToOppositeDiagonal()
    {
        // THE landmine (D-41): the up-and-right diagonal must NOT come back as the opposite
        // diagonal. It renders without erroring, so nothing but a test catches this.
        var result = DrawGesture.Build(FigureType.Line, 0, 100, 100, 0);

        Assert.Equal(new Box(0, 100, 100, 0), result);
        Assert.NotEqual(new Box(0, 0, 100, 100), result);
    }

    [Fact]
    public void Line_DrawnTheOtherWay_SwapsTheWholePointPairToTheSameCanonicalForm()
    {
        var result = DrawGesture.Build(FigureType.Line, 100, 0, 0, 100);

        Assert.Equal(new Box(0, 100, 100, 0), result);
    }

    [Fact]
    public void Line_Horizontal_IsLegalAndDrawable()
    {
        var result = DrawGesture.Build(FigureType.Line, 10, 50, 200, 50);

        Assert.Equal(new Box(10, 50, 200, 50), result);
        Assert.True(MinSizeGuard.IsDrawable(FigureType.Line, result));
    }

    [Fact]
    public void Line_Vertical_IsLegalAndDrawable()
    {
        var result = DrawGesture.Build(FigureType.Line, 50, 10, 50, 200);

        Assert.Equal(new Box(50, 10, 50, 200), result);
        Assert.True(MinSizeGuard.IsDrawable(FigureType.Line, result));
    }

    [Fact]
    public void Circle_CentreOut_ProducesTheInscribedSquare_AndRoundTripsExactly()
    {
        var result = DrawGesture.Build(FigureType.Circle, 640, 360, 740, 360);

        Assert.Equal(new Box(540, 260, 740, 460), result);
        Assert.Equal((640, 360, 100), CircleEncoding.ToCentreRadius(result));
    }

    [Fact]
    public void Circle_NearLeftEdge_KeepsFullRadius_AndExtendsOffCanvas()
    {
        // The circle draw-clamp is gone (D-59 item 6, STOR-04): pressing 10px from the left
        // edge and dragging 200px out produces the full radius-200 circle, whose left edge is
        // off-canvas — not a capped tiny circle.
        var result = DrawGesture.Build(FigureType.Circle, 10, 360, 210, 360);

        Assert.Equal(new Box(-190, 160, 210, 560), result);
        Assert.Equal((10, 360, 200), CircleEncoding.ToCentreRadius(result));
    }

    [Theory]
    [InlineData(FigureType.Line)]
    [InlineData(FigureType.Rectangle)]
    [InlineData(FigureType.Triangle)]
    [InlineData(FigureType.Circle)]
    public void PressEqualsCursor_ProducesABoxTheGuardRejects_ForEveryType(FigureType type)
    {
        var result = DrawGesture.Build(type, 500, 500, 500, 500);

        Assert.False(MinSizeGuard.IsDrawable(type, result));
    }

    // Grid points spanning far outside the canvas on all four sides, exactly on each boundary,
    // and comfortably inside. Zipped rather than fully crossed on x/y so each single "point" is
    // itself a legal (x, y) pair drawn from both required sets; press and cursor are then crossed
    // against each other and against all four figure types.
    private static readonly (int X, int Y)[] GridPoints =
    {
        (-500, -500),
        (0, 0),
        (5, 5),
        (640, 360),
        (1467, 823),
        (1472, 828),
        (5000, 5000),
    };

    public static IEnumerable<object[]> InvariantGridCases()
    {
        var types = new[] { FigureType.Line, FigureType.Rectangle, FigureType.Circle, FigureType.Triangle };

        foreach (var type in types)
        foreach (var press in GridPoints)
        foreach (var cursor in GridPoints)
        {
            yield return new object[] { type, press.X, press.Y, cursor.X, cursor.Y };
        }
    }

    [Theory]
    [MemberData(nameof(InvariantGridCases))]
    public void EveryResult_ReproducesTheGesture_WithNoClamp(
        FigureType type, int pressX, int pressY, int cursorX, int cursorY)
    {
        var result = DrawGesture.Build(type, pressX, pressY, cursorX, cursorY);

        switch (type)
        {
            case FigureType.Rectangle:
            case FigureType.Triangle:
                // Rectangle/triangle: the result is exactly the axis-sorted press/cursor box —
                // no clamp anywhere reshapes it.
                var expected = new Box(
                    Math.Min(pressX, cursorX), Math.Min(pressY, cursorY),
                    Math.Max(pressX, cursorX), Math.Max(pressY, cursorY));
                Assert.Equal(expected, result);
                break;

            case FigureType.Line:
                // Line: the result is the press/cursor pair or its whole-pair swap — never the
                // axis-sorted box (D-41's landmine).
                var straight = new Box(pressX, pressY, cursorX, cursorY);
                var swapped = new Box(cursorX, cursorY, pressX, pressY);
                Assert.True(result == straight || result == swapped);
                break;

            case FigureType.Circle:
                // Circle: width equals height equals twice the rounded press-to-cursor distance
                // — never capped by any canvas edge.
                var dx = (double)(cursorX - pressX);
                var dy = (double)(cursorY - pressY);
                var radius = (int)Math.Round(Math.Sqrt(dx * dx + dy * dy), MidpointRounding.AwayFromZero);
                Assert.Equal(radius * 2, result.Width);
                Assert.Equal(radius * 2, result.Height);
                break;
        }
    }
}
