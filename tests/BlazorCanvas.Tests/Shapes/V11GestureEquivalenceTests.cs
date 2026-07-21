using BlazorCanvas.Geometry;
using BlazorCanvas.Shapes;

namespace BlazorCanvas.Tests.Shapes;

// Deliberately temporary: this test reads the v1.1 geometry classes. Phase 11 deletes both those
// classes and this file once the renderer cutover has been completed and proven.
public class V11GestureEquivalenceTests
{
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

    public static IEnumerable<object[]> LineGridCases() => CasesFor(FigureType.Line);

    public static IEnumerable<object[]> TriangleGridCases() => CasesFor(FigureType.Triangle);

    public static IEnumerable<object[]> RectangleGridCases() => CasesFor(FigureType.Rectangle);

    public static IEnumerable<object[]> CircleGridCases() => CasesFor(FigureType.Circle);

    [Theory]
    [MemberData(nameof(InvariantGridCases))]
    public void FromGesture_EveryV11InvariantGridCase_MatchesV11BoundsAndDrawability(
        FigureType type, int pressX, int pressY, int cursorX, int cursorY)
    {
        var v11Box = DrawGesture.Build(type, pressX, pressY, cursorX, cursorY);
        var definition = DefaultShapes.CreateRegistry().Get(FigureTypeNames.ToDbValue(type));
        var placement = definition.FromGesture(new CanvasPoint(pressX, pressY), new CanvasPoint(cursorX, cursorY));
        var bounds = definition.BoundsOf(placement.Geometry);

        var expected = CanonicalExtent(v11Box);
        var actual = new Bbox(placement.X + bounds.X, placement.Y + bounds.Y, bounds.W, bounds.H);

        Assert.Equal(expected.X, actual.X);
        Assert.Equal(expected.Y, actual.Y);
        Assert.Equal(expected.W, actual.W);
        Assert.Equal(expected.H, actual.H);
        Assert.Equal(MinSizeGuard.IsDrawable(type, v11Box), definition.IsDrawable(placement.Geometry));
    }

    [Theory]
    [MemberData(nameof(LineGridCases))]
    public void FromGesture_LineGrid_PreservesTheV11EndpointPair(
        FigureType type, int pressX, int pressY, int cursorX, int cursorY)
    {
        var v11Box = DrawGesture.Build(type, pressX, pressY, cursorX, cursorY);
        var placement = DefaultShapes.CreateRegistry().Get("line")
            .FromGesture(new CanvasPoint(pressX, pressY), new CanvasPoint(cursorX, cursorY));
        var line = Assert.IsType<LineGeometry>(placement.Geometry);

        // Direction-preservation proof: a flipped diagonal has the same bounds but a different endpoint set.
        var actual = line.Points
            .Select(point => new LocalPoint(placement.X + point.X, placement.Y + point.Y))
            .OrderBy(point => point.X)
            .ThenBy(point => point.Y)
            .ToArray();
        var expected = new[] { new LocalPoint(v11Box.X1, v11Box.Y1), new LocalPoint(v11Box.X2, v11Box.Y2) }
            .OrderBy(point => point.X)
            .ThenBy(point => point.Y)
            .ToArray();

        Assert.Equal(expected, actual);
    }

    [Theory]
    [MemberData(nameof(TriangleGridCases))]
    public void FromGesture_TriangleGrid_MatchesTheV11RendererVertexOrder(
        FigureType type, int pressX, int pressY, int cursorX, int cursorY)
    {
        var v11Box = DrawGesture.Build(type, pressX, pressY, cursorX, cursorY);
        var placement = DefaultShapes.CreateRegistry().Get("triangle")
            .FromGesture(new CanvasPoint(pressX, pressY), new CanvasPoint(cursorX, cursorY));
        var triangle = Assert.IsType<TriangleGeometry>(placement.Geometry);

        var actual = triangle.Points
            .Select(point => new LocalPoint(placement.X + point.X, placement.Y + point.Y))
            .ToArray();
        var expected = new[]
        {
            new LocalPoint((v11Box.X1 + v11Box.X2) / 2.0, v11Box.Y1),
            new LocalPoint(v11Box.X1, v11Box.Y2),
            new LocalPoint(v11Box.X2, v11Box.Y2),
        };

        Assert.Equal(expected, actual);
    }

    [Theory]
    [MemberData(nameof(RectangleGridCases))]
    public void FromGesture_RectangleGrid_MatchesTheV11PlacementAndExtent(
        FigureType type, int pressX, int pressY, int cursorX, int cursorY)
    {
        var v11Box = DrawGesture.Build(type, pressX, pressY, cursorX, cursorY);
        var placement = DefaultShapes.CreateRegistry().Get("rectangle")
            .FromGesture(new CanvasPoint(pressX, pressY), new CanvasPoint(cursorX, cursorY));
        var rectangle = Assert.IsType<RectangleGeometry>(placement.Geometry);

        Assert.Equal(v11Box.X1, placement.X);
        Assert.Equal(v11Box.Y1, placement.Y);
        Assert.Equal(v11Box.X2 - v11Box.X1, rectangle.W);
        Assert.Equal(v11Box.Y2 - v11Box.Y1, rectangle.H);
    }

    [Theory]
    [MemberData(nameof(CircleGridCases))]
    public void FromGesture_CircleGrid_MatchesTheV11CentreAndRadius(
        FigureType type, int pressX, int pressY, int cursorX, int cursorY)
    {
        var v11Box = DrawGesture.Build(type, pressX, pressY, cursorX, cursorY);
        var placement = DefaultShapes.CreateRegistry().Get("circle")
            .FromGesture(new CanvasPoint(pressX, pressY), new CanvasPoint(cursorX, cursorY));
        var circle = Assert.IsType<CircleGeometry>(placement.Geometry);
        var expected = CircleEncoding.ToCentreRadius(v11Box);

        Assert.Equal(expected.Cx, placement.X + circle.R);
        Assert.Equal(expected.Cy, placement.Y + circle.R);
        Assert.Equal(expected.R, circle.R);
    }

    private static IEnumerable<object[]> CasesFor(FigureType type) =>
        InvariantGridCases().Where(values => (FigureType)values[0] == type);

    private static Bbox CanonicalExtent(Box box) => new(
        Math.Min(box.X1, box.X2),
        Math.Min(box.Y1, box.Y2),
        Math.Abs(box.X2 - box.X1),
        Math.Abs(box.Y2 - box.Y1));
}
