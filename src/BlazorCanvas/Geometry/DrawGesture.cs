namespace BlazorCanvas.Geometry;

/// <summary>
/// Turns a press point, a cursor point, and a figure type into a normalised <see cref="Box"/>.
/// A pure composition over the geometry core.
///
/// No canvas-edge clamp (D-59 drops D-24/D-29/D-36): pointer coordinates are reproduced exactly
/// as the browser reported them, so a figure may legitimately extend past the canvas edge, wholly
/// or partly (STOR-04). This class still does NOT call <see cref="MinSizeGuard"/> — the guard is
/// the caller's decision, so a live preview keeps rendering a not-yet-drawable gesture (D-35,
/// D-50).
/// </summary>
public static class DrawGesture
{
    /// <summary>
    /// Builds the Box a draw gesture produces. Does NOT call <see cref="MinSizeGuard"/> — the
    /// guard is the caller's decision (checked before the INSERT and rejected silently, D-50); a
    /// preview still renders for a not-yet-drawable gesture.
    /// </summary>
    public static Box Build(FigureType type, int pressX, int pressY, int cursorX, int cursorY)
    {
        if (type == FigureType.Circle)
        {
            // The press point is the centre (D-13); drag distance sets the radius.
            // Cast to double before multiplying so an int overflow is impossible.
            var dx = (double)(cursorX - pressX);
            var dy = (double)(cursorY - pressY);
            var distance = Math.Sqrt(dx * dx + dy * dy);

            var radius = (int)Math.Round(distance, MidpointRounding.AwayFromZero);

            // Already normalised and already even-sided — must NOT be passed through
            // Normalisation.Normalise afterwards.
            return CircleEncoding.FromCentreRadius(pressX, pressY, radius);
        }

        // Press point first, cursor second, so the line arm's whole-point-pair swap sees the
        // gesture in the order it was actually drawn.
        return Normalisation.Normalise(type, new Box(pressX, pressY, cursorX, cursorY));
    }
}
