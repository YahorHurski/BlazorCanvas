using System.Text.Json;
using System.Text.RegularExpressions;

namespace BlazorCanvas.Tests.Migrations;

internal sealed record FixtureManifestRow(
    int OldId,
    string Type,
    int OldX1,
    int OldY1,
    int OldX2,
    int OldY2,
    int X,
    int Y,
    IReadOnlyDictionary<string, int> Geometry,
    decimal Z);

internal static partial class FixtureManifest
{
    private static readonly Regex RowPattern = ManifestRowRegex();

    public static IReadOnlyList<FixtureManifestRow> Load(string path)
    {
        var rows = new List<FixtureManifestRow>();

        foreach (var line in File.ReadLines(path))
        {
            var match = RowPattern.Match(line);
            if (!match.Success)
            {
                continue;
            }

            rows.Add(new FixtureManifestRow(
                int.Parse(match.Groups["id"].Value),
                match.Groups["type"].Value,
                int.Parse(match.Groups["x1"].Value),
                int.Parse(match.Groups["y1"].Value),
                int.Parse(match.Groups["x2"].Value),
                int.Parse(match.Groups["y2"].Value),
                int.Parse(match.Groups["x"].Value),
                int.Parse(match.Groups["y"].Value),
                ParseGeometry(match.Groups["geometry"].Value),
                decimal.Parse(match.Groups["z"].Value)));
        }

        return rows;
    }

    private static IReadOnlyDictionary<string, int> ParseGeometry(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.EnumerateObject()
            .ToDictionary(p => p.Name, p => p.Value.GetInt32(), StringComparer.Ordinal);
    }

    [GeneratedRegex(@"^\|\s*(?<id>\d+)\s*\|\s*(?<type>\w+)\s*\|\s*(?<x1>-?\d+),(?<y1>-?\d+),(?<x2>-?\d+),(?<y2>-?\d+)\s*\|\s*(?<x>-?\d+),(?<y>-?\d+)\s*\|\s*`(?<geometry>\{[^`]+\})`\s*\|\s*(?<z>\d+)\s*\|$")]
    private static partial Regex ManifestRowRegex();
}
