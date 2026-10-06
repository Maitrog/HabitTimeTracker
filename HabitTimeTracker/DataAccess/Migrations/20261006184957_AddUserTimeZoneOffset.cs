using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HabitTimeTracker.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddUserTimeZoneOffset : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TimeZoneOffsetMinutes",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 180);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TimeZoneOffsetMinutes",
                table: "Users");
        }
    }
}
