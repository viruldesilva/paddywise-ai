using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PaddyWise.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddLastAnalyzedAtToCropObservation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastAnalyzedAt",
                table: "CropObservations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "PestDiseaseKnowledgeEntries",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 24, 9, 46, 1, 540, DateTimeKind.Utc).AddTicks(9619), new DateTime(2026, 9, 24, 9, 46, 1, 540, DateTimeKind.Utc).AddTicks(9622) });

            migrationBuilder.UpdateData(
                table: "PestDiseaseKnowledgeEntries",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 24, 9, 46, 1, 540, DateTimeKind.Utc).AddTicks(9627), new DateTime(2026, 9, 24, 9, 46, 1, 540, DateTimeKind.Utc).AddTicks(9627) });

            migrationBuilder.UpdateData(
                table: "PestDiseaseKnowledgeEntries",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 24, 9, 46, 1, 540, DateTimeKind.Utc).AddTicks(9629), new DateTime(2026, 9, 24, 9, 46, 1, 540, DateTimeKind.Utc).AddTicks(9629) });

            migrationBuilder.UpdateData(
                table: "PestDiseaseKnowledgeEntries",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 24, 9, 46, 1, 540, DateTimeKind.Utc).AddTicks(9631), new DateTime(2026, 9, 24, 9, 46, 1, 540, DateTimeKind.Utc).AddTicks(9631) });

            migrationBuilder.UpdateData(
                table: "PestDiseaseKnowledgeEntries",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 24, 9, 46, 1, 540, DateTimeKind.Utc).AddTicks(9632), new DateTime(2026, 9, 24, 9, 46, 1, 540, DateTimeKind.Utc).AddTicks(9633) });

            migrationBuilder.UpdateData(
                table: "PestDiseaseKnowledgeEntries",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 24, 9, 46, 1, 540, DateTimeKind.Utc).AddTicks(9634), new DateTime(2026, 9, 24, 9, 46, 1, 540, DateTimeKind.Utc).AddTicks(9634) });

            migrationBuilder.UpdateData(
                table: "PestDiseaseKnowledgeEntries",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 24, 9, 46, 1, 540, DateTimeKind.Utc).AddTicks(9636), new DateTime(2026, 9, 24, 9, 46, 1, 540, DateTimeKind.Utc).AddTicks(9636) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastAnalyzedAt",
                table: "CropObservations");

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
        }
    }
}
