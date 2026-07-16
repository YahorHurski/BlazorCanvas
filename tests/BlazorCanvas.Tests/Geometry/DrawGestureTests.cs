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
    public void CornerToCorner_ClampedAtTheFarCorner(FigureType type)
    {
        var result = DrawGesture.Build(type, 1200, 600, 5000, 5000);

        Assert.Equal(new Box(1200, 600, 1280, 720), result);
    }

    [Theory]
    [InlineData(FigureType.Rectangle)]
    [InlineData(FigureType.Triangle)]
    public void CornerToCorner_ClampedAtTheOrigin(FigureType type)
    {
        var result = DrawGesture.Build(type, 100, 100, -500, -500);

        Assert.Equal(new Box(0, 0, 100, 100), result);
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
    public void Circle_DrawClamp_NearLeftEdge_CapsTheRadius()
    {
        // Known, accepted consequence of D-13 x D-29: pressing near an edge forces a tiny
        // circle. Still square, never an oval.
        var result = DrawGesture.Build(FigureType.Circle, 10, 360, 210, 360);

        Assert.Equal(new Box(0, 350, 20, 370), result);
        Assert.Equal(result.Width, result.Height);
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

    // Grid points drawn from the mandated coordinate sets (D-29, D-36): far outside the canvas
    // on all four sides, exactly on each boundary, and comfortably inside. Zipped rather than
    // fully crossed on x/y so each single "point" is itself a legal (x, y) pair drawn from both
    // required sets; press and cursor are then crossed against each other and against all four
    // figure types.
    private static readonly (int X, int Y)[] GridPoints =
    {
        (-500, -500),
        (0, 0),
        (5, 5),
        (640, 360),
        (1275, 715),
        (1280, 720),
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
    public void EveryResult_LiesEntirelyInsideTheCanvas_AndCirclesAreAlwaysSquare(
        FigureType type, int pressX, int pressY, int cursorX, int cursorY)
    {
        var result = DrawGesture.Build(type, pressX, pressY, cursorX, cursorY);

        // A normalised line may legally have Y1 > Y2 (D-41) — use min/max, not raw X1/Y1.
        Assert.True(0 <= Math.Min(result.X1, result.X2));
        Assert.True(Math.Max(result.X1, result.X2) <= 1280);
        Assert.True(0 <= Math.Min(result.Y1, result.Y2));
        Assert.True(Math.Max(result.Y1, result.Y2) <= 720);

        if (type == FigureType.Circle)
        {
            Assert.Equal(result.Width, result.Height);
        }
    }
}
