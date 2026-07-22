namespace BlazorCanvas.Data.V11.Transition;

/// <summary>Direct transcription of a legacy public.figures row.</summary>
public sealed record LegacyFigureRow(int Id, int UserId, string Type, int X1, int Y1, int X2, int Y2);
