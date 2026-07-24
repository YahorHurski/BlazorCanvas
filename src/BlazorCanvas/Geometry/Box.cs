namespace BlazorCanvas.Geometry;

/// <summary>
/// Four integers describing an extent (D-20). Storage, from D-59, is an anchor <c>x,y</c> plus
/// <c>geometry jsonb</c> — <see cref="Box"/> is no longer the storage model. It is now a
/// transient extent only: the shape a draw gesture or a render produces, and the decoded form
/// <see cref="GeometryCodec"/> speaks. A circle's box is the square it is inscribed in; see
/// <see cref="CircleEncoding"/>.
/// </summary>
public readonly record struct Box(int X1, int Y1, int X2, int Y2)
{
    public int Width => X2 - X1;

    public int Height => Y2 - Y1;
}
