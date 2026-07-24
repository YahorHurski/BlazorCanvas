using System.Text.Json;

namespace BlazorCanvas.Geometry;

/// <summary>
/// Turns an anchor plus the stored <c>geometry</c> JSON into the exact SVG coordinates the retired
/// bounding-box renderer produced for the same figure (D-59, T-10-14): circle {r} about the centre
/// anchor, rectangle/triangle {w,h} from the top-left anchor, line {dx,dy} from the first endpoint.
/// Deserialises through <see cref="GeometryCodec"/>'s own internal wire-format records rather than
/// re-deriving the "w"/"h"/"r"/"dx"/"dy" member names, so a rename there cannot silently diverge
/// from this renderer.
/// </summary>
public static class ShapeRender
{
    /// <summary>Rectangle/triangle size: the stored width and height (the "w"/"h" pair).</summary>
    public static (int W, int H) Size(string geometry)
    {
        var size = JsonSerializer.Deserialize<GeometryCodec.BoxGeometry?>(geometry)
            ?? throw new InvalidOperationException("Box geometry JSON is empty.");

        return (size.W, size.H);
    }

    /// <summary>Circle radius: the stored "r".</summary>
    public static int Radius(string geometry)
    {
        var circle = JsonSerializer.Deserialize<GeometryCodec.CircleGeometry?>(geometry)
            ?? throw new InvalidOperationException("Circle geometry JSON is empty.");

        return circle.R;
    }

    /// <summary>Line delta: the stored signed "dx"/"dy" pair from the first endpoint.</summary>
    public static (int Dx, int Dy) LineDelta(string geometry)
    {
        var line = JsonSerializer.Deserialize<GeometryCodec.LineGeometry?>(geometry)
            ?? throw new InvalidOperationException("Line geometry JSON is empty.");

        return (line.Dx, line.Dy);
    }

    /// <summary>
    /// The triangle's SVG <c>points</c> attribute value: apex top-centre, base along the bottom
    /// (D-21). Apex x is the anchor x plus half the width as a FRACTIONAL value — integer division
    /// is forbidden, or an odd-width triangle stops being isosceles. Formatted through
    /// InvariantCulture: on a comma-decimal server locale a half-pixel apex would otherwise emit a
    /// comma, and the browser would silently reparse the points list as different coordinates
    /// (T-10-13).
    /// </summary>
    public static string TrianglePoints(int x, int y, int width, int height) =>
        FormattableString.Invariant(
            $"{x + width / 2.0},{y} {x},{y + height} {x + width},{y + height}");
}
