namespace BlazorCanvas.Shapes;

/// <summary>
/// The derived, stroke-excluding axis-aligned extent of local geometry. Phase 10 stores
/// x + X, y + Y, W, and H in <c>bbox_*</c>; this cache is never the source of truth.
/// SVG strokes are centred on the outline and extend by half their width on each side.
/// </summary>
public readonly record struct Bbox(double X, double Y, double W, double H);
