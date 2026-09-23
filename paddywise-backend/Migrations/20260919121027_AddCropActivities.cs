using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PaddyWise.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCropActivities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CropActivities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CultivationCycleId = table.Column<int>(type: "integer", nullable: false),
                    ActivityType = table.Column<int>(type: "integer", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    DetailsJson = table.Column<string>(type: "jsonb", nullable: false),
                    LoggedByUserId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CropActivities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CropActivities_CultivationCycles_CultivationCycleId",
                        column: x => x.CultivationCycleId,
                        principalTable: "CultivationCycles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CropActivities_Users_LoggedByUserId",
                        column: x => x.LoggedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CropActivities_ActivityType",
                table: "CropActivities",
                column: "ActivityType");

            migrationBuilder.CreateIndex(
                name: "IX_CropActivities_CultivationCycleId",
                table: "CropActivities",
                column: "CultivationCycleId");

            migrationBuilder.CreateIndex(
                name: "IX_CropActivities_LoggedByUserId",
                table: "CropActivities",
                column: "LoggedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CropActivities");
        }
    }
}
