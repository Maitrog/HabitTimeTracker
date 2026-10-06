using HabitTimeTracker.Application.Heatmaps;
using HabitTimeTracker.Domain.Heatmaps;

namespace HabitTimeTracker.Tests;

public class HeatmapRendererTests
{
    [Theory]
    [InlineData(HeatmapPeriod.Last7Days)]
    [InlineData(HeatmapPeriod.Last30Days)]
    [InlineData(HeatmapPeriod.CurrentMonth)]
    [InlineData(HeatmapPeriod.CurrentYear)]
    public void Render_ProducesPng(HeatmapPeriod period)
    {
        var model = HeatmapLayoutBuilder.Build(
            period, new DateOnly(2026, 10, 6), new Dictionary<DateOnly, long>());

        var png = HeatmapRenderer.Render(model);

        Assert.True(png.Length > 8);
        Assert.Equal(new byte[] { 0x89, 0x50, 0x4E, 0x47 }, png[..4]);
    }
}
