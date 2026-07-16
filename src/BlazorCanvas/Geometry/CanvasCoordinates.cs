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
/// (3) This class only MAPS — it does not clamp. Bounds clamping (D-36) belongs to
/// <see cref="Movement"/> and <see cref="DrawGesture"/>, never here.
/// </summary>
public static class CanvasCoordinates
{
    public const int ToolbarHeight = 48;

    /// <summary>
    /// Maps a page-relative pointer position to canvas coordinates. Fractional input rounds to
    /// the nearest integer, midpoints rounding away from zero — the same convention already
    /// established by <see cref="CircleEncoding.ClampDrawRadius"/>. Deliberately unclamped: a
    /// point above the canvas maps to a negative canvas y.
    /// </summary>
    public static (int X, int Y) FromPage(double pageX, double pageY)
    {
        var x = (int)Math.Round(pageX, MidpointRounding.AwayFromZero);
        var y = (int)Math.Round(pageY - ToolbarHeight, MidpointRounding.AwayFromZero);
        return (x, y);
    }
}
