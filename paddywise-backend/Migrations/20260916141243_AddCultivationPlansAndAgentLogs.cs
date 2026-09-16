using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PaddyWise.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCultivationPlansAndAgentLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CultivationPlans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CultivationCycleId = table.Column<int>(type: "integer", nullable: false),
                    RequestedByUserId = table.Column<int>(type: "integer", nullable: false),
                    Objective = table.Column<string>(type: "text", nullable: false),
                    PlanJson = table.Column<string>(type: "jsonb", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ValidationErrorsJson = table.Column<string>(type: "jsonb", nullable: true),
                    OfficerId = table.Column<int>(type: "integer", nullable: true),
                    OfficerComment = table.Column<string>(type: "text", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CultivationPlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CultivationPlans_CultivationCycles_CultivationCycleId",
                        column: x => x.CultivationCycleId,
                        principalTable: "CultivationCycles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CultivationPlans_Users_OfficerId",
                        column: x => x.OfficerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CultivationPlans_Users_RequestedByUserId",
                        column: x => x.RequestedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AgentRunLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CultivationPlanId = table.Column<int>(type: "integer", nullable: true),
                    AgentName = table.Column<string>(type: "text", nullable: false),
                    CorrelationId = table.Column<string>(type: "text", nullable: false),
                    InputJson = table.Column<string>(type: "jsonb", nullable: false),
                    ToolCallsJson = table.Column<string>(type: "jsonb", nullable: false),
                    RawOutput = table.Column<string>(type: "text", nullable: false),
                    Success = table.Column<bool>(type: "boolean", nullable: false),
                    Error = table.Column<string>(type: "text", nullable: true),
                    DurationMs = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentRunLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AgentRunLogs_CultivationPlans_CultivationPlanId",
                        column: x => x.CultivationPlanId,
                        principalTable: "CultivationPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AgentRunLogs_CorrelationId",
                table: "AgentRunLogs",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentRunLogs_CultivationPlanId",
                table: "AgentRunLogs",
                column: "CultivationPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_CultivationPlans_CultivationCycleId",
                table: "CultivationPlans",
                column: "CultivationCycleId");

            migrationBuilder.CreateIndex(
                name: "IX_CultivationPlans_OfficerId",
                table: "CultivationPlans",
                column: "OfficerId");

            migrationBuilder.CreateIndex(
                name: "IX_CultivationPlans_RequestedByUserId",
                table: "CultivationPlans",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CultivationPlans_Status",
                table: "CultivationPlans",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AgentRunLogs");

            migrationBuilder.DropTable(
                name: "CultivationPlans");
        }
    }
}
