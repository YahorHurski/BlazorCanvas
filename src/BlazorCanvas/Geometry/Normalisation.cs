namespace BlazorCanvas.Geometry;

/// <summary>
/// The canonical order on write (D-41). Applied once, before the INSERT, in exactly one place.
/// </summary>
public static class Normalisation
{
    public static Box Normalise(FigureType type, Box b)
    {
        if (type == FigureType.Line)
        {
            // A line is normalised by swapping the WHOLE POINT PAIR — never by sorting its axes
            // independently. Sorting axes independently turns the up-and-right diagonal into the
            // opposite (down-and-right) diagonal; the figure still renders, so nothing errors and
            // nobody notices (D-41's landmine).
            var swap = b.X1 > b.X2 || (b.X1 == b.X2 && b.Y1 > b.Y2);
            return swap ? new Box(b.X2, b.Y2, b.X1, b.Y1) : b;
        }

        // Rectangle, Triangle, Circle: sort the axes independently.
        var x1 = Math.Min(b.X1, b.X2);
        var x2 = Math.Max(b.X1, b.X2);
        var y1 = Math.Min(b.Y1, b.Y2);
        var y2 = Math.Max(b.Y1, b.Y2);
        return new Box(x1, y1, x2, y2);
    }
}
