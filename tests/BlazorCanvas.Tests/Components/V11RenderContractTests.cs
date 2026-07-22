namespace BlazorCanvas.Tests.Components;

public class V11RenderContractTests
{
    [Fact]
    public void LocalRenderers_UseOneInvariantLocalTransformAndRetainAppearance()
    {
        var figure = Source("FigureShape.razor");
        var trace = Source("SelectionTrace.razor");

        Assert.Contains("translate({Number(X)}, {Number(Y)}) rotate({Number(Rotation)})", figure);
        Assert.Contains("LineGeometry", figure);
        Assert.Contains("RectangleGeometry", figure);
        Assert.Contains("CircleGeometry", figure);
        Assert.Contains("TriangleGeometry", figure);
        Assert.Contains("case Star5Geometry star:", figure);
        Assert.Contains("points=\"@Points(star.Points)\"", figure);
        Assert.Contains("StyleGateway.Parse(Figure.StyleJson)", figure);
        Assert.Contains("Style.Opacity * 0.7", figure);
        Assert.Contains("CultureInfo.InvariantCulture", figure);
        Assert.DoesNotContain("B" + "ox", figure);

        var transformedRenderBody = ExtractBetween(figure, "<g transform=\"@Transform\">", "</g>");
        var triangleBranch = ExtractSwitchBranch(transformedRenderBody, "case TriangleGeometry triangle:");
        var starBranch = ExtractSwitchBranch(transformedRenderBody, "case Star5Geometry star:");

        Assert.Contains("case Star5Geometry star:", transformedRenderBody);
        Assert.Contains("points=\"@Points(star.Points)\"", starBranch);
        Assert.DoesNotContain("Star5Shape", starBranch);
        Assert.DoesNotContain("Math.", starBranch);
        AssertSameCommittedPolygonAttributes(triangleBranch, "triangle.Points");
        AssertSameCommittedPolygonAttributes(starBranch, "star.Points");

        Assert.Contains("pointer-events=\"none\"", trace);
        Assert.Contains("#FFFFFF", trace);
        Assert.Contains("#1D4ED8", trace);
        Assert.Contains("stroke-dasharray=\"4 4\"", trace);
        Assert.Contains("case Star5Geometry star:", trace);
        Assert.Contains("points=\"@Points(star.Points)\"", trace);
        Assert.Contains("rotate({Number(Figure.Rotation)})", trace);
    }

    [Fact]
    public void PersistedFigurePath_ParsesThroughRegistryAndFailsClosedForMalformedJson()
    {
        var figure = Source("FigureShape.razor");
        var persistedBranch = ExtractBetween(
            figure,
            "if (Figure is not null && Registry.TryGet(Figure.Type, out var definition))",
            "else if (PreviewPlacement is { } placement && Registry.Contains(PreviewType))");

        Assert.Contains("Registry.TryGet(Figure.Type, out var definition)", figure);
        Assert.Contains("JsonDocument.Parse(Figure.GeometryJson)", persistedBranch);
        Assert.Contains("definition.TryParseGeometry(document.RootElement, out var geometry)", persistedBranch);
        Assert.Contains("Geometry = geometry;", persistedBranch);
        Assert.Contains("catch (System.Text.Json.JsonException)", persistedBranch);
        Assert.DoesNotContain("MarkupString", persistedBranch);
        Assert.DoesNotContain("GeometryJson)", ExtractSwitchBranch(figure, "case Star5Geometry star:"));
    }

    [Fact]
    public void CanvasCoordinates_RetainsToolbarOffset()
    {
        Assert.Equal((20, 30), BlazorCanvas.Geometry.CanvasCoordinates.FromPage(20, 78));
        Assert.Equal(48, BlazorCanvas.Geometry.CanvasCoordinates.ToolbarHeight);
    }

    private static string Source(string name)
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var path = Path.Combine(directory.FullName, "src", "BlazorCanvas", "Components", "Canvas", name);
            if (File.Exists(path)) return File.ReadAllText(path);
        }

        throw new DirectoryNotFoundException("Could not locate the repository source tree.");
    }

    private static void AssertSameCommittedPolygonAttributes(string branch, string pointsExpression)
    {
        Assert.Contains($"points=\"@Points({pointsExpression})\"", branch);
        Assert.Contains("fill=\"@Style.Fill\"", branch);
        Assert.Contains("stroke=\"@Style.Stroke\"", branch);
        Assert.Contains("stroke-width=\"@Number(Style.StrokeWidth)\"", branch);
        Assert.Contains("fill-opacity=\"@Opacity\"", branch);
        Assert.Contains("stroke-opacity=\"@Opacity\"", branch);
        Assert.Contains("@onpointerdown=\"HandlePointerDown\"", branch);
        Assert.Contains("@onpointerdown:stopPropagation=\"Selectable\"", branch);
    }

    private static string ExtractSwitchBranch(string source, string caseLabel)
    {
        var start = source.IndexOf(caseLabel, StringComparison.Ordinal);
        Assert.True(start >= 0, $"Could not find switch branch '{caseLabel}'.");

        var end = source.IndexOf("break;", start, StringComparison.Ordinal);
        Assert.True(end >= 0, $"Could not find end of switch branch '{caseLabel}'.");

        return source[start..end];
    }

    private static string ExtractBetween(string source, string startMarker, string endMarker)
    {
        var start = source.IndexOf(startMarker, StringComparison.Ordinal);
        Assert.True(start >= 0, $"Could not find start marker '{startMarker}'.");
        start += startMarker.Length;

        var end = source.IndexOf(endMarker, start, StringComparison.Ordinal);
        Assert.True(end >= 0, $"Could not find end marker '{endMarker}'.");

        return source[start..end];
    }
}
