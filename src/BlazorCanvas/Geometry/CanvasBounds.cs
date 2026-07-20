namespace BlazorCanvas.Geometry;

/// <summary>
/// The fixed canvas surface (D-19). This size may grow but must never shrink because shrinking
/// would orphan stored figures off the surface. Bounds are INCLUSIVE: the valid domain is
/// 0..1472 x 0..828.
/// </summary>
public static class CanvasBounds
{
    public const int Width = 1472;

    public const int Height = 828;
}
