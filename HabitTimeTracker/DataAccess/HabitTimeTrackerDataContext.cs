using HabitTimeTracker.Domain.Dtos;
using HabitTimeTracker.Domain.Models;
using HabitTimeTracker.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace HabitTimeTracker.DataAccess;

public class HabitTimeTrackerDataContext(DbContextOptions<HabitTimeTrackerDataContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }

    public DbSet<Habit> Habits { get; set; }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<TelegramId>().HaveConversion<TelegramIdConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(b =>
        {
            b.ComplexProperty(u => u.Name);
            b.HasIndex(u => u.TelegramId).IsUnique();
            b.Property(u => u.Version).IsRowVersion();
        });
        
        modelBuilder.Entity<Habit>(b =>
        {
            b.Property(u => u.Version).IsRowVersion();
        });
    }
    public async Task<User?> GetUserByTelegramIdAsync(TelegramId telegramId)
    {
        return await Users.SingleOrDefaultAsync(u => u.TelegramId == telegramId);
    }

    public async Task CreateUserAsync(User user, CancellationToken ct)
    {
        await Users.AddAsync(user, ct);
        await SaveChangesAsync(ct);
    }

    public async Task UpdateUserAsync(User user, CancellationToken ct)
    {
        Users.Update(user);
        await SaveChangesAsync(ct);
    }

    public async Task CreateHabitAsync(User user, Habit habit, CancellationToken ct)
    {
        Users.Update(user);
        await Habits.AddAsync(habit, ct);
        await SaveChangesAsync(ct);
    }
    public async Task<List<HabitIdName>> GetActiveHabitsAsync(Guid userId)
    {
        return await Habits.Where(h => h.UserId == userId && h.Deleted == false)
                           .Select(x => new HabitIdName(x))
                           .ToListAsync();
    }

    public async Task<List<HabitTotalTime>> GetHabitTotalTimesAsync(Guid userId, CancellationToken ct)
    {
        return await Habits.Where(h => h.UserId == userId && h.Deleted == false)
                           .AsNoTracking()
                           .Select(h => new HabitTotalTime(h.Name, h.TimePeriods.Sum(p => p.DurationInSecondes)))
                           .ToListAsync(ct);
    }

    public async Task<Habit?> GetHabitAsync(Guid habitId,  CancellationToken ct)
    {
        return await Habits.Where(h => h.Id == habitId && h.Deleted == false)
                           .Include(x => x.TimePeriods)
                           .FirstOrDefaultAsync(ct);
    }

    public async Task UpdateHabitAsync(User user, Habit habit, CancellationToken ct)
    {
        Users.Update(user);
        Habits.Update(habit);
        await SaveChangesAsync(ct);
    }

    private sealed class TelegramIdConverter() : ValueConverter<TelegramId, long>(id => id.Id, value => new TelegramId(value));
}

