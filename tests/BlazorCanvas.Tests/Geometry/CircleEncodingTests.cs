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
    public void Radius_SurvivesTenSuccessiveTranslations_IncludingTwoFarOffCanvas()
    {
        // Translates the centre directly through FromCentreRadius — no clamp exists any more to
        // route this through. Two of the ten deltas send the centre far off-canvas; the radius
        // must still come back bit-identical (D-59 item 6).
        var (cx, cy) = (200, 200);
        const int radius = 50;

        var deltas = new (int dx, int dy)[]
        {
            (10, 10),
            (-5, 20),
            (30, -15),
            (-100_000, 0), // sends the centre far off-canvas to the left
            (0, -100_000), // sends the centre far off-canvas above
            (5, 5),
            (-20, -20),
            (15, 0),
            (0, 15),
            (7, -7),
        };

        foreach (var (dx, dy) in deltas)
        {
            cx += dx;
            cy += dy;

            var box = CircleEncoding.FromCentreRadius(cx, cy, radius);
            var (_, _, r) = CircleEncoding.ToCentreRadius(box);

            Assert.Equal(radius, r);
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
    public void CentreOutGesture_EncodesAsAnchorPlusRadiusLiteral_AndDecodesToTheIdenticalBox()
    {
        // TEST-02's re-expression of the D-49 silent-failure test 2: the circle round-trip is
        // now a geometry {r} assertion, not an inscribed-square assertion.
        var box = DrawGesture.Build(FigureType.Circle, 640, 360, 740, 360);

        var encoded = GeometryCodec.Encode(FigureType.Circle, box);

        Assert.Equal(640, encoded.X);
        Assert.Equal(360, encoded.Y);
        Assert.Equal("{\"r\":100}", encoded.Geometry);

        var decoded = GeometryCodec.DecodeToBox(FigureType.Circle, encoded.X, encoded.Y, encoded.Geometry);
        Assert.Equal(box, decoded);
    }
}
