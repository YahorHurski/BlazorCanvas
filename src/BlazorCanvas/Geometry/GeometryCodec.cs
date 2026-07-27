using System.Text.Json;
using System.Text.Json.Serialization;

namespace BlazorCanvas.Geometry;

public readonly record struct FigureGeometry(int X, int Y, string Geometry);

/// <summary>
/// Maps the in-memory Box representation to the D-59 anchor plus geometry JSON storage model.
/// </summary>
public static class GeometryCodec
{
    public static FigureGeometry Encode(FigureType type, Box box) =>
        type switch
        {
            FigureType.Line => new(box.X1, box.Y1, JsonSerializer.Serialize(new LineGeometry(box.Width, box.Height))),
            FigureType.Rectangle => new(box.X1, box.Y1, JsonSerializer.Serialize(new BoxGeometry(box.Width, box.Height))),
            FigureType.Circle => EncodeCircle(box),
            FigureType.Triangle => new(box.X1, box.Y1, JsonSerializer.Serialize(new BoxGeometry(box.Width, box.Height))),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown figure type.")
        };

    public static Box DecodeToBox(FigureType type, int x, int y, string geometry) =>
        type switch
        {
            FigureType.Line => DecodeLine(x, y, geometry),
            FigureType.Rectangle => DecodeBox(x, y, geometry),
            FigureType.Circle => DecodeCircle(x, y, geometry),
            FigureType.Triangle => DecodeBox(x, y, geometry),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown figure type.")
        };

    private static FigureGeometry EncodeCircle(Box box)
    {
        var (cx, cy, r) = CircleEncoding.ToCentreRadius(box);
        return new(cx, cy, JsonSerializer.Serialize(new CircleGeometry(r)));
    }

    private static Box DecodeLine(int x, int y, string geometry)
    {
        var line = JsonSerializer.Deserialize<LineGeometry?>(geometry)
            ?? throw new InvalidOperationException("Line geometry JSON is empty.");

        return new Box(x, y, x + line.Dx, y + line.Dy);
    }

    private static Box DecodeBox(int x, int y, string geometry)
    {
        var box = JsonSerializer.Deserialize<BoxGeometry?>(geometry)
            ?? throw new InvalidOperationException("Box geometry JSON is empty.");

        return new Box(x, y, x + box.W, y + box.H);
    }

    private static Box DecodeCircle(int x, int y, string geometry)
    {
        var circle = JsonSerializer.Deserialize<CircleGeometry?>(geometry)
            ?? throw new InvalidOperationException("Circle geometry JSON is empty.");

        return CircleEncoding.FromCentreRadius(x, y, circle.R);
    }

    // Internal, not private: ShapeRender deserialises geometry JSON through these same wire-format
    // records rather than re-deriving the "w"/"h"/"r"/"dx"/"dy" member names, so a rename here
    // cannot silently diverge from the renderer (T-10-14).
    internal readonly record struct BoxGeometry(
        [property: JsonPropertyName("w")] int W,
        [property: JsonPropertyName("h")] int H);

    internal readonly record struct CircleGeometry([property: JsonPropertyName("r")] int R);

    internal readonly record struct LineGeometry(
        [property: JsonPropertyName("dx")] int Dx,
        [property: JsonPropertyName("dy")] int Dy);
}
