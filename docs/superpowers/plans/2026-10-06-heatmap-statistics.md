# Heatmap-статистика по привычкам — план реализации

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** В разделе «Статистика» по каждой привычке открывать PNG-heatmap (квадратики как на GitHub) за 7 дней / 30 дней / текущий месяц / текущий год, с подписями дат, дней недели и месяцев; итоги — отдельным текстовым сообщением.

**Architecture:** Чистое ядро (`DailyDurationCalculator` + `HeatmapLayoutBuilder`) отделено от рендера (SkiaSharp). Навигация drill-down без нового `ChatState`: id привычки и период кодируются в callback (`heatmap 7:<guid>`). Пользователь хранит фиксированное смещение UTC (`TimeZoneOffsetMinutes`, дефолт 180) с ручной сменой через бота.

**Tech Stack:** .NET 10, Telegram.Bot 22, EF Core 10 + PostgreSQL, SkiaSharp 3.x, xunit.

**Spec:** этот документ.

## Global Constraints

- Язык сообщений бота — русский; комментарии по необходимости, `ponytail:` для осознанных упрощений.
- Callback-данные разбиваются по пробелу максимум на 2 части (`TelegramBotWorker.HandleUpdateAsync`); внутри data использовать `:` как разделитель (пробел занят).
- Неделя начинается с понедельника; локальный день = `UTC + offsetMinutes`.
- Фиксированное смещение UTC не учитывает DST — осознанный потолок, помечается `ponytail:`.
- Шкала цвета — относительно максимума диапазона, 5 уровней (GitHub-палитра).
- TDD: сначала падающий тест, затем минимальная реализация. Каждая задача заканчивается `dotnet test` (зелёный) и коммитом.

---

### Task 1: Пояс пользователя (домен + БД + команды смены)

**Files:**
- Modify: `HabitTimeTracker/Domain/Models/User.cs`
- Modify: `HabitTimeTracker/Domain/Models/ChatState.cs`
- Modify: `HabitTimeTracker/Application/TelegramMessages/Commands/Constants/TelegramCommand.cs`
- Modify: `HabitTimeTracker/Application/TelegramMessages/Commands/TelegramCommandBase.cs`
- Modify: `HabitTimeTracker/Application/TelegramMessages/Commands/Factory/TelegramCommandFactory.cs`
- Modify: `HabitTimeTracker/DataAccess/HabitTimeTrackerDataContext.cs`
- Create: `HabitTimeTracker/Application/TelegramMessages/Commands/StartTimeZoneChangeCommand.cs`
- Create: `HabitTimeTracker/Application/TelegramMessages/Commands/SetTimeZoneCommand.cs`
- Create: `HabitTimeTracker/DataAccess/Migrations/*_AddUserTimeZoneOffset.cs` (через `dotnet ef`)
- Test: `HabitTimeTracker.Tests/UserTimeZoneTests.cs`

**Interfaces (Produces для других задач):**
- `ChatState.ChangingTimeZone`
- `User.TimeZoneOffsetMinutes` (int, дефолт 180), `bool StartChangingTimeZone()`, `bool SetTimeZoneOffsetMinutes(int)` (диапазон `[-720, 840]`)
- `TelegramCommand.TimeZone = "/timezone"`, `HeatmapHabit = "/heatmaphabit"`, `Heatmap = "/heatmap"` (последние две — только константы, используются в задаче 5)

- [ ] **Step 1: Падающий тест домена**

`HabitTimeTracker.Tests/UserTimeZoneTests.cs`:
```csharp
using HabitTimeTracker.Domain.Models;

namespace HabitTimeTracker.Tests;

public class UserTimeZoneTests
{
    private static User NewUser() =>
        User.FromTelegramUser(new Telegram.Bot.Types.User { Id = 1, FirstName = "Test" });

    [Fact]
    public void DefaultOffsetIs180()
    {
        Assert.Equal(180, NewUser().TimeZoneOffsetMinutes);
    }

    [Fact]
    public void SetValidOffset_ChangesAndResetsState()
    {
        var user = NewUser();
        Assert.True(user.StartChangingTimeZone());
        Assert.True(user.SetTimeZoneOffsetMinutes(-300));
        Assert.Equal(-300, user.TimeZoneOffsetMinutes);
        Assert.Equal(ChatState.Default, user.ChatState);
    }

    [Theory]
    [InlineData(-721)]
    [InlineData(841)]
    public void SetInvalidOffset_IsRejected(int minutes)
    {
        var user = NewUser();
        Assert.False(user.SetTimeZoneOffsetMinutes(minutes));
        Assert.Equal(180, user.TimeZoneOffsetMinutes);
    }
}
```

- [ ] **Step 2: Запустить — убедиться, что падает**

Run: `dotnet test HabitTimeTracker.Tests --filter UserTimeZoneTests`
Expected: FAIL (нет `TimeZoneOffsetMinutes`/`StartChangingTimeZone`/`ChangingTimeZone`).

- [ ] **Step 3: Минимальная реализация домена**

`ChatState.cs` — добавить `ChangingTimeZone` в конец enum.

`User.cs`:
```csharp
public int TimeZoneOffsetMinutes { get; private set; } = 180;

public bool StartChangingTimeZone()
{
    if (ChatState != ChatState.Default)
        return false;

    ChatState = ChatState.ChangingTimeZone;
    Updated = DateTime.UtcNow;
    return true;
}

public bool SetTimeZoneOffsetMinutes(int minutes)
{
    if (minutes is < -720 or > 840)
        return false;

    TimeZoneOffsetMinutes = minutes;
    ResetState();
    return true;
}
```

- [ ] **Step 4: Запустить тест — зелёный**

Run: `dotnet test HabitTimeTracker.Tests --filter UserTimeZoneTests`
Expected: PASS (4 теста).

- [ ] **Step 5: Конфиг EF + миграция**

`HabitTimeTrackerDataContext.OnModelCreating`, внутри `modelBuilder.Entity<User>(b => { ... })`:
```csharp
b.Property(u => u.TimeZoneOffsetMinutes).HasDefaultValue(180);
```
Сгенерировать миграцию:
```bash
dotnet ef migrations add AddUserTimeZoneOffset --project HabitTimeTracker
```
Expected: создан файл миграции (или ошибка «No changes» — тогда проверить, что default-конфиг добавлен).

- [ ] **Step 6: Команды смены пояса**

`Constants/TelegramCommand.cs` — добавить:
```csharp
public const string TimeZone = "/timezone";
public const string HeatmapHabit = "/heatmaphabit";
public const string Heatmap = "/heatmap";
```

`StartTimeZoneChangeCommand.cs`:
```csharp
using HabitTimeTracker.Application.TelegramMessages.Commands.Constants;
using HabitTimeTracker.DataAccess;
using HabitTimeTracker.Domain.Models;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace HabitTimeTracker.Application.TelegramMessages.Commands;

public class StartTimeZoneChangeCommand(ITelegramBotClient botClient, IServiceProvider serviceProvider)
    : TelegramCommandBase(botClient)
{
    public override async Task Execute(User user, string? data, CancellationToken ct = default)
    {
        var repository = serviceProvider.GetRequiredService<HabitTimeTrackerDataContext>();

        if (!user.StartChangingTimeZone())
        {
            await SendBaseAnswerAsync(user, ct);
            return;
        }

        await repository.UpdateUserAsync(user, ct);

        await BotClient.SendMessage(
            chatId: user.TelegramId.Id,
            text: $"Введите смещение от UTC в минутах (сейчас {user.TimeZoneOffsetMinutes}, например 180 для Москвы):",
            replyMarkup: new InlineKeyboardMarkup(
                [InlineKeyboardButton.WithCallbackData("Назад в главное меню", TelegramCommand.Reset)]),
            cancellationToken: ct);
    }
}
```

`SetTimeZoneCommand.cs`:
```csharp
using HabitTimeTracker.DataAccess;
using HabitTimeTracker.Domain.Models;
using Telegram.Bot;

namespace HabitTimeTracker.Application.TelegramMessages.Commands;

public class SetTimeZoneCommand(ITelegramBotClient botClient, IServiceProvider serviceProvider)
    : TelegramCommandBase(botClient)
{
    public override async Task Execute(User user, string? data, CancellationToken ct = default)
    {
        var repository = serviceProvider.GetRequiredService<HabitTimeTrackerDataContext>();

        if (data == null || !int.TryParse(data.Trim(), out var minutes) || !user.SetTimeZoneOffsetMinutes(minutes))
        {
            await BotClient.SendMessage(
                chatId: user.TelegramId.Id,
                text: "Некорректное смещение. Введите целое число минут от -720 до 840:",
                cancellationToken: ct);
            return;
        }

        await repository.UpdateUserAsync(user, ct);

        await BotClient.SendMessage(
            chatId: user.TelegramId.Id,
            text: $"Часовой пояс сохранён: UTC{(minutes >= 0 ? "+" : string.Empty)}{minutes} мин.",
            replyMarkup: DefaultMenu,
            cancellationToken: ct);
    }
}
```

- [ ] **Step 7: Меню + фабрика**

`TelegramCommandBase.DefaultMenu` — добавить кнопку:
```csharp
[InlineKeyboardButton.WithCallbackData("Часовой пояс", TelegramCommand.TimeZone)]
```
`TelegramCommandFactory.Create` — добавить ветки:
```csharp
if (state == ChatState.Default && command == TelegramCommand.TimeZone)
    return new StartTimeZoneChangeCommand(botClient, _serviceProvider);

if (state == ChatState.ChangingTimeZone)
    return new SetTimeZoneCommand(botClient, _serviceProvider);
```

- [ ] **Step 8: Сборка/тесты + коммит**

Run: `dotnet build && dotnet test`
Expected: PASS.
```bash
git add -A && git commit -m "feat: add per-user time zone with change flow"
```

---

### Task 2: DailyDurationCalculator (чистое ядро)

**Files:**
- Create: `HabitTimeTracker/Domain/Heatmaps/DailyDurationCalculator.cs`
- Test: `HabitTimeTracker.Tests/DailyDurationCalculatorTests.cs`

**Interfaces:**
- Consumes: `TimePeriod` (`StartedAt`, `FinishAt`).
- Produces: `static Dictionary<DateOnly, long> DailyDurationCalculator.Calculate(IEnumerable<TimePeriod> periods, int offsetMinutes, DateTime nowUtc)`.

- [ ] **Step 1: Падающий тест**

`DailyDurationCalculatorTests.cs`:
```csharp
using HabitTimeTracker.Domain.Heatmaps;
using HabitTimeTracker.Domain.Models;

namespace HabitTimeTracker.Tests;

public class DailyDurationCalculatorTests
{
    private static TimePeriod Period(DateTime start, DateTime? finish)
    {
        var p = TimePeriod.StartHabit(Guid.NewGuid(), start);
        if (finish is { } f)
            p.FinishHabit(f);
        return p;
    }

    [Fact]
    public void SinglePeriodWithinDay()
    {
        var start = new DateTime(2026, 10, 6, 10, 0, 0, DateTimeKind.Utc);
        var result = DailyDurationCalculator.Calculate([Period(start, start.AddHours(2))], 0, DateTime.UtcNow);
        Assert.Equal(7200, result[new DateOnly(2026, 10, 6)]);
    }

    [Fact]
    public void PeriodSpanningMidnight_SplitsAcrossDays()
    {
        var start = new DateTime(2026, 10, 6, 23, 0, 0, DateTimeKind.Utc);
        var result = DailyDurationCalculator.Calculate([Period(start, start.AddHours(2))], 0, DateTime.UtcNow);
        Assert.Equal(3600, result[new DateOnly(2026, 10, 6)]);
        Assert.Equal(3600, result[new DateOnly(2026, 10, 7)]);
    }

    [Fact]
    public void OffsetMovesPeriodToNextLocalDay()
    {
        var start = new DateTime(2026, 10, 6, 22, 0, 0, DateTimeKind.Utc);
        var result = DailyDurationCalculator.Calculate([Period(start, start.AddHours(1))], 180, DateTime.UtcNow);
        var localDate = new DateOnly(2026, 10, 7);
        Assert.True(result.ContainsKey(localDate));
        Assert.Equal(3600, result[localDate]);
    }

    [Fact]
    public void OpenPeriodUsesNow()
    {
        var now = new DateTime(2026, 10, 6, 12, 0, 0, DateTimeKind.Utc);
        var result = DailyDurationCalculator.Calculate([Period(now.AddMinutes(-30), null)], 0, now);
        Assert.Equal(1800, result[new DateOnly(2026, 10, 6)]);
    }

    [Fact]
    public void Empty_ReturnsEmpty()
    {
        Assert.Empty(DailyDurationCalculator.Calculate([], 0, DateTime.UtcNow));
    }
}
```

- [ ] **Step 2: Запустить — падает**

Run: `dotnet test HabitTimeTracker.Tests --filter DailyDurationCalculatorTests`
Expected: FAIL (тип `DailyDurationCalculator` не найден).

- [ ] **Step 3: Реализация**

`DailyDurationCalculator.cs`:
```csharp
using HabitTimeTracker.Domain.Models;

namespace HabitTimeTracker.Domain.Heatmaps;

public static class DailyDurationCalculator
{
    // ponytail: фиксированный UTC-offset, DST не учитывается
    public static Dictionary<DateOnly, long> Calculate(
        IEnumerable<TimePeriod> periods, int offsetMinutes, DateTime nowUtc)
    {
        var result = new Dictionary<DateOnly, long>();
        var offset = TimeSpan.FromMinutes(offsetMinutes);

        foreach (var period in periods)
        {
            var start = period.StartedAt;
            var end = period.FinishAt ?? nowUtc;
            if (end <= start)
                continue;

            while (start < end)
            {
                var localDate = DateOnly.FromDateTime(start + offset);
                var nextLocalMidnightUtc = localDate.AddDays(1).ToDateTime(TimeOnly.MinValue) - offset;
                var segmentEnd = end < nextLocalMidnightUtc ? end : nextLocalMidnightUtc;
                var seconds = (long)(segmentEnd - start).TotalSeconds;

                result[localDate] = result.GetValueOrDefault(localDate) + seconds;
                start = segmentEnd;
            }
        }

        return result;
    }
}
```

- [ ] **Step 4: Запустить — зелёный**

Run: `dotnet test HabitTimeTracker.Tests --filter DailyDurationCalculatorTests`
Expected: PASS (5 тестов).

- [ ] **Step 5: Коммит**

```bash
git add -A && git commit -m "feat: add daily duration calculator for heatmap"
```

---

### Task 3: Модель и построение сетки heatmap (чистое ядро)

**Files:**
- Create: `HabitTimeTracker/Domain/Heatmaps/HeatmapPeriod.cs`
- Create: `HabitTimeTracker/Domain/Heatmaps/HeatmapModel.cs`
- Create: `HabitTimeTracker/Domain/Heatmaps/HeatmapLayoutBuilder.cs`
- Test: `HabitTimeTracker.Tests/HeatmapLayoutBuilderTests.cs`

**Interfaces:**
- Consumes: `Dictionary<DateOnly, long>` из задачи 2.
- Produces:
  - `enum HeatmapPeriod { Last7Days, Last30Days, CurrentMonth, CurrentYear }`
  - `HeatmapCell(int Row, int Column, DateOnly Date, long DurationSeconds)`
  - `HeatmapModel(int Rows, int Columns, string Title, IReadOnlyList<HeatmapCell> Cells, IReadOnlyList<string> WeekdayLabels, IReadOnlyList<(int Column, string Text)> ColumnHeaders, string? LeftDateLabel, string? RightDateLabel)`
  - `static HeatmapModel HeatmapLayoutBuilder.Build(HeatmapPeriod period, DateOnly today, IReadOnlyDictionary<DateOnly, long> durations)`

- [ ] **Step 1: Падающий тест**

`HeatmapLayoutBuilderTests.cs`:
```csharp
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
```

- [ ] **Step 2: Запустить — падает**

Run: `dotnet test HabitTimeTracker.Tests --filter HeatmapLayoutBuilderTests`
Expected: FAIL (типы не найдены).

- [ ] **Step 3: Реализация**

`HeatmapPeriod.cs`:
```csharp
namespace HabitTimeTracker.Domain.Heatmaps;

public enum HeatmapPeriod
{
    Last7Days,
    Last30Days,
    CurrentMonth,
    CurrentYear
}
```

`HeatmapModel.cs`:
```csharp
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
```

`HeatmapLayoutBuilder.cs`:
```csharp
using System.Globalization;

namespace HabitTimeTracker.Domain.Heatmaps;

public static class HeatmapLayoutBuilder
{
    private static readonly string[] Weekdays = ["Пн", "Вт", "Ср", "Чт", "Пт", "Сб", "Вс"];

    public static HeatmapModel Build(
        HeatmapPeriod period, DateOnly today, IReadOnlyDictionary<DateOnly, long> durations)
    {
        return period switch
        {
            HeatmapPeriod.Last7Days => BuildWeekStrip(today, durations),
            HeatmapPeriod.Last30Days => BuildCalendar(
                today.AddDays(-29), today, "Последние 30 дней", durations, DayNumberHeaders),
            HeatmapPeriod.CurrentMonth => BuildCalendar(
                new DateOnly(today.Year, today.Month, 1),
                new DateOnly(today.Year, today.Month, DateTime.DaysInMonth(today.Year, today.Month)),
                MonthTitle(today), durations, NoHeaders),
            HeatmapPeriod.CurrentYear => BuildCalendar(
                new DateOnly(today.Year, 1, 1),
                new DateOnly(today.Year, 12, 31),
                today.Year.ToString(), durations, MonthHeaders),
            _ => throw new ArgumentOutOfRangeException(nameof(period))
        };
    }

    private static HeatmapModel BuildWeekStrip(DateOnly today, IReadOnlyDictionary<DateOnly, long> durations)
    {
        var start = today.AddDays(-6);
        var cells = new List<HeatmapCell>();
        for (var i = 0; i < 7; i++)
        {
            var date = start.AddDays(i);
            cells.Add(new HeatmapCell(0, i, date, durations.GetValueOrDefault(date)));
        }

        return new HeatmapModel(1, 7, "Последние 7 дней", cells, [], [],
            start.ToString("dd.MM"), today.ToString("dd.MM"));
    }

    private static HeatmapModel BuildCalendar(
        DateOnly rangeStart,
        DateOnly rangeEnd,
        string title,
        IReadOnlyDictionary<DateOnly, long> durations,
        Func<IReadOnlyList<DateOnly>, IReadOnlyList<(int, string)>> headers)
    {
        var anchor = rangeStart.AddDays(-(((int)rangeStart.DayOfWeek + 6) % 7)); // понедельник <= rangeStart
        var columns = ((rangeEnd.DayNumber - anchor.DayNumber) / 7) + 1;

        var cells = new List<HeatmapCell>();
        var weekStarts = new List<DateOnly>();
        for (var c = 0; c < columns; c++)
        {
            var weekStart = anchor.AddDays(c * 7);
            weekStarts.Add(weekStart);
            for (var r = 0; r < 7; r++)
            {
                var date = weekStart.AddDays(r);
                if (date < rangeStart || date > rangeEnd)
                    continue;

                cells.Add(new HeatmapCell(r, c, date, durations.GetValueOrDefault(date)));
            }
        }

        return new HeatmapModel(7, columns, title, cells, Weekdays, headers(weekStarts), null, null);
    }

    private static IReadOnlyList<(int, string)> DayNumberHeaders(IReadOnlyList<DateOnly> weekStarts) =>
        weekStarts.Select((d, i) => (i, d.Day.ToString())).ToList();

    private static IReadOnlyList<(int, string)> MonthHeaders(IReadOnlyList<DateOnly> weekStarts)
    {
        var headers = new List<(int, string)>();
        for (var i = 0; i < weekStarts.Count; i++)
        {
            if (i == 0 || weekStarts[i].Month != weekStarts[i - 1].Month)
                headers.Add((i, MonthName(weekStarts[i].Month)));
        }

        return headers;
    }

    private static IReadOnlyList<(int, string)> NoHeaders(IReadOnlyList<DateOnly> weekStarts) => [];

    private static string MonthTitle(DateOnly date)
    {
        var ru = CultureInfo.GetCultureInfo("ru-RU");
        var name = date.ToString("MMMM", ru);
        return char.ToUpper(name[0], ru) + name[1..] + " " + date.Year;
    }

    private static string MonthName(int month)
    {
        var ru = CultureInfo.GetCultureInfo("ru-RU");
        var name = new DateTime(2000, month, 1).ToString("MMMM", ru);
        return char.ToUpper(name[0], ru) + name[1..];
    }
}
```

- [ ] **Step 4: Запустить — зелёный**

Run: `dotnet test HabitTimeTracker.Tests --filter HeatmapLayoutBuilderTests`
Expected: PASS (4 теста).

- [ ] **Step 5: Коммит**

```bash
git add -A && git commit -m "feat: add heatmap layout builder"
```

---

### Task 4: Рендер PNG (SkiaSharp) + Docker

**Files:**
- Modify: `HabitTimeTracker/HabitTimeTracker.csproj`
- Modify: `Dockerfile`
- Create: `HabitTimeTracker/Application/Heatmaps/HeatmapRenderer.cs`
- Test: `HabitTimeTracker.Tests/HeatmapRendererTests.cs`

**Interfaces:**
- Consumes: `HeatmapModel` из задачи 3.
- Produces: `static byte[] HeatmapRenderer.Render(HeatmapModel model)` — PNG.

- [ ] **Step 1: Добавить пакеты**

```bash
dotnet add HabitTimeTracker package SkiaSharp
dotnet add HabitTimeTracker package SkiaSharp.NativeAssets.Linux
```
Expected: пакеты в `HabitTimeTracker.csproj`.

- [ ] **Step 2: Падающий тест**

`HeatmapRendererTests.cs`:
```csharp
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
```

- [ ] **Step 3: Запустить — падает**

Run: `dotnet test HabitTimeTracker.Tests --filter HeatmapRendererTests`
Expected: FAIL (`HeatmapRenderer` не найден).

- [ ] **Step 4: Реализация**

`HeatmapRenderer.cs`:
```csharp
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
        using var textPaint = new SKPaint { Color = Text, IsAntialias = true, Typeface = typeface, TextSize = 12 };
        using var cellPaint = new SKPaint { IsAntialias = true };

        canvas.DrawText(model.Title, Padding, Padding + textPaint.TextSize, textPaint);

        var gridX = Padding + leftGutter;
        var gridY = Padding + topRows;

        foreach (var (column, text) in model.ColumnHeaders)
        {
            canvas.DrawText(text, gridX + column * (Cell + Gap), Padding + TitleHeight + textPaint.TextSize, textPaint);
        }

        for (var r = 0; r < model.WeekdayLabels.Count; r++)
        {
            var y = gridY + r * (Cell + Gap) + Cell / 2 + textPaint.TextSize / 3;
            canvas.DrawText(model.WeekdayLabels[r], Padding, y, textPaint);
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

        var labelY = gridY + gridHeight + textPaint.TextSize + 2;
        if (model.LeftDateLabel is not null)
            canvas.DrawText(model.LeftDateLabel, gridX, labelY, textPaint);
        if (model.RightDateLabel is not null)
        {
            var lastCellX = gridX + (model.Columns - 1) * (Cell + Gap);
            canvas.DrawText(model.RightDateLabel,
                lastCellX + Cell - textPaint.MeasureText(model.RightDateLabel), labelY, textPaint);
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
```

- [ ] **Step 5: Запустить — зелёный**

Run: `dotnet test HabitTimeTracker.Tests --filter HeatmapRendererTests`
Expected: PASS.

- [ ] **Step 6: Docker: шрифты для SkiaSharp**

В `Dockerfile`, в runtime-стадии (`FROM mcr.microsoft.com/dotnet/runtime:10.0`) перед `ENTRYPOINT`:
```dockerfile
RUN apt-get update && apt-get install -y --no-install-recommends libfontconfig1 fonts-dejavu-core \
    && rm -rf /var/lib/apt/lists/*
```

- [ ] **Step 7: Коммит**

Run: `dotnet build && dotnet test`
```bash
git add -A && git commit -m "feat: render heatmap as PNG with SkiaSharp"
```

---

### Task 5: Кнопки heatmap, выбор периода, отправка

**Files:**
- Modify: `HabitTimeTracker/Domain/Dtos/HabitTotalTime.cs`
- Modify: `HabitTimeTracker/DataAccess/HabitTimeTrackerDataContext.cs`
- Modify: `HabitTimeTracker/Application/TelegramMessages/Commands/StatisticsCommand.cs`
- Modify: `HabitTimeTracker/Application/TelegramMessages/Commands/Factory/TelegramCommandFactory.cs`
- Create: `HabitTimeTracker/Application/TelegramMessages/Commands/StartHeatmapCommand.cs`
- Create: `HabitTimeTracker/Application/TelegramMessages/Commands/HeatmapCommand.cs`

**Interfaces:**
- Consumes: `TelegramCommand.HeatmapHabit`/`Heatmap` (задача 1), `DailyDurationCalculator` (2), `HeatmapLayoutBuilder`/`HeatmapPeriod`/`HeatmapModel` (3), `HeatmapRenderer` (4), `User.TimeZoneOffsetMinutes` (1).
- Produces: `StartHeatmapCommand.BuildPeriodMenu(Guid habitId)`.

- [ ] **Step 1: DTO с Id**

`HabitTotalTime.cs`:
```csharp
namespace HabitTimeTracker.Domain.Dtos;

public record HabitTotalTime(Guid Id, string Name, long DurationInSeconds);
```
`HabitTimeTrackerDataContext.GetHabitTotalTimesAsync` — обновить проекцию:
```csharp
.Select(h => new HabitTotalTime(h.Id, h.Name, h.TimePeriods.Sum(p => p.DurationInSecondes)))
```

- [ ] **Step 2: StartHeatmapCommand**

```csharp
using HabitTimeTracker.Application.TelegramMessages.Commands.Constants;
using HabitTimeTracker.DataAccess;
using HabitTimeTracker.Domain.Models;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace HabitTimeTracker.Application.TelegramMessages.Commands;

public class StartHeatmapCommand(ITelegramBotClient botClient, IServiceProvider serviceProvider)
    : TelegramCommandBase(botClient)
{
    internal static InlineKeyboardMarkup BuildPeriodMenu(Guid habitId) => new([
        [InlineKeyboardButton.WithCallbackData("7 дней", $"{TelegramCommand.Heatmap} 7:{habitId}")],
        [InlineKeyboardButton.WithCallbackData("30 дней", $"{TelegramCommand.Heatmap} 30:{habitId}")],
        [InlineKeyboardButton.WithCallbackData("Текущий месяц", $"{TelegramCommand.Heatmap} month:{habitId}")],
        [InlineKeyboardButton.WithCallbackData("Текущий год", $"{TelegramCommand.Heatmap} year:{habitId}")],
        [InlineKeyboardButton.WithCallbackData("Назад в статистику", TelegramCommand.Statistics)]
    ]);

    public override async Task Execute(User user, string? data, CancellationToken ct = default)
    {
        var repository = serviceProvider.GetRequiredService<HabitTimeTrackerDataContext>();

        if (data == null || !Guid.TryParse(data, out var habitId))
        {
            await SendBaseAnswerAsync(user, ct);
            return;
        }

        var habit = await repository.GetHabitAsync(habitId, ct);
        if (habit == null)
        {
            await SendBaseAnswerAsync(user, ct);
            return;
        }

        await BotClient.SendMessage(
            chatId: user.TelegramId.Id,
            text: $"Выберите период для привычки «{habit.Name}»",
            replyMarkup: BuildPeriodMenu(habit.Id),
            cancellationToken: ct);
    }
}
```

- [ ] **Step 3: HeatmapCommand**

```csharp
using HabitTimeTracker.Application.TelegramMessages.Commands.Constants;
using HabitTimeTracker.DataAccess;
using HabitTimeTracker.Domain.Heatmaps;
using HabitTimeTracker.Domain.Models;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace HabitTimeTracker.Application.TelegramMessages.Commands;

public class HeatmapCommand(ITelegramBotClient botClient, IServiceProvider serviceProvider)
    : TelegramCommandBase(botClient)
{
    public override async Task Execute(User user, string? data, CancellationToken ct = default)
    {
        var repository = serviceProvider.GetRequiredService<HabitTimeTrackerDataContext>();

        var parts = data?.Split(':', 2) ?? [];
        if (parts.Length != 2 || !TryMapPeriod(parts[0], out var period) || !Guid.TryParse(parts[1], out var habitId))
        {
            await SendBaseAnswerAsync(user, ct);
            return;
        }

        var habit = await repository.GetHabitAsync(habitId, ct);
        if (habit == null)
        {
            await SendBaseAnswerAsync(user, ct);
            return;
        }

        var nowUtc = DateTime.UtcNow;
        var durations = DailyDurationCalculator.Calculate(habit.TimePeriods, user.TimeZoneOffsetMinutes, nowUtc);
        var today = DateOnly.FromDateTime(nowUtc.AddMinutes(user.TimeZoneOffsetMinutes));
        var model = HeatmapLayoutBuilder.Build(period, today, durations);
        var png = HeatmapRenderer.Render(model);

        await BotClient.SendPhoto(
            chatId: user.TelegramId.Id,
            photo: InputFile.FromStream(new MemoryStream(png), "heatmap.png"),
            caption: $"{habit.Name} — {model.Title}",
            replyMarkup: StartHeatmapCommand.BuildPeriodMenu(habit.Id),
            cancellationToken: ct);

        await BotClient.SendMessage(
            chatId: user.TelegramId.Id,
            text: BuildSummary(model),
            replyMarkup: new InlineKeyboardMarkup(
                [InlineKeyboardButton.WithCallbackData("Назад в статистику", TelegramCommand.Statistics)]),
            cancellationToken: ct);
    }

    private static bool TryMapPeriod(string value, out HeatmapPeriod period)
    {
        period = value switch
        {
            "7" => HeatmapPeriod.Last7Days,
            "30" => HeatmapPeriod.Last30Days,
            "month" => HeatmapPeriod.CurrentMonth,
            "year" => HeatmapPeriod.CurrentYear,
            _ => (HeatmapPeriod)(-1)
        };
        return Enum.IsDefined(period);
    }

    private static string BuildSummary(HeatmapModel model)
    {
        var active = model.Cells.Where(c => c.DurationSeconds > 0).ToList();
        var total = active.Sum(c => c.DurationSeconds);
        if (total == 0)
            return "За выбранный период записей нет.";

        var best = active.MaxBy(c => c.DurationSeconds)!;
        return $"Всего: {Format(total)}\n" +
               $"Активных дней: {active.Count}\n" +
               $"Лучший день: {best.Date:dd.MM.yyyy} — {Format(best.DurationSeconds)}\n" +
               $"Среднее в активный день: {Format(total / active.Count)}";
    }

    private static string Format(long seconds)
    {
        var time = TimeSpan.FromSeconds(seconds);
        return $"{time.Days} д {time.Hours} ч {time.Minutes} мин";
    }
}
```

- [ ] **Step 4: StatisticsCommand — кнопки heatmap**

Заменить формирование меню (текст со списком итогов оставить):
```csharp
var buttons = habits
    .Select(h => new[] { InlineKeyboardButton.WithCallbackData($"Heatmap: {h.Name}", $"{TelegramCommand.HeatmapHabit} {h.Id}") })
    .Append([InlineKeyboardButton.WithCallbackData("Назад в главное меню", TelegramCommand.Reset)]);

var menu = new InlineKeyboardMarkup(buttons);
```

- [ ] **Step 5: Фабрика**

Добавить ветки:
```csharp
if (state == ChatState.Default && command == TelegramCommand.HeatmapHabit)
    return new StartHeatmapCommand(botClient, _serviceProvider);

if (state == ChatState.Default && command == TelegramCommand.Heatmap)
    return new HeatmapCommand(botClient, _serviceProvider);
```

- [ ] **Step 6: Сборка/тесты + коммит**

Run: `dotnet build && dotnet test`
Expected: PASS.
```bash
git add -A && git commit -m "feat: add heatmap buttons and rendering flow to statistics"
```

---

### Task 6: Финальная проверка

- [ ] **Step 1: Полная сборка и тесты**

Run: `dotnet build && dotnet test`
Expected: PASS, 0 failures.

- [ ] **Step 2: Проверить, что миграция применяется**

Run: `dotnet ef migrations list --project HabitTimeTracker | tail -3`
Expected: `AddUserTimeZoneOffset` присутствует.

- [ ] **Step 3: Ручная проверка (если возможно)**

Запустить бота, проверить: «Статистика» → выбор привычки → все 4 периода; смена пояса; привычка без данных (все квадраты серые); период через полночь.

---

## Самопроверка плана

- **Покрытие требований:** PNG-квадраты — задачи 3-5; 7 дней — задача 3 (`BuildWeekStrip`); 30 дней — 3 (`DayNumberHeaders`); месяц — 3 (`MonthTitle`); год — 3 (`MonthHeaders`); пояс пользователя — 1; drill-down кнопки — 5.
- **Типы согласованы:** `HeatmapModel` одинаков во всех задачах; `HabitTotalTime` с `Id` используется в статистике; `TimeZoneOffsetMinutes` — задача 1, читается в 5.
- **Плейсхолдеров нет:** весь код приведён целиком.
