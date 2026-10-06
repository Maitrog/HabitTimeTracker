using HabitTimeTracker.Application.Heatmaps;
using HabitTimeTracker.Domain.Heatmaps;

namespace HabitTimeTracker.Tests;

public class HeatmapRendererTests
{
    [Fact]
    public void Render_ProducesPng()
    {
        var model = HeatmapLayoutBuilder.Build(
            HeatmapPeriod.Last7Days, new DateOnly(2026, 10, 6), new Dictionary<DateOnly, long>());

        var png = HeatmapRenderer.Render(model);

        Assert.True(png.Length > 8);
        Assert.Equal(new byte[] { 0x89, 0x50, 0x4E, 0x47 }, png[..4]);
    }
}
