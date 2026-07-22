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
        Assert.Contains("StyleGateway.Parse(Figure.StyleJson)", figure);
        Assert.Contains("Style.Opacity * 0.7", figure);
        Assert.Contains("CultureInfo.InvariantCulture", figure);
        Assert.DoesNotContain("Box", figure);

        Assert.Contains("pointer-events=\"none\"", trace);
        Assert.Contains("#FFFFFF", trace);
        Assert.Contains("#1D4ED8", trace);
        Assert.Contains("stroke-dasharray=\"4 4\"", trace);
        Assert.Contains("rotate({Number(Figure.Rotation)})", trace);
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
}
