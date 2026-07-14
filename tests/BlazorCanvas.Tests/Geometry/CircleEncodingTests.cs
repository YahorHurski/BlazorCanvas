using BlazorCanvas.Geometry;

namespace BlazorCanvas.Tests.Geometry;

public class CircleEncodingTests
{
    public static IEnumerable<object[]> RoundTripCases()
    {
        yield return new object[] { 100, 100, 50 };
        yield return new object[] { 640, 360, 1 };
        yield return new object[] { 640, 360, 360 };
        yield return new object[] { 200, 200, 7 };
    }

    [Theory]
    [MemberData(nameof(RoundTripCases))]
    public void ToCentreRadius_OfFromCentreRadius_IsExact(int cx, int cy, int r)
    {
        // Mandated test 2 of TEST-01 (D-22): centre and radius come back EXACT — not
        // approximately, exactly — after encode + decode.
        var box = CircleEncoding.FromCentreRadius(cx, cy, r);
        var (resultCx, resultCy, resultR) = CircleEncoding.ToCentreRadius(box);

        Assert.Equal(cx, resultCx);
        Assert.Equal(cy, resultCy);
        Assert.Equal(r, resultR);
    }

    [Fact]
    public void Radius_SurvivesTenSuccessiveTranslations_IncludingTwoEdgeClipped()
    {
        var box = CircleEncoding.FromCentreRadius(200, 200, 50);

        var deltas = new (int dx, int dy)[]
        {
            (10, 10),
            (-5, 20),
            (30, -15),
            (-100_000, 0), // edge-clipped: clamps against the left edge
            (0, -100_000), // edge-clipped: clamps against the top edge
            (5, 5),
            (-20, -20),
            (15, 0),
            (0, 15),
            (7, -7),
        };

        foreach (var (dx, dy) in deltas)
        {
            box = Movement.ClampMove(box, dx, dy);
            var (_, _, r) = CircleEncoding.ToCentreRadius(box);
            Assert.Equal(50, r);
        }
    }

    [Theory]
    [MemberData(nameof(RoundTripCases))]
    public void EveryEncodedCircle_SatisfiesMinSizeGuard(int cx, int cy, int r)
    {
        var box = CircleEncoding.FromCentreRadius(cx, cy, r);

        Assert.True(MinSizeGuard.IsDrawable(FigureType.Circle, box));
    }

    [Fact]
    public void ZeroRadius_ProducesABoxTheGuardRejects()
    {
        var box = CircleEncoding.FromCentreRadius(100, 100, 0);

        Assert.False(MinSizeGuard.IsDrawable(FigureType.Circle, box));
    }

    [Fact]
    public void ClampDrawRadius_NearLeftEdge_ForcesATinyCircle()
    {
        // Known and accepted consequence of D-13 x D-29 — assert it, do not "fix" it.
        Assert.Equal(10, CircleEncoding.ClampDrawRadius(cx: 10, cy: 360, distance: 200));
    }

    [Fact]
    public void ClampDrawRadius_FarFromAnyEdge_PassesThrough()
    {
        Assert.Equal(100, CircleEncoding.ClampDrawRadius(cx: 640, cy: 360, distance: 100));
    }

    [Fact]
    public void ClampDrawRadius_CappedByVerticalExtent()
    {
        Assert.Equal(360, CircleEncoding.ClampDrawRadius(cx: 640, cy: 360, distance: 1000));
    }

    [Fact]
    public void ClampDrawRadius_CappedByRightEdge()
    {
        Assert.Equal(5, CircleEncoding.ClampDrawRadius(cx: 1275, cy: 360, distance: 50));
    }

    [Fact]
    public void ClampDrawRadius_CappedByTopEdge()
    {
        Assert.Equal(5, CircleEncoding.ClampDrawRadius(cx: 640, cy: 5, distance: 50));
    }

    [Fact]
    public void ClampDrawRadius_RoundsTheDistanceBeforeCapping()
    {
        Assert.Equal(11, CircleEncoding.ClampDrawRadius(cx: 100, cy: 100, distance: 10.5));
    }
}
