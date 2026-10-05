using HabitTimeTracker.Application.TelegramMessages.Messages;
using HabitTimeTracker.DataAccess;
using HabitTimeTracker.Domain;
using HabitTimeTracker.Domain.ValueObjects;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace HabitTimeTracker.Application;
using User = Domain.Models.User;

public class TelegramBotWorker(
    IOptions<TelegramBotSettings> telegramBotSettings,
    IServiceProvider serviceProvider)
    : BackgroundService
{
    private readonly TelegramBotSettings _telegramBotSettings = telegramBotSettings.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var botClient = new TelegramBotClient(_telegramBotSettings.Token);

        ReceiverOptions receiverOptions = new()
        {
            AllowedUpdates = [UpdateType.Message, UpdateType.CallbackQuery]
        };

        botClient.StartReceiving(
            updateHandler: HandleUpdateAsync,
            errorHandler: HandlePollingErrorAsync,
            receiverOptions: receiverOptions,
            cancellationToken: stoppingToken);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken ct)
    {
        using var scope = serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<HabitTimeTrackerDataContext>();
        var messageHandler = scope.ServiceProvider.GetRequiredService<TextMessageHandler>();

        User? user = null;
        var command = string.Empty;
        var commandData = string.Empty;

        if (update.CallbackQuery is { } callback)
        {
            await botClient.AnswerCallbackQuery(callback.Id, cancellationToken: ct);
            
            var data = callback.Data;
            user = await repository.GetUserByTelegramIdAsync(new TelegramId(callback.From.Id));
            if (user is null || data is null)
                return;

            var splitData = data.Split(' ');
            if (splitData.Length == 2)
            {
                command = splitData[0];
                commandData = splitData[1];
            }
            else
            {
                command = data;
            }
        }

        if (update.Message is { From: not null } message)
        {
            user = await repository.GetUserByTelegramIdAsync(new TelegramId(message.From.Id));
            if (user is null)
            {
                user = User.FromTelegramUser(message.From);
                await repository.CreateUserAsync(user, ct);
            }

            if (message is { Type: MessageType.Text, Text: not null })
            {
                command = message.Text;
                commandData = message.Text;
            }
        }

        await messageHandler.Handle(user, command, commandData, ct);
    }

    private static Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        var errorMessage = exception switch
                           {
                               ApiRequestException apiRequestException
                                   => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                               _ => exception.ToString()
                           };

        Console.WriteLine(errorMessage);
        return Task.CompletedTask;
    }
}