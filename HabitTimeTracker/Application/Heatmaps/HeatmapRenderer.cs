using HabitTimeTracker.Domain.Heatmaps;
using SkiaSharp;

namespace HabitTimeTracker.Application.Heatmaps;

public static class HeatmapRenderer
{
    private const int Cell = 16;
    private const int Gap = 4;
    private const int LeftGutter = 30;
    private const int TopHeader = 20;
    private const int TitleHeight = 24;
    private const int Padding = 8;

    private static readonly SKColor Empty = new(0xEB, 0xED, 0xF0);
    private static readonly SKColor[] Levels =
    [
        new(0x9B, 0xE9, 0xA8),
        new(0x40, 0xC4, 0x63),
        new(0x30, 0xA1, 0x4E),
        new(0x21, 0x6E, 0x39)
    ];
    private static readonly SKColor Text = new(0x57, 0x60, 0x6A);

    public static byte[] Render(HeatmapModel model)
    {
        var gridWidth = model.Columns * Cell + (model.Columns - 1) * Gap;
        var gridHeight = model.Rows * Cell + (model.Rows - 1) * Gap;
        var leftGutter = model.WeekdayLabels.Count > 0 ? LeftGutter : 0;
        var headerHeight = model.ColumnHeaders.Count > 0 ? TopHeader : 0;
        var topRows = TitleHeight + headerHeight;
        var bottom = model.LeftDateLabel is null ? 0 : TopHeader;

        var width = Padding * 2 + leftGutter + gridWidth;
        var height = Padding * 2 + topRows + gridHeight + bottom;

        using var surface = SKSurface.Create(new SKImageInfo(width, height));
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.White);

        using var typeface = SKTypeface.FromFamilyName("DejaVu Sans") ?? SKTypeface.Default;
        using var font = new SKFont(typeface, 12);
        using var textPaint = new SKPaint { Color = Text, IsAntialias = true };
        using var cellPaint = new SKPaint { IsAntialias = true };

        canvas.DrawText(model.Title, Padding, Padding + font.Size, font, textPaint);

        var gridX = Padding + leftGutter;
        var gridY = Padding + topRows;

        foreach (var (column, text) in model.ColumnHeaders)
        {
            canvas.DrawText(text, gridX + column * (Cell + Gap), Padding + TitleHeight + font.Size, font, textPaint);
        }

        for (var r = 0; r < model.WeekdayLabels.Count; r++)
        {
            var y = gridY + r * (Cell + Gap) + Cell / 2 + font.Size / 3;
            canvas.DrawText(model.WeekdayLabels[r], Padding, y, font, textPaint);
        }

        var max = model.Cells.Count == 0 ? 0 : model.Cells.Max(c => c.DurationSeconds);
        foreach (var cell in model.Cells)
        {
            cellPaint.Color = ColorFor(cell.DurationSeconds, max);
            canvas.DrawRoundRect(
                gridX + cell.Column * (Cell + Gap),
                gridY + cell.Row * (Cell + Gap),
                Cell, Cell, 2, 2, cellPaint);
        }

        var labelY = gridY + gridHeight + font.Size + 2;
        if (model.LeftDateLabel is not null)
            canvas.DrawText(model.LeftDateLabel, gridX, labelY, font, textPaint);
        if (model.RightDateLabel is not null)
        {
            var lastCellX = gridX + (model.Columns - 1) * (Cell + Gap);
            canvas.DrawText(model.RightDateLabel,
                lastCellX + Cell - font.MeasureText(model.RightDateLabel), labelY, font, textPaint);
        }

        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    private static SKColor ColorFor(long seconds, long max)
    {
        if (seconds <= 0 || max <= 0)
            return Empty;

        var level = (int)Math.Ceiling(4.0 * seconds / max);
        return Levels[Math.Clamp(level, 1, 4) - 1];
    }
}
