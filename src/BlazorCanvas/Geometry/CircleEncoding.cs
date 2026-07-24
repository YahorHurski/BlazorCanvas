namespace BlazorCanvas.Geometry;

/// <summary>
/// A circle is drawn centre-out (press = centre, drag = radius — D-13) and stored, from D-59, as
/// an anchor plus <c>{r}</c>. The inscribed square computed here is an intermediate the codec
/// uses to reach that centre and radius — it is never itself the storage form. A move is a
/// uniform translation, so the radius is exactly preserved across any number of drags: the
/// offset cancels algebraically in <see cref="ToCentreRadius"/>.
/// </summary>
public static class CircleEncoding
{
    public static Box FromCentreRadius(int cx, int cy, int r) =>
        new(cx - r, cy - r, cx + r, cy + r);

    public static (int Cx, int Cy, int R) ToCentreRadius(Box b)
    {
        var r = (b.X2 - b.X1) / 2;
        var cx = b.X1 + r;
        var cy = b.Y1 + r;
        return (cx, cy, r);
    }
}
