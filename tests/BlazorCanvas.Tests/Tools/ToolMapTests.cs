using BlazorCanvas.Tools;

namespace BlazorCanvas.Tests.Tools;

public class ToolMapTests
{
    [Fact]
    public void ToShapeName_MapsArmableToolsToRegistryNames()
    {
        Assert.Null(ToolMap.ToShapeName(Tool.Pointer));
        Assert.Equal("line", ToolMap.ToShapeName(Tool.Line));
        Assert.Equal("rectangle", ToolMap.ToShapeName(Tool.Rectangle));
        Assert.Equal("circle", ToolMap.ToShapeName(Tool.Circle));
        Assert.Equal("triangle", ToolMap.ToShapeName(Tool.Triangle));
        Assert.Equal("star5", ToolMap.ToShapeName(Tool.Star));
    }

    [Fact]
    public void ToolEnum_ContainsOnlyArmableTools()
    {
        Assert.Equal(new[] { "Pointer", "Line", "Rectangle", "Circle", "Triangle", "Star" }, Enum.GetNames<Tool>());
        Assert.DoesNotContain("Delete", Enum.GetNames<Tool>());
    }
}
