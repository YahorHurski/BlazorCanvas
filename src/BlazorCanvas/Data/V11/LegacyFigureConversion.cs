using BlazorCanvas.Shapes;

namespace BlazorCanvas.Data.V11;

/// <summary>
/// Converts v1.1 bounding-box rows to the v1.11 position-and-local-geometry model without changing
/// the picture they represent. Serialisation is deliberately excluded: FigureInputGateway remains
/// the single Phase 9 trust boundary for geometry JSON.
/// </summary>
public static class LegacyFigureConversion
{
    // No rounding, clamping, or canvas-bound involvement belongs here: migration copies positions and
    // subtracts extents exactly. It also produces typed records only; JSON remains FigureInputGateway's
    // single Phase 9 write boundary, and this converter deliberately owns no z, style, or bbox values.
    public static ConvertedFigure Convert(LegacyFigureRow row)
    {
        ArgumentNullException.ThrowIfNull(row);
        return Convert(row.Type, row.X1, row.Y1, row.X2, row.Y2);
    }

    public static ConvertedFigure Convert(string type, int x1, int y1, int x2, int y2) => type switch
    {
        "rectangle" => ConvertRectangle(x1, y1, x2, y2),
        "circle" => ConvertCircle(x1, y1, x2, y2),
        "triangle" => ConvertTriangle(x1, y1, x2, y2),
        "line" => ConvertLine(x1, y1, x2, y2),
        // A migration that skips a row it cannot understand is exactly the silent data loss MIGR-01 forbids.
        _ => throw new ArgumentOutOfRangeException(nameof(type), "Legacy figure type is unsupported."),
    };

    private static ConvertedFigure ConvertRectangle(int x1, int y1, int x2, int y2)
    {
        ThrowIfInvalidBox(x1, y1, x2, y2, "rectangle");
        return new ConvertedFigure(x1, y1, new RectangleGeometry(x2 - x1, y2 - y1));
    }

    private static ConvertedFigure ConvertCircle(int x1, int y1, int x2, int y2)
    {
        var side = x2 - x1;
        if (side <= 0 || side % 2 != 0 || y2 - y1 != side)
        {
            ThrowImpossibleCoordinates("circle");
        }

        return new ConvertedFigure(x1, y1, new CircleGeometry(side / 2.0));
    }

    private static ConvertedFigure ConvertTriangle(int x1, int y1, int x2, int y2)
    {
        ThrowIfInvalidBox(x1, y1, x2, y2, "triangle");
        var width = x2 - x1;
        var height = y2 - y1;

        // Literal v1.1 renderer transcription: apex at top-centre; base along the bottom, left then right.
        return new ConvertedFigure(x1, y1, new TriangleGeometry(
        [
            new LocalPoint(width / 2.0, 0),
            new LocalPoint(0, height),
            new LocalPoint(width, height),
        ]));
    }

    private static ConvertedFigure ConvertLine(int x1, int y1, int x2, int y2)
    {
        if (x2 < x1 || (x1 == x2 && y1 == y2))
        {
            ThrowImpossibleCoordinates("line");
        }

        // This is the only minimum operation: it sets the local origin and never reorders either endpoint.
        // Original point order carries diagonal direction, keeping D-41's normalisation landmine defused by D-60.
        // No sorting, swapping, or other canonicalisation of this pair is permitted.
        var y = Math.Min(y1, y2);
        return new ConvertedFigure(x1, y, new LineGeometry(
        [
            new LocalPoint(0, y1 - y),
            new LocalPoint(x2 - x1, y2 - y),
        ]));
    }

    private static void ThrowIfInvalidBox(int x1, int y1, int x2, int y2, string type)
    {
        if (x2 <= x1 || y2 <= y1)
        {
            ThrowImpossibleCoordinates(type);
        }
    }

    private static void ThrowImpossibleCoordinates(string type) =>
        // v1.1 CHECK constraints policed these rows; an impossible row must stop migration, not be invented.
        throw new InvalidOperationException($"Legacy {type} coordinates are impossible.");
}
