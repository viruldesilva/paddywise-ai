using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PaddyWise.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddReportingApprovalNotificationEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                table: "PestDiseaseKnowledgeEntries",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 27, 7, 42, 16, 280, DateTimeKind.Utc).AddTicks(6292), new DateTime(2026, 9, 27, 7, 42, 16, 280, DateTimeKind.Utc).AddTicks(6294) });

            migrationBuilder.UpdateData(
                table: "PestDiseaseKnowledgeEntries",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 27, 7, 42, 16, 280, DateTimeKind.Utc).AddTicks(6296), new DateTime(2026, 9, 27, 7, 42, 16, 280, DateTimeKind.Utc).AddTicks(6296) });

            migrationBuilder.UpdateData(
                table: "PestDiseaseKnowledgeEntries",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 27, 7, 42, 16, 280, DateTimeKind.Utc).AddTicks(6297), new DateTime(2026, 9, 27, 7, 42, 16, 280, DateTimeKind.Utc).AddTicks(6298) });

            migrationBuilder.UpdateData(
                table: "PestDiseaseKnowledgeEntries",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 27, 7, 42, 16, 280, DateTimeKind.Utc).AddTicks(6299), new DateTime(2026, 9, 27, 7, 42, 16, 280, DateTimeKind.Utc).AddTicks(6299) });

            migrationBuilder.UpdateData(
                table: "PestDiseaseKnowledgeEntries",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 27, 7, 42, 16, 280, DateTimeKind.Utc).AddTicks(6300), new DateTime(2026, 9, 27, 7, 42, 16, 280, DateTimeKind.Utc).AddTicks(6300) });

            migrationBuilder.UpdateData(
                table: "PestDiseaseKnowledgeEntries",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 27, 7, 42, 16, 280, DateTimeKind.Utc).AddTicks(6301), new DateTime(2026, 9, 27, 7, 42, 16, 280, DateTimeKind.Utc).AddTicks(6302) });

            migrationBuilder.UpdateData(
                table: "PestDiseaseKnowledgeEntries",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 27, 7, 42, 16, 280, DateTimeKind.Utc).AddTicks(6303), new DateTime(2026, 9, 27, 7, 42, 16, 280, DateTimeKind.Utc).AddTicks(6303) });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_IsRead",
                table: "Notifications",
                column: "IsRead");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId",
                table: "Notifications",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Notifications");

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
