# HabitTimeTracker

Telegram-бот для трекинга времени, потраченного на привычки. Каждый период занятий фиксируется командой бота, а статистика отображается в виде тепловой карты (heatmap), которая рендерится в PNG через SkiaSharp и отправляется фото.

## Возможности

- Создание, отслеживание и завершение привычек
- Фиксация периодов (в том числе пропущенных) с возможностью редактирования времени окончания последнего периода
- Статистика и тепловая карта активности по привычкам
- Настройка часового пояса пользователя
- Доступ ограничен списком разрешённых пользователей (`AllowedUsers`)

## Технологии

- .NET 10 (Worker Service)
- PostgreSQL + Entity Framework Core (Npgsql)
- Telegram.Bot
- SkiaSharp (рендеринг heatmap)
- xUnit (тесты в `HabitTimeTracker.Tests`)

## Структура проекта

- `HabitTimeTracker/Domain` — модели, DTO, value objects и логика расчёта/компоновки heatmap
- `HabitTimeTracker/Application` — worker Telegram-бота, обработка команд и сообщений, рендеринг heatmap
- `HabitTimeTracker/DataAccess` — DbContext и миграции EF Core
- `HabitTimeTracker.Tests` — модульные тесты

## Запуск

### Docker (рекомендуется)

1. Клонировать репозиторий:

```bash
git clone https://github.com/Maitrog/HabitTimeTracker.git
cd HabitTimeTracker
```

2. Создать бота через [@BotFather](https://t.me/BotFather) и получить токен.

3. Создать файл `.env` рядом с `compose.yml`:
```bash
TELEGRAM_TOKEN=<токен бота>
ALLOWED_USER_0=<ваш Telegram user ID>
```

ID пользователя можно узнать у [@userinfobot](https://t.me/userinfobot); список можно оставить пустым — тогда доступ не фильтруется.

compose поднимает приложение и PostgreSQL 16, миграции применяются автоматически при старте.

4. Запустить:

```bash
docker compose up -d --build
```

### Локально

1. Клонировать репозиторий:

```bash
git clone https://github.com/Maitrog/HabitTimeTracker.git
cd HabitTimeTracker
```

2. Создать бота через [@BotFather](https://t.me/BotFather) и получить токен.

3. Указать токен и список разрешённых пользователей в `TelegramBotSettings` (через user secrets, `appsettings.Development.json` или переменные окружения `TelegramBotSettings__Token`).

4. Поднять PostgreSQL (по умолчанию строка подключения: `Host=localhost;Port=5433;...`, см. `appsettings.json`).

5. Запустить:

```bash
dotnet run --project HabitTimeTracker
```

## Тесты

```bash
dotnet test
```
