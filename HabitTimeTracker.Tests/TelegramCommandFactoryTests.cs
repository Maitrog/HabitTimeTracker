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
    public void EpochActions_RouteOnlyInTheirDialogStates()
    {
        var factory = NewFactory();
        Assert.IsType<StartEpochCompletionCommand>(factory.Create(null!, ChatState.Default, "/endepoch"));
        Assert.IsType<EpochHistoryCommand>(factory.Create(null!, ChatState.Default, "/epochs"));
        Assert.IsType<CompleteEpochCommand>(factory.Create(null!, ChatState.NamingEpoch, "Книга"));
        Assert.IsType<ResetCommand>(factory.Create(null!, ChatState.NamingEpoch, "/reset"));
        Assert.Null(factory.Create(null!, ChatState.HabitInProgress, "/endepoch"));
        Assert.IsType<SelectMissedPeriodEpochCommand>(factory.Create(null!, ChatState.ChoosingMissedPeriodEpoch, "/pickepoch"));
        Assert.IsType<SelectMissedPeriodEpochCommand>(factory.Create(null!, ChatState.ChoosingMissedPeriodEpoch, "/epochpage"));
        Assert.Null(factory.Create(null!, ChatState.Default, "/pickepoch"));
    }

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
