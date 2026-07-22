namespace BlazorCanvas.Data.V11;

/// <summary>
/// A direct transcription of one v1.1 <c>public.figures</c> row. The old column names are retained
/// deliberately so the migration reads like the table it reads from.
/// </summary>
public sealed record LegacyFigureRow(int Id, int UserId, string Type, int X1, int Y1, int X2, int Y2);
