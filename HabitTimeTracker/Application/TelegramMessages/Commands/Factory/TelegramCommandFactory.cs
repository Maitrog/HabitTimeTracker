using HabitTimeTracker.Application.TelegramMessages.Commands.Constants;
using HabitTimeTracker.Domain.Models;
using Telegram.Bot;

namespace HabitTimeTracker.Application.TelegramMessages.Commands.Factory;

public class TelegramCommandFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;

    public TelegramCommandFactory(
        IConfiguration configuration,
        IServiceProvider serviceProvider)
    {
        _configuration = configuration;
        _serviceProvider = serviceProvider;
    }

    public TelegramCommandBase? Create(ITelegramBotClient botClient, ChatState state, string? command = null)
    {
        if (command == TelegramCommand.Start)
            return new StartCommand(botClient);
        
        if (command == TelegramCommand.Reset)
            return new ResetCommand(botClient, _serviceProvider);

        if (state == ChatState.Default && command == TelegramCommand.CreateHabit)
            return new StartHabitCreationCommand(botClient, _serviceProvider);

        if (state == ChatState.HabitCreation)
            return new CreateHabitCommand(botClient, _serviceProvider);

        if (state == ChatState.Default && command == TelegramCommand.DeleteHabit)
            return new StartHabitDeletionCommand(botClient, _serviceProvider);

        if (state == ChatState.HabitDeletion)
            return new DeleteHabitCommand(botClient, _serviceProvider);
        
        if (state == ChatState.Default && command == TelegramCommand.ChooseHabit)
            return new ChooseHabitForTrackingCommand(botClient, _serviceProvider);

        if (state == ChatState.ChoosingHabitForStart && command == TelegramCommand.StartHabit)
            return new StartHabitTrackingCommand(botClient, _serviceProvider);

        if (state == ChatState.HabitInProgress  && command == TelegramCommand.FinishHabit)
            return new FinishHabitCommand(botClient, _serviceProvider);

        if (state == ChatState.Default && command == TelegramCommand.Statistics)
            return new StatisticsCommand(botClient, _serviceProvider);

        if (state == ChatState.Default && command == TelegramCommand.HeatmapHabit)
            return new StartHeatmapCommand(botClient, _serviceProvider);

        if (state == ChatState.Default && command == TelegramCommand.Heatmap)
            return new HeatmapCommand(botClient, _serviceProvider);

        if (state == ChatState.Default && command == TelegramCommand.EditLastPeriodFinish)
            return new StartEditLastPeriodFinishCommand(botClient, _serviceProvider);

        if (state == ChatState.EditingLastPeriodFinish)
            return new EditLastPeriodFinishCommand(botClient, _serviceProvider);

        if (state == ChatState.Default && command == TelegramCommand.AddMissedPeriod)
            return new StartAddMissedPeriodCommand(botClient, _serviceProvider);

        if (state == ChatState.ChoosingHabitForMissedPeriod && command == TelegramCommand.AddMissedPeriodHabit)
            return new StartMissedPeriodInputCommand(botClient, _serviceProvider);

        if (state == ChatState.AddingMissedPeriod)
            return new AddMissedPeriodCommand(botClient, _serviceProvider);

        if (state == ChatState.Default && command == TelegramCommand.TimeZone)
            return new StartTimeZoneChangeCommand(botClient, _serviceProvider);

        if (state == ChatState.ChangingTimeZone)
            return new SetTimeZoneCommand(botClient, _serviceProvider);

        return null;
    }
}