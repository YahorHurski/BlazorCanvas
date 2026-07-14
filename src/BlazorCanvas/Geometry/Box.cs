namespace BlazorCanvas.Geometry;

/// <summary>
/// Four integers that ARE the figure's bounding box, for every shape (D-20, D-22).
/// A circle's box is the square it is inscribed in; see <see cref="CircleEncoding"/>.
/// </summary>
public readonly record struct Box(int X1, int Y1, int X2, int Y2)
{
    public int Width => X2 - X1;

    public int Height => Y2 - Y1;
}
