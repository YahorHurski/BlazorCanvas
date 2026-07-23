using System.Text.RegularExpressions;

namespace BlazorCanvas.Shapes;

// These defaults reproduce FigureShape.razor's committed appearance exactly; Phase 10 writes them
// to every migrated row so existing figures do not change on screen (D-66, MODEL-06, MIGR-01).
public sealed partial record FigureStyle(
    string Stroke = "#000000",
    double StrokeWidth = 2,
    string Fill = "#FFFFFF",
    double Opacity = 1)
{
    // This anchored, fixed-width character class has no alternation, nested quantifier, or
    // backreference, so catastrophic backtracking is not expressible.
    [GeneratedRegex("^#[0-9A-Fa-f]{6}$", RegexOptions.CultureInvariant)]
    private static partial Regex ColourRegex();

    public FigureStyle Sanitised()
    {
        var stroke = Stroke is not null && ColourRegex().IsMatch(Stroke) ? Stroke : "#000000";
        var fill = Fill is not null && ColourRegex().IsMatch(Fill) ? Fill : "#FFFFFF";

        // Math.Clamp(double.NaN, ...) returns NaN; replace non-finite values first so NaN cannot be
        // stored and later emitted as the literal SVG stroke-width or opacity attribute text.
        var strokeWidth = double.IsFinite(StrokeWidth) ? StrokeWidth : 2;
        strokeWidth = Math.Clamp(strokeWidth, 0.5, 64);

        var opacity = double.IsFinite(Opacity) ? Opacity : 1;
        opacity = Math.Clamp(opacity, 0, 1);

        return new FigureStyle(stroke, strokeWidth, fill, opacity);
    }
}
