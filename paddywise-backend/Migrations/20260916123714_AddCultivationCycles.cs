using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PaddyWise.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCultivationCycles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Varieties",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    DurationDays = table.Column<int>(type: "integer", nullable: false),
                    AgeGroup = table.Column<string>(type: "text", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Varieties", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CultivationCycles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FieldId = table.Column<int>(type: "integer", nullable: false),
                    VarietyId = table.Column<int>(type: "integer", nullable: false),
                    Season = table.Column<int>(type: "integer", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    Method = table.Column<int>(type: "integer", nullable: false),
                    SowingDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ExpectedHarvestDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ActualHarvestDate = table.Column<DateOnly>(type: "date", nullable: true),
                    CurrentStage = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CultivationCycles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CultivationCycles_Fields_FieldId",
                        column: x => x.FieldId,
                        principalTable: "Fields",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CultivationCycles_Varieties_VarietyId",
                        column: x => x.VarietyId,
                        principalTable: "Varieties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GrowthStageLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CultivationCycleId = table.Column<int>(type: "integer", nullable: false),
                    Stage = table.Column<int>(type: "integer", nullable: false),
                    ObservedOn = table.Column<DateOnly>(type: "date", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    LoggedByUserId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GrowthStageLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GrowthStageLogs_CultivationCycles_CultivationCycleId",
                        column: x => x.CultivationCycleId,
                        principalTable: "CultivationCycles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GrowthStageLogs_Users_LoggedByUserId",
                        column: x => x.LoggedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Varieties",
                columns: new[] { "Id", "AgeGroup", "DurationDays", "Name", "Notes" },
                values: new object[,]
                {
                    { 1, "3 month", 90, "Bg 300", null },
                    { 2, "3.5 month", 105, "Bg 352", null },
                    { 3, "3.5 month", 105, "Bg 360", null },
                    { 4, "3.5 month", 105, "At 362", null },
                    { 5, "3.5 month", 105, "Bg 94-1", null },
                    { 6, "3.5 month", 105, "Bg 359", null },
                    { 7, "3 month", 90, "At 307", null },
                    { 8, "3.5 month", 105, "Bw 367", null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_CultivationCycles_FieldId_Season_Year",
                table: "CultivationCycles",
                columns: new[] { "FieldId", "Season", "Year" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CultivationCycles_Status",
                table: "CultivationCycles",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_CultivationCycles_VarietyId",
                table: "CultivationCycles",
                column: "VarietyId");

            migrationBuilder.CreateIndex(
                name: "IX_GrowthStageLogs_CultivationCycleId",
                table: "GrowthStageLogs",
                column: "CultivationCycleId");

            migrationBuilder.CreateIndex(
                name: "IX_GrowthStageLogs_LoggedByUserId",
                table: "GrowthStageLogs",
                column: "LoggedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Varieties_Name",
                table: "Varieties",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GrowthStageLogs");

            migrationBuilder.DropTable(
                name: "CultivationCycles");

            migrationBuilder.DropTable(
                name: "Varieties");
        }
    }
}
