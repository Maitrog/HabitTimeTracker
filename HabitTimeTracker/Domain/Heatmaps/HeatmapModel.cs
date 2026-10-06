namespace HabitTimeTracker.Domain.Heatmaps;

public sealed record HeatmapCell(int Row, int Column, DateOnly Date, long DurationSeconds);

public sealed record HeatmapModel(
    int Rows,
    int Columns,
    string Title,
    IReadOnlyList<HeatmapCell> Cells,
    IReadOnlyList<string> WeekdayLabels,
    IReadOnlyList<(int Column, string Text)> ColumnHeaders,
    string? LeftDateLabel,
    string? RightDateLabel);
