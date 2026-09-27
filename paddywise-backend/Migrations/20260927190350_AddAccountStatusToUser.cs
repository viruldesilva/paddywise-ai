using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PaddyWise.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountStatusToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AccountStatus",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "PestDiseaseKnowledgeEntries",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 27, 19, 3, 48, 738, DateTimeKind.Utc).AddTicks(8846), new DateTime(2026, 9, 27, 19, 3, 48, 738, DateTimeKind.Utc).AddTicks(8849) });

            migrationBuilder.UpdateData(
                table: "PestDiseaseKnowledgeEntries",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 27, 19, 3, 48, 738, DateTimeKind.Utc).AddTicks(8851), new DateTime(2026, 9, 27, 19, 3, 48, 738, DateTimeKind.Utc).AddTicks(8851) });

            migrationBuilder.UpdateData(
                table: "PestDiseaseKnowledgeEntries",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 27, 19, 3, 48, 738, DateTimeKind.Utc).AddTicks(8852), new DateTime(2026, 9, 27, 19, 3, 48, 738, DateTimeKind.Utc).AddTicks(8852) });

            migrationBuilder.UpdateData(
                table: "PestDiseaseKnowledgeEntries",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 27, 19, 3, 48, 738, DateTimeKind.Utc).AddTicks(8854), new DateTime(2026, 9, 27, 19, 3, 48, 738, DateTimeKind.Utc).AddTicks(8854) });

            migrationBuilder.UpdateData(
                table: "PestDiseaseKnowledgeEntries",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 27, 19, 3, 48, 738, DateTimeKind.Utc).AddTicks(8855), new DateTime(2026, 9, 27, 19, 3, 48, 738, DateTimeKind.Utc).AddTicks(8855) });

            migrationBuilder.UpdateData(
                table: "PestDiseaseKnowledgeEntries",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 27, 19, 3, 48, 738, DateTimeKind.Utc).AddTicks(8857), new DateTime(2026, 9, 27, 19, 3, 48, 738, DateTimeKind.Utc).AddTicks(8857) });

            migrationBuilder.UpdateData(
                table: "PestDiseaseKnowledgeEntries",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 27, 19, 3, 48, 738, DateTimeKind.Utc).AddTicks(8858), new DateTime(2026, 9, 27, 19, 3, 48, 738, DateTimeKind.Utc).AddTicks(8858) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccountStatus",
                table: "Users");

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
    }
}
