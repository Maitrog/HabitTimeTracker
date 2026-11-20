using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HabitTimeTracker.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddHabitEpochs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "EditingHabit_EpochId",
                table: "Users",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EditingHabit_PromptMessageId",
                table: "Users",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "EpochId",
                table: "TimePeriod",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CurrentEpochId",
                table: "Habits",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "Habits" SET "CurrentEpochId" = gen_random_uuid();
                UPDATE "TimePeriod" AS p SET "EpochId" = h."CurrentEpochId"
                FROM "Habits" AS h WHERE p."HabitId" = h."Id";
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "CurrentEpochId",
                table: "Habits",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "EpochId",
                table: "TimePeriod",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "Epochs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    HabitId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Epochs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Epochs_Habits_HabitId",
                        column: x => x.HabitId,
                        principalTable: "Habits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Epochs_HabitId",
                table: "Epochs",
                column: "HabitId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE "Users" SET "ChatState" = 0, "EditingHabit_HabitId" = NULL
                WHERE "ChatState" IN (9, 10);
                """);

            migrationBuilder.DropTable(
                name: "Epochs");

            migrationBuilder.DropColumn(
                name: "EditingHabit_EpochId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "EditingHabit_PromptMessageId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "EpochId",
                table: "TimePeriod");

            migrationBuilder.DropColumn(
                name: "CurrentEpochId",
                table: "Habits");
        }
    }
}
