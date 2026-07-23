using System.Buffers;
using System.Text;
using System.Text.Json;

namespace BlazorCanvas.Shapes;

/// <summary>
/// Shared JSON primitives for typed shape geometry.
/// </summary>
public static class GeometryJson
{
    /// <summary>
    /// Reads a finite numeric property from an already-parsed JSON object.
    /// </summary>
    public static bool TryReadFiniteDouble(JsonElement obj, string key, out double value)
    {
        value = default;

        return obj.ValueKind == JsonValueKind.Object
            && obj.TryGetProperty(key, out var property)
            && property.ValueKind == JsonValueKind.Number
            && property.TryGetDouble(out value)
            && double.IsFinite(value);
    }

    /// <summary>
    /// Reads an exactly sized, ordered list of finite local points from an already-parsed JSON object.
    /// </summary>
    public static bool TryReadPointList(
        JsonElement obj,
        string key,
        int requiredCount,
        out IReadOnlyList<LocalPoint> points)
    {
        points = Array.Empty<LocalPoint>();

        if (obj.ValueKind != JsonValueKind.Object
            || !obj.TryGetProperty(key, out var property)
            || property.ValueKind != JsonValueKind.Array
            || property.GetArrayLength() != requiredCount)
        {
            return false;
        }

        var result = new List<LocalPoint>(requiredCount);
        foreach (var item in property.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Array || item.GetArrayLength() != 2)
            {
                return false;
            }

            var coordinates = item.EnumerateArray();
            coordinates.MoveNext();
            var x = coordinates.Current;
            coordinates.MoveNext();
            var y = coordinates.Current;

            if (!TryReadFiniteNumber(x, out var xValue) || !TryReadFiniteNumber(y, out var yValue))
            {
                return false;
            }

            result.Add(new LocalPoint(xValue, yValue));
        }

        points = result;
        return true;
    }

    /// <summary>
    /// Writes a named number through <see cref="Utf8JsonWriter"/>.
    /// </summary>
    public static void WriteNumber(Utf8JsonWriter writer, string propertyName, double value) =>
        writer.WriteNumber(propertyName, value);

    /// <summary>
    /// Writes an ordered point list through <see cref="Utf8JsonWriter"/> only. It formats numbers
    /// with an invariant, shortest round-trippable representation, preventing comma-decimal cultures
    /// from silently changing coordinates.
    /// </summary>
    public static void WritePoints(Utf8JsonWriter writer, string propertyName, IReadOnlyList<LocalPoint> points)
    {
        writer.WriteStartArray(propertyName);
        foreach (var point in points)
        {
            writer.WriteStartArray();
            writer.WriteNumberValue(point.X);
            writer.WriteNumberValue(point.Y);
            writer.WriteEndArray();
        }

        writer.WriteEndArray();
    }

    /// <summary>
    /// Produces byte-stable, minified UTF-8 JSON.
    /// </summary>
    public static string Serialise(Action<Utf8JsonWriter> body)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = false });
        body(writer);
        writer.Flush();
        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    private static bool TryReadFiniteNumber(JsonElement value, out double number)
    {
        number = default;
        return value.ValueKind == JsonValueKind.Number
            && value.TryGetDouble(out number)
            && double.IsFinite(number);
    }
}
