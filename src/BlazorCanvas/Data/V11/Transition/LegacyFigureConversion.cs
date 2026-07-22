using BlazorCanvas.Shapes;
namespace BlazorCanvas.Data.V11.Transition;

/// <summary>Preserves the legacy picture while changing it to local v1.11 geometry.</summary>
public static class LegacyFigureConversion
{
    public static ConvertedFigure Convert(LegacyFigureRow row) => Convert(row.Type, row.X1, row.Y1, row.X2, row.Y2);
    public static ConvertedFigure Convert(string type, int x1, int y1, int x2, int y2) => type switch
    {
        "rectangle" => Box(x1, y1, x2, y2, "rectangle", (w, h) => new RectangleGeometry(w, h)),
        "triangle" => Box(x1, y1, x2, y2, "triangle", (w, h) => new TriangleGeometry([new LocalPoint(w / 2.0, 0), new LocalPoint(0, h), new LocalPoint(w, h)])),
        "circle" when x2 > x1 && x2 - x1 == y2 - y1 && (x2 - x1) % 2 == 0 => new ConvertedFigure(x1, y1, new CircleGeometry((x2 - x1) / 2.0)),
        "line" when x2 >= x1 && (x1 != x2 || y1 != y2) => new ConvertedFigure(x1, Math.Min(y1, y2), new LineGeometry([new LocalPoint(0, y1 - Math.Min(y1, y2)), new LocalPoint(x2 - x1, y2 - Math.Min(y1, y2))])),
        "line" or "circle" or "rectangle" or "triangle" => throw new InvalidOperationException($"Legacy {type} coordinates are impossible."),
        _ => throw new ArgumentOutOfRangeException(nameof(type), "Legacy figure type is unsupported."),
    };
    private static ConvertedFigure Box(int x1, int y1, int x2, int y2, string type, Func<int, int, IFigureGeometry> geometry) => x2 > x1 && y2 > y1 ? new ConvertedFigure(x1, y1, geometry(x2 - x1, y2 - y1)) : throw new InvalidOperationException($"Legacy {type} coordinates are impossible.");
}
