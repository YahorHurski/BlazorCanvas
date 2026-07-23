using BlazorCanvas.Data.V11;

namespace BlazorCanvas.Geometry;

/// <summary>
/// Movement for v1.11 local geometry. The bbox cache lives in the figure's local frame, so moving
/// a figure updates only x/y and leaves all geometry-derived bbox columns valid.
/// </summary>
public static class V11Movement
{
    public static (decimal X, decimal Y) ClampPosition(FigureRow figure, decimal requestedX, decimal requestedY)
    {
        ArgumentNullException.ThrowIfNull(figure);

        var left = requestedX + (decimal)figure.BboxX;
        var top = requestedY + (decimal)figure.BboxY;
        var right = left + (decimal)figure.BboxW;
        var bottom = top + (decimal)figure.BboxH;

        // The old move semantics deliberately do not teleport a shape whose extent cannot fit.
        var minX = -(decimal)figure.BboxX;
        var maxX = CanvasBounds.Width - (decimal)figure.BboxX - (decimal)figure.BboxW;
        var minY = -(decimal)figure.BboxY;
        var maxY = CanvasBounds.Height - (decimal)figure.BboxY - (decimal)figure.BboxH;

        var x = minX > maxX ? figure.X : Math.Min(Math.Max(requestedX, minX), maxX);
        var y = minY > maxY ? figure.Y : Math.Min(Math.Max(requestedY, minY), maxY);
        return (x, y);
    }
}
