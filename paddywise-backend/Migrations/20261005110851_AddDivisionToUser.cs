using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PaddyWise.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddDivisionToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DivisionId",
                table: "Users",
                type: "integer",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "PestDiseaseKnowledgeEntries",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 10, 5, 11, 8, 50, 163, DateTimeKind.Utc).AddTicks(7656), new DateTime(2026, 10, 5, 11, 8, 50, 163, DateTimeKind.Utc).AddTicks(7659) });

            migrationBuilder.UpdateData(
                table: "PestDiseaseKnowledgeEntries",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 10, 5, 11, 8, 50, 163, DateTimeKind.Utc).AddTicks(7662), new DateTime(2026, 10, 5, 11, 8, 50, 163, DateTimeKind.Utc).AddTicks(7662) });

            migrationBuilder.UpdateData(
                table: "PestDiseaseKnowledgeEntries",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 10, 5, 11, 8, 50, 163, DateTimeKind.Utc).AddTicks(7663), new DateTime(2026, 10, 5, 11, 8, 50, 163, DateTimeKind.Utc).AddTicks(7663) });

            migrationBuilder.UpdateData(
                table: "PestDiseaseKnowledgeEntries",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 10, 5, 11, 8, 50, 163, DateTimeKind.Utc).AddTicks(7665), new DateTime(2026, 10, 5, 11, 8, 50, 163, DateTimeKind.Utc).AddTicks(7665) });

            migrationBuilder.UpdateData(
                table: "PestDiseaseKnowledgeEntries",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 10, 5, 11, 8, 50, 163, DateTimeKind.Utc).AddTicks(7666), new DateTime(2026, 10, 5, 11, 8, 50, 163, DateTimeKind.Utc).AddTicks(7667) });

            migrationBuilder.UpdateData(
                table: "PestDiseaseKnowledgeEntries",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 10, 5, 11, 8, 50, 163, DateTimeKind.Utc).AddTicks(7668), new DateTime(2026, 10, 5, 11, 8, 50, 163, DateTimeKind.Utc).AddTicks(7668) });

            migrationBuilder.UpdateData(
                table: "PestDiseaseKnowledgeEntries",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 10, 5, 11, 8, 50, 163, DateTimeKind.Utc).AddTicks(7669), new DateTime(2026, 10, 5, 11, 8, 50, 163, DateTimeKind.Utc).AddTicks(7670) });

            migrationBuilder.CreateIndex(
                name: "IX_Users_DivisionId",
                table: "Users",
                column: "DivisionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Divisions_DivisionId",
                table: "Users",
                column: "DivisionId",
                principalTable: "Divisions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Divisions_DivisionId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_DivisionId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DivisionId",
                table: "Users");

            migrationBuilder.UpdateData(
                table: "PestDiseaseKnowledgeEntries",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 27, 20, 23, 25, 604, DateTimeKind.Utc).AddTicks(1954), new DateTime(2026, 9, 27, 20, 23, 25, 604, DateTimeKind.Utc).AddTicks(1956) });

            migrationBuilder.UpdateData(
                table: "PestDiseaseKnowledgeEntries",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 27, 20, 23, 25, 604, DateTimeKind.Utc).AddTicks(1960), new DateTime(2026, 9, 27, 20, 23, 25, 604, DateTimeKind.Utc).AddTicks(1960) });

            migrationBuilder.UpdateData(
                table: "PestDiseaseKnowledgeEntries",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 27, 20, 23, 25, 604, DateTimeKind.Utc).AddTicks(1962), new DateTime(2026, 9, 27, 20, 23, 25, 604, DateTimeKind.Utc).AddTicks(1962) });

            migrationBuilder.UpdateData(
                table: "PestDiseaseKnowledgeEntries",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 27, 20, 23, 25, 604, DateTimeKind.Utc).AddTicks(1963), new DateTime(2026, 9, 27, 20, 23, 25, 604, DateTimeKind.Utc).AddTicks(1964) });

            migrationBuilder.UpdateData(
                table: "PestDiseaseKnowledgeEntries",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 27, 20, 23, 25, 604, DateTimeKind.Utc).AddTicks(1965), new DateTime(2026, 9, 27, 20, 23, 25, 604, DateTimeKind.Utc).AddTicks(1965) });

            migrationBuilder.UpdateData(
                table: "PestDiseaseKnowledgeEntries",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 27, 20, 23, 25, 604, DateTimeKind.Utc).AddTicks(1966), new DateTime(2026, 9, 27, 20, 23, 25, 604, DateTimeKind.Utc).AddTicks(1966) });

            migrationBuilder.UpdateData(
                table: "PestDiseaseKnowledgeEntries",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 27, 20, 23, 25, 604, DateTimeKind.Utc).AddTicks(1968), new DateTime(2026, 9, 27, 20, 23, 25, 604, DateTimeKind.Utc).AddTicks(1968) });
        }
    }
}
