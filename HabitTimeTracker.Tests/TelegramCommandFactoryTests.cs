using HabitTimeTracker.Application.TelegramMessages.Commands;
using HabitTimeTracker.Application.TelegramMessages.Commands.Constants;
using HabitTimeTracker.Application.TelegramMessages.Commands.Factory;
using HabitTimeTracker.Domain.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HabitTimeTracker.Tests;

public class TelegramCommandFactoryTests
{
    private static TelegramCommandFactory NewFactory() =>
        new(new ConfigurationBuilder().Build(), new ServiceCollection().BuildServiceProvider());

    [Fact]
    public void HabitManagementRoutes_FromDefault()
    {
        var factory = NewFactory();

        Assert.IsType<HabitManagementCommand>(factory.Create(null!, ChatState.Default, TelegramCommand.HabitManagement));
        Assert.IsType<DeleteHabitCommand>(factory.Create(null!, ChatState.Default, TelegramCommand.DeleteHabit));
        Assert.IsType<StartMissedPeriodInputCommand>(factory.Create(null!, ChatState.Default, TelegramCommand.AddMissedPeriodHabit));
        Assert.IsType<StatisticsCommand>(factory.Create(null!, ChatState.Default, TelegramCommand.Statistics));
        Assert.IsType<HeatmapCommand>(factory.Create(null!, ChatState.Default, TelegramCommand.Heatmap));
    }

    [Fact]
    public void RemovedCommands_NoLongerRoute()
    {
        var factory = NewFactory();

        Assert.Null(factory.Create(null!, ChatState.Default, "/addmissedperiod"));
        Assert.Null(factory.Create(null!, ChatState.Default, "/heatmaphabit"));
        Assert.Null(factory.Create(null!, (ChatState)2, TelegramCommand.DeleteHabit));
        Assert.Null(factory.Create(null!, (ChatState)6, TelegramCommand.AddMissedPeriodHabit));
    }

    [Fact]
    public void InputStates_StillRoute()
    {
        var factory = NewFactory();

        Assert.IsType<CreateHabitCommand>(factory.Create(null!, ChatState.HabitCreation, "Привычка"));
        Assert.IsType<StartHabitTrackingCommand>(factory.Create(null!, ChatState.ChoosingHabitForStart, TelegramCommand.StartHabit));
        Assert.IsType<AddMissedPeriodCommand>(factory.Create(null!, ChatState.AddingMissedPeriod, "06.10.2026 14:30; 45"));
    }
}
