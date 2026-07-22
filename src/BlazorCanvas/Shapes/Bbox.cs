namespace BlazorCanvas.Shapes;

/// <summary>
/// The derived, stroke-excluding axis-aligned extent of local geometry. Phase 10 stores this
/// record's X, Y, W, and H verbatim in <c>bbox_*</c>, in the figure's LOCAL frame; this cache is
/// never the source of truth. MODEL-01 requires a move to write only x and y, so an absolute cache
/// would have to be rewritten on every drag instead of remaining a pure function of geometry. A
/// consumer needing an absolute extent adds the figure's x/y at read time and half the stroke width
/// on top (D-67). SVG strokes are centred on the outline and extend by half their width on each side.
/// </summary>
public readonly record struct Bbox(double X, double Y, double W, double H);
