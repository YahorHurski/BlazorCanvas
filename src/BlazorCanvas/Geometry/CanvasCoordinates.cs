namespace BlazorCanvas.Geometry;

/// <summary>
/// The page-to-canvas coordinate mapping (D-43, D-18). This is the ONLY place in the app
/// allowed to subtract the toolbar height — every pointer handler routes through
/// <see cref="FromPage"/>.
///
/// (1) The source event properties MUST be the page-relative ones (Blazor's
/// <c>PointerEventArgs.PageX</c> / <c>PageY</c>), never the event-target-relative pair
/// (<c>OffsetX</c> / <c>OffsetY</c>). <c>OffsetX/Y</c> is measured from whatever element the
/// pointer is currently over, and every draw and drag in this app can begin on top of an
/// existing figure (D-18, D-43).
///
/// (2) <see cref="ToolbarHeight"/> must stay equal to the `.toolbar { height: 48px }` rule in
/// Home.razor.css, and the SVG must carry no CSS border — a border shifts the SVG interior by
/// its own width and silently adds another term to the mapping (D-43).
///
/// (3) This class only MAPS — it does not clamp to the canvas. Neither <see cref="Movement"/>
/// nor <see cref="DrawGesture"/> clamps any more either (D-59 drops D-24/D-29/D-36); a point
/// beyond the canvas maps to a coordinate beyond the canvas, on purpose, unclamped.
///
/// (T-10-01) With no downstream clamp left to catch it, this is now the last place a non-finite
/// browser-supplied double (NaN, +Infinity, -Infinity) can be stopped before it reaches an
/// unchecked `(int)` cast, which is undefined behaviour for those values in C#. Non-finite input
/// maps to 0; finite input is rounded and clamped into the `int` domain before the cast — an
/// integer-domain guard bounding the value to a representable integer, not a reinstated canvas
/// bound.
/// </summary>
public static class CanvasCoordinates
{
    public const int ToolbarHeight = 48;

    /// <summary>
    /// Maps a page-relative pointer position to canvas coordinates. Fractional input rounds to
    /// the nearest integer, midpoints rounding away from zero. Deliberately unclamped: a point
    /// above the canvas maps to a negative canvas y, and a point past the far edge maps past the
    /// canvas — nothing here bounds a coordinate to the canvas (T-10-01, D-59).
    /// </summary>
    public static (int X, int Y) FromPage(double pageX, double pageY)
    {
        var x = ToCanvasCoordinate(pageX);
        var y = ToCanvasCoordinate(pageY - ToolbarHeight);
        return (x, y);
    }

    /// <summary>
    /// Converts one page-space double to a canvas int (T-10-01 mitigation). Non-finite input
    /// (NaN, either infinity) returns 0 rather than reaching the cast below, which is undefined
    /// for those values. Finite input rounds away from zero and is clamped into the int domain
    /// before the cast — bounding the value to a representable integer, not to the canvas.
    /// </summary>
    private static int ToCanvasCoordinate(double value)
    {
        if (!double.IsFinite(value))
        {
            return 0;
        }

        var rounded = Math.Round(value, MidpointRounding.AwayFromZero);
        var clamped = Math.Clamp(rounded, (double)int.MinValue, (double)int.MaxValue);
        return (int)clamped;
    }
}
