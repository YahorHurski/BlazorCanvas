using System.Globalization;
using BlazorCanvas.Geometry;

namespace BlazorCanvas.Tests.Geometry;

public sealed class ShapeRenderTests
{
    // --- Accessors, against the exact JSON literals GeometryCodec emits per type ---

    [Fact]
    public void Size_ReadsWAndH_FromTheExactCodecLiteral()
    {
        var (w, h) = ShapeRender.Size("""{"w":70,"h":40}""");

        Assert.Equal(70, w);
        Assert.Equal(40, h);
    }

    [Fact]
    public void Radius_ReadsR_FromTheExactCodecLiteral()
    {
        var r = ShapeRender.Radius("""{"r":20}""");

        Assert.Equal(20, r);
    }

    [Fact]
    public void LineDelta_ReadsDxAndDy_FromTheExactCodecLiteral()
    {
        var (dx, dy) = ShapeRender.LineDelta("""{"dx":100,"dy":-50}""");

        Assert.Equal(100, dx);
        Assert.Equal(-50, dy);
    }

    // --- Triangle points formatter: even width, odd width, and the locale landmine ---

    [Fact]
    public void TrianglePoints_EvenWidth_ReturnsExpectedString()
    {
        Assert.Equal("30,20 10,80 50,80", ShapeRender.TrianglePoints(10, 20, 40, 60));
    }

    [Fact]
    public void TrianglePoints_OddWidth_ReturnsHalfPixelApex()
    {
        Assert.Equal("4.5,0 0,10 9,10", ShapeRender.TrianglePoints(0, 0, 9, 10));
    }

    [Fact]
    public void TrianglePoints_UnderCommaDecimalCulture_StillUsesAPeriod()
    {
        // T-10-13: a comma-decimal host culture must never leak into the SVG points list, or the
        // browser silently reparses it as an extra coordinate pair.
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");

            var points = ShapeRender.TrianglePoints(0, 0, 9, 10);

            Assert.Contains("4.5", points);
            Assert.DoesNotContain("4,5", points);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    // --- Appearance-preservation cross-check: ShapeRender must reproduce the retired
    // bounding-box renderer's coordinates exactly, for all four types. ---

    public static TheoryData<FigureType, Box> AppearanceCases => new()
    {
        { FigureType.Rectangle, new Box(10, 20, 80, 90) },
        { FigureType.Triangle, new Box(5, 6, 45, 66) },
        { FigureType.Triangle, new Box(0, 0, 9, 10) }, // odd width, forces a half-pixel apex
        { FigureType.Circle, new Box(20, 30, 60, 70) },
        { FigureType.Line, new Box(0, 0, 100, 50) },     // down-and-right diagonal
        { FigureType.Line, new Box(0, 100, 100, 0) },    // up-and-right diagonal
    };

    [Theory]
    [MemberData(nameof(AppearanceCases))]
    public void ShapeRender_MatchesTheRetiredBoundingBoxRenderer(FigureType type, Box box)
    {
        var encoded = GeometryCodec.Encode(type, box);

        switch (type)
        {
            case FigureType.Line:
                var (dx, dy) = ShapeRender.LineDelta(encoded.Geometry);
                Assert.Equal(box.X1, encoded.X);
                Assert.Equal(box.Y1, encoded.Y);
                Assert.Equal(box.X2, encoded.X + dx);
                Assert.Equal(box.Y2, encoded.Y + dy);
                break;

            case FigureType.Rectangle:
            case FigureType.Triangle:
                var (w, h) = ShapeRender.Size(encoded.Geometry);
                Assert.Equal(box.X1, encoded.X);
                Assert.Equal(box.Y1, encoded.Y);
                Assert.Equal(box.Width, w);
                Assert.Equal(box.Height, h);
                break;

            case FigureType.Circle:
                var r = ShapeRender.Radius(encoded.Geometry);
                var (expectedCx, expectedCy, expectedR) = CircleEncoding.ToCentreRadius(box);
                Assert.Equal(expectedCx, encoded.X);
                Assert.Equal(expectedCy, encoded.Y);
                Assert.Equal(expectedR, r);
                break;
        }
    }

    [Fact]
    public void Line_DownAndRightDiagonal_DoesNotRenderAsUpAndRight()
    {
        // The flipped-line landmine (D-41): swapping the whole point pair must never collapse into
        // sorted axes, or the opposite diagonal is drawn.
        var downRight = GeometryCodec.Encode(FigureType.Line, new Box(0, 0, 100, 50));
        var upRight = GeometryCodec.Encode(FigureType.Line, new Box(0, 100, 100, 0));

        var (dxDown, dyDown) = ShapeRender.LineDelta(downRight.Geometry);
        var (dxUp, dyUp) = ShapeRender.LineDelta(upRight.Geometry);

        Assert.Equal((0, 0, 100, 50), (downRight.X, downRight.Y, dxDown, dyDown));
        Assert.Equal((0, 100, 100, -100), (upRight.X, upRight.Y, dxUp, dyUp));
        Assert.NotEqual((downRight.X, downRight.Y, dxDown, dyDown), (upRight.X, upRight.Y, dxUp, dyUp));
    }

    [Theory]
    [InlineData(10, 20, 80, 90)]
    [InlineData(5, 6, 45, 66)]
    [InlineData(0, 0, 9, 10)]
    public void TrianglePoints_MatchesTheStringTheRetiredFormatterBuilt(int x1, int y1, int x2, int y2)
    {
        var box = new Box(x1, y1, x2, y2);
        var encoded = GeometryCodec.Encode(FigureType.Triangle, box);
        var (w, h) = ShapeRender.Size(encoded.Geometry);

        // The retired formula, verbatim, from the old FigureShape/SelectionTrace formatter.
        var expected = FormattableString.Invariant(
            $"{(box.X1 + box.X2) / 2.0},{box.Y1} {box.X1},{box.Y2} {box.X2},{box.Y2}");

        var actual = ShapeRender.TrianglePoints(encoded.X, encoded.Y, w, h);

        Assert.Equal(expected, actual);
    }
}
