namespace BlazorCanvas.Geometry;

/// <summary>
/// The fixed canvas surface (D-19). This number must never change — stored coordinates are
/// only meaningful relative to it. Bounds are INCLUSIVE: the valid domain is 0..1280 x 0..720.
/// </summary>
public static class CanvasBounds
{
    public const int Width = 1280;

    public const int Height = 720;
}
