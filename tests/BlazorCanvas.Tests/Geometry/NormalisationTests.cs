using BlazorCanvas.Geometry;

namespace BlazorCanvas.Tests.Geometry;

public class NormalisationTests
{
    [Fact]
    public void Line_UpAndRightDiagonal_IsNotFlippedToOppositeDiagonal()
    {
        // Mandated test 3 of TEST-01 (D-41): the up-and-right diagonal must NOT come back
        // as the opposite diagonal.
        var result = Normalisation.Normalise(FigureType.Line, new Box(0, 100, 100, 0));

        Assert.Equal(new Box(0, 100, 100, 0), result);
        Assert.NotEqual(new Box(0, 0, 100, 100), result);
    }

    [Fact]
    public void Line_DownAndRightDiagonal_SwapsWholePointPair()
    {
        var result = Normalisation.Normalise(FigureType.Line, new Box(100, 0, 0, 100));

        Assert.Equal(new Box(0, 100, 100, 0), result);
    }

    [Fact]
    public void Line_VerticalWithX1EqualsX2_TiebreakSwapsOnY()
    {
        var result = Normalisation.Normalise(FigureType.Line, new Box(50, 100, 50, 0));

        Assert.Equal(new Box(50, 0, 50, 100), result);
    }

    [Fact]
    public void Line_AlreadyCanonical_IsUnchanged()
    {
        var result = Normalisation.Normalise(FigureType.Line, new Box(50, 0, 50, 100));

        Assert.Equal(new Box(50, 0, 50, 100), result);
    }

    [Fact]
    public void Line_Y1CanExceedY2_AfterNormalisation()
    {
        // A down-and-right diagonal is a legal canonical line form: X1 <= X2 but Y1 > Y2.
        // This is exactly the case Task 2's clamp must recompute a min/max bounding box for.
        var result = Normalisation.Normalise(FigureType.Line, new Box(0, 700, 100, 100));

        Assert.Equal(new Box(0, 700, 100, 100), result);
        Assert.True(result.Y1 > result.Y2);
    }

    [Fact]
    public void Rectangle_ReversedDiagonal_SortsAxesIndependently()
    {
        var result = Normalisation.Normalise(FigureType.Rectangle, new Box(100, 100, 0, 0));

        Assert.Equal(new Box(0, 0, 100, 100), result);
    }

    [Fact]
    public void Rectangle_OppositeDiagonal_GetsAxisSort()
    {
        // A rectangle DOES get the axis sort; only a line does not.
        var result = Normalisation.Normalise(FigureType.Rectangle, new Box(0, 100, 100, 0));

        Assert.Equal(new Box(0, 0, 100, 100), result);
    }

    [Fact]
    public void Triangle_ReversedDiagonal_SortsAxesIndependently()
    {
        var result = Normalisation.Normalise(FigureType.Triangle, new Box(100, 100, 0, 0));

        Assert.Equal(new Box(0, 0, 100, 100), result);
    }

    [Fact]
    public void Triangle_OppositeDiagonal_GetsAxisSort()
    {
        var result = Normalisation.Normalise(FigureType.Triangle, new Box(0, 100, 100, 0));

        Assert.Equal(new Box(0, 0, 100, 100), result);
    }

    [Fact]
    public void Circle_ReversedDiagonal_SortsAxesIndependently()
    {
        var result = Normalisation.Normalise(FigureType.Circle, new Box(100, 100, 0, 0));

        Assert.Equal(new Box(0, 0, 100, 100), result);
    }

    [Fact]
    public void Circle_OppositeDiagonal_GetsAxisSort()
    {
        var result = Normalisation.Normalise(FigureType.Circle, new Box(0, 100, 100, 0));

        Assert.Equal(new Box(0, 0, 100, 100), result);
    }

    [Theory]
    [InlineData(FigureType.Line)]
    [InlineData(FigureType.Rectangle)]
    [InlineData(FigureType.Triangle)]
    [InlineData(FigureType.Circle)]
    public void PostCondition_X1IsAlwaysLessThanOrEqualX2(FigureType type)
    {
        var inputs = new[]
        {
            new Box(0, 0, 100, 100),
            new Box(100, 100, 0, 0),
            new Box(0, 100, 100, 0),
            new Box(100, 0, 0, 100),
            new Box(50, 50, 50, 50),
        };

        foreach (var input in inputs)
        {
            var result = Normalisation.Normalise(type, input);
            Assert.True(result.X1 <= result.X2, $"{type}: {input} -> {result}");
        }
    }

    [Theory]
    [InlineData(FigureType.Rectangle)]
    [InlineData(FigureType.Triangle)]
    [InlineData(FigureType.Circle)]
    public void PostCondition_Y1IsAlwaysLessThanOrEqualY2_ForNonLineShapes(FigureType type)
    {
        var inputs = new[]
        {
            new Box(0, 0, 100, 100),
            new Box(100, 100, 0, 0),
            new Box(0, 100, 100, 0),
            new Box(100, 0, 0, 100),
            new Box(50, 50, 50, 50),
        };

        foreach (var input in inputs)
        {
            var result = Normalisation.Normalise(type, input);
            Assert.True(result.Y1 <= result.Y2, $"{type}: {input} -> {result}");
        }
    }
}

public class FigureTypeNamesTests
{
    [Theory]
    [InlineData(FigureType.Line, "line")]
    [InlineData(FigureType.Rectangle, "rectangle")]
    [InlineData(FigureType.Circle, "circle")]
    [InlineData(FigureType.Triangle, "triangle")]
    public void ToDbValue_ReturnsExactLowercaseLiteral(FigureType type, string expected)
    {
        Assert.Equal(expected, FigureTypeNames.ToDbValue(type));
    }

    [Theory]
    [InlineData("line", FigureType.Line)]
    [InlineData("rectangle", FigureType.Rectangle)]
    [InlineData("circle", FigureType.Circle)]
    [InlineData("triangle", FigureType.Triangle)]
    public void Parse_IsTheInverseOfToDbValue(string value, FigureType expected)
    {
        Assert.Equal(expected, FigureTypeNames.Parse(value));
    }

    [Fact]
    public void Parse_ThrowsOnUnknownLiteral()
    {
        Assert.Throws<ArgumentException>(() => FigureTypeNames.Parse("hexagon"));
    }
}

public class CanvasBoundsTests
{
    [Fact]
    public void Bounds_AreTheFixed1280x720Canvas()
    {
        Assert.Equal(1280, CanvasBounds.Width);
        Assert.Equal(720, CanvasBounds.Height);
    }
}
