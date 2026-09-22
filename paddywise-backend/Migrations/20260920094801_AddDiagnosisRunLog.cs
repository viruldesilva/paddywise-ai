using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PaddyWise.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddDiagnosisRunLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DiagnosisRunLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CropObservationId = table.Column<int>(type: "integer", nullable: true),
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
                    table.PrimaryKey("PK_DiagnosisRunLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DiagnosisRunLogs_CropObservations_CropObservationId",
                        column: x => x.CropObservationId,
                        principalTable: "CropObservations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.UpdateData(
                table: "PestDiseaseKnowledgeEntries",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 20, 9, 48, 1, 188, DateTimeKind.Utc).AddTicks(233), new DateTime(2026, 9, 20, 9, 48, 1, 188, DateTimeKind.Utc).AddTicks(236) });

            migrationBuilder.UpdateData(
                table: "PestDiseaseKnowledgeEntries",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 20, 9, 48, 1, 188, DateTimeKind.Utc).AddTicks(239), new DateTime(2026, 9, 20, 9, 48, 1, 188, DateTimeKind.Utc).AddTicks(239) });

            migrationBuilder.UpdateData(
                table: "PestDiseaseKnowledgeEntries",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 20, 9, 48, 1, 188, DateTimeKind.Utc).AddTicks(241), new DateTime(2026, 9, 20, 9, 48, 1, 188, DateTimeKind.Utc).AddTicks(241) });

            migrationBuilder.UpdateData(
                table: "PestDiseaseKnowledgeEntries",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 20, 9, 48, 1, 188, DateTimeKind.Utc).AddTicks(243), new DateTime(2026, 9, 20, 9, 48, 1, 188, DateTimeKind.Utc).AddTicks(244) });

            migrationBuilder.UpdateData(
                table: "PestDiseaseKnowledgeEntries",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 20, 9, 48, 1, 188, DateTimeKind.Utc).AddTicks(246), new DateTime(2026, 9, 20, 9, 48, 1, 188, DateTimeKind.Utc).AddTicks(246) });

            migrationBuilder.UpdateData(
                table: "PestDiseaseKnowledgeEntries",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 20, 9, 48, 1, 188, DateTimeKind.Utc).AddTicks(247), new DateTime(2026, 9, 20, 9, 48, 1, 188, DateTimeKind.Utc).AddTicks(247) });

            migrationBuilder.UpdateData(
                table: "PestDiseaseKnowledgeEntries",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 20, 9, 48, 1, 188, DateTimeKind.Utc).AddTicks(248), new DateTime(2026, 9, 20, 9, 48, 1, 188, DateTimeKind.Utc).AddTicks(248) });

            migrationBuilder.CreateIndex(
                name: "IX_DiagnosisRunLogs_CorrelationId",
                table: "DiagnosisRunLogs",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_DiagnosisRunLogs_CropObservationId",
                table: "DiagnosisRunLogs",
                column: "CropObservationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DiagnosisRunLogs");

            migrationBuilder.UpdateData(
                table: "PestDiseaseKnowledgeEntries",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 17, 16, 34, 55, 157, DateTimeKind.Utc).AddTicks(5200), new DateTime(2026, 9, 17, 16, 34, 55, 157, DateTimeKind.Utc).AddTicks(5203) });

            migrationBuilder.UpdateData(
                table: "PestDiseaseKnowledgeEntries",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 17, 16, 34, 55, 157, DateTimeKind.Utc).AddTicks(5207), new DateTime(2026, 9, 17, 16, 34, 55, 157, DateTimeKind.Utc).AddTicks(5207) });

            migrationBuilder.UpdateData(
                table: "PestDiseaseKnowledgeEntries",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 17, 16, 34, 55, 157, DateTimeKind.Utc).AddTicks(5209), new DateTime(2026, 9, 17, 16, 34, 55, 157, DateTimeKind.Utc).AddTicks(5209) });

            migrationBuilder.UpdateData(
                table: "PestDiseaseKnowledgeEntries",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 17, 16, 34, 55, 157, DateTimeKind.Utc).AddTicks(5210), new DateTime(2026, 9, 17, 16, 34, 55, 157, DateTimeKind.Utc).AddTicks(5211) });

            migrationBuilder.UpdateData(
                table: "PestDiseaseKnowledgeEntries",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 17, 16, 34, 55, 157, DateTimeKind.Utc).AddTicks(5212), new DateTime(2026, 9, 17, 16, 34, 55, 157, DateTimeKind.Utc).AddTicks(5212) });

            migrationBuilder.UpdateData(
                table: "PestDiseaseKnowledgeEntries",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 17, 16, 34, 55, 157, DateTimeKind.Utc).AddTicks(5213), new DateTime(2026, 9, 17, 16, 34, 55, 157, DateTimeKind.Utc).AddTicks(5214) });

            migrationBuilder.UpdateData(
                table: "PestDiseaseKnowledgeEntries",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 17, 16, 34, 55, 157, DateTimeKind.Utc).AddTicks(5215), new DateTime(2026, 9, 17, 16, 34, 55, 157, DateTimeKind.Utc).AddTicks(5215) });
        }
    }
}
