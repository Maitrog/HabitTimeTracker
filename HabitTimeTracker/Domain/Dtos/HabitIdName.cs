using DtoSrcGen;
using HabitTimeTracker.Domain.Models;

namespace HabitTimeTracker.Domain.Dtos;

[Pick(typeof(Habit), nameof(Habit.Id), nameof(Habit.Name))]
public partial class HabitIdName
{
}