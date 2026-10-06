using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HabitTimeTracker.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddUserEditingHabit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "EditingHabit_HabitId",
                table: "Users",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EditingHabit_HabitId",
                table: "Users");
        }
    }
}
