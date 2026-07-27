namespace BlazorCanvas.Geometry;

/// <summary>
/// The fixed canvas surface size (D-19). This size may grow but must never shrink because
/// shrinking would orphan stored figures off the surface. These constants only size the SVG
/// surface — no coordinate is clamped to them any more (D-59 drops D-24/D-29/D-36).
/// </summary>
public static class CanvasBounds
{
    public const int Width = 1472;

    public const int Height = 828;
}
