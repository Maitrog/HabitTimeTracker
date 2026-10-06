using HabitTimeTracker.Domain.Heatmaps;

namespace HabitTimeTracker.Tests;

public class HeatmapLayoutBuilderTests
{
    private static readonly DateOnly Today = new(2026, 10, 6); // вторник

    [Fact]
    public void Last7Days_IsSingleRowOfSeven()
    {
        var model = HeatmapLayoutBuilder.Build(HeatmapPeriod.Last7Days, Today, new Dictionary<DateOnly, long>());
        Assert.Equal(1, model.Rows);
        Assert.Equal(7, model.Columns);
        Assert.Equal(7, model.Cells.Count);
        Assert.Equal("30.09", model.LeftDateLabel);
        Assert.Equal("06.10", model.RightDateLabel);
    }

    [Fact]
    public void Last30Days_GridStartsOnMondayAndStaysInRange()
    {
        var model = HeatmapLayoutBuilder.Build(HeatmapPeriod.Last30Days, Today, new Dictionary<DateOnly, long>());
        Assert.Equal(7, model.Rows);
        Assert.True(model.Columns is >= 5 and <= 6);
        var first = model.Cells.MinBy(c => c.Column)!;
        Assert.Equal(DayOfWeek.Monday, first.Date.DayOfWeek);
        Assert.All(model.Cells, c => Assert.InRange(c.Date, Today.AddDays(-29), Today));
        Assert.Equal(model.Columns, model.ColumnHeaders.Count);
    }

    [Fact]
    public void CurrentMonth_TitleAndDaysWithinMonth()
    {
        var model = HeatmapLayoutBuilder.Build(HeatmapPeriod.CurrentMonth, Today, new Dictionary<DateOnly, long>());
        Assert.Equal("Октябрь 2026", model.Title);
        Assert.All(model.Cells, c => Assert.Equal(10, c.Date.Month));
        Assert.Equal(31, model.Cells.Count);
    }

    [Fact]
    public void CurrentYear_HasTwelveMonthHeaders()
    {
        var model = HeatmapLayoutBuilder.Build(HeatmapPeriod.CurrentYear, Today, new Dictionary<DateOnly, long>());
        Assert.Equal(365, model.Cells.Count);
        Assert.Equal(12, model.ColumnHeaders.Count);
        Assert.All(model.Cells, c => Assert.Equal(2026, c.Date.Year));
    }
}
