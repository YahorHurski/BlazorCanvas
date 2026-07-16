namespace BlazorCanvas.Geometry;

/// <summary>
/// Turns a press point, a cursor point, and a figure type into a clamped, normalised
/// <see cref="Box"/>. A pure composition over the Phase 1 geometry core — it re-derives none of
/// the clamp, normalisation, or circle maths, all of which are already tested (plans 01-02 and
/// 01-05).
///
/// Trust boundary (T-03-02): pointer coordinates arrive from the browser over the Blazor Server
/// circuit and are never trustworthy — a crafted circuit message could carry any double,
/// including values far outside the canvas. Clamping every coordinate first, before any type
/// dispatch, is what makes this class the choke point that guarantees every Box it returns lies
/// entirely within the canvas (D-29, D-36).
/// </summary>
public static class DrawGesture
{
    /// <summary>
    /// Builds the Box a draw gesture produces. Does NOT call <see cref="MinSizeGuard"/> — the
    /// guard is the caller's decision (plan 03-05 checks it before the INSERT and rejects
    /// silently, D-50); a preview still renders for a not-yet-drawable gesture.
    /// </summary>
    public static Box Build(FigureType type, int pressX, int pressY, int cursorX, int cursorY)
    {
        // Step 1: clamp all four inputs to the canvas first (D-29) — this is the DRAW clamp.
        // The MOVE clamp is a different method entirely and belongs to Phase 4; it is not
        // called anywhere in this class.
        var px = Movement.ClampDelta(pressX, 0, CanvasBounds.Width);
        var py = Movement.ClampDelta(pressY, 0, CanvasBounds.Height);
        var cx = Movement.ClampDelta(cursorX, 0, CanvasBounds.Width);
        var cy = Movement.ClampDelta(cursorY, 0, CanvasBounds.Height);

        if (type == FigureType.Circle)
        {
            // Step 2: the press point is the centre (D-13); drag distance sets the radius.
            // Cast to double before multiplying so an int overflow is impossible.
            var dx = (double)(cx - px);
            var dy = (double)(cy - py);
            var distance = Math.Sqrt(dx * dx + dy * dy);

            var radius = CircleEncoding.ClampDrawRadius(px, py, distance);

            // Already normalised and already even-sided — must NOT be passed through
            // Normalisation.Normalise afterwards.
            return CircleEncoding.FromCentreRadius(px, py, radius);
        }

        // Step 3: press point first, cursor second, so the line arm's whole-point-pair swap
        // sees the gesture in the order it was actually drawn.
        return Normalisation.Normalise(type, new Box(px, py, cx, cy));
    }
}
