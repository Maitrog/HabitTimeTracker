using HabitTimeTracker.Application;
using HabitTimeTracker.Application.TelegramMessages.Commands.Factory;
using HabitTimeTracker.Application.TelegramMessages.Messages;
using HabitTimeTracker.DataAccess;
using HabitTimeTracker.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Telegram.Bot;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<TelegramBotSettings>(builder.Configuration.GetSection("TelegramBotSettings"));
builder.Services.AddSingleton<ITelegramBotClient>(sp
    => new TelegramBotClient(sp.GetRequiredService<IOptions<TelegramBotSettings>>().Value.Token));

builder.Services.AddDbContext<HabitTimeTrackerDataContext>(options
    => options.UseNpgsql(builder.Configuration.GetConnectionString("HabitTimeTrackerBot")));

builder.Services.AddScoped<TextMessageHandler>();
builder.Services.AddScoped<TelegramCommandFactory>();

builder.Services.AddHostedService<TelegramBotWorker>();

var host = builder.Build();

using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<HabitTimeTrackerDataContext>();
    db.Database.Migrate();
}

host.Run();