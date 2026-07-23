using System.Text.Json;

namespace BlazorCanvas.Shapes;

/// <summary>
/// Parses untrusted style JSON into the bounded typed form and writes only that form back out.
/// </summary>
public static class StyleGateway
{
    private static readonly JsonDocumentOptions ParseOptions = new()
    {
        MaxDepth = 32,
        AllowTrailingCommas = false,
        CommentHandling = JsonCommentHandling.Disallow,
    };

    public static FigureStyle Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new FigureStyle().Sanitised();
        }

        try
        {
            using var document = JsonDocument.Parse(json, ParseOptions);
            return Parse(document.RootElement);
        }
        catch (JsonException)
        {
            return new FigureStyle().Sanitised();
        }
    }

    public static FigureStyle Parse(JsonElement json)
    {
        if (json.ValueKind != JsonValueKind.Object)
        {
            return new FigureStyle().Sanitised();
        }

        var stroke = TryReadString(json, "stroke", "#000000");
        var strokeWidth = TryReadNumber(json, "stroke_width", 2);
        var fill = TryReadString(json, "fill", "#FFFFFF");
        var opacity = TryReadNumber(json, "opacity", 1);

        return new FigureStyle(stroke, strokeWidth, fill, opacity).Sanitised();
    }

    public static string ToJson(FigureStyle style)
    {
        var sanitised = style.Sanitised();

        return GeometryJson.Serialise(writer =>
        {
            writer.WriteStartObject();
            writer.WriteString("stroke", sanitised.Stroke);
            writer.WriteNumber("stroke_width", sanitised.StrokeWidth);
            writer.WriteString("fill", sanitised.Fill);
            writer.WriteNumber("opacity", sanitised.Opacity);
            writer.WriteEndObject();
        });
    }

    private static string TryReadString(JsonElement json, string propertyName, string defaultValue) =>
        json.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString() ?? defaultValue
            : defaultValue;

    private static double TryReadNumber(JsonElement json, string propertyName, double defaultValue) =>
        json.TryGetProperty(propertyName, out var property)
            && property.ValueKind == JsonValueKind.Number
            && property.TryGetDouble(out var value)
            ? value
            : defaultValue;
}
