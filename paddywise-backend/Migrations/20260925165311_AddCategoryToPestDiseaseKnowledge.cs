using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PaddyWise.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoryToPestDiseaseKnowledge : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Category",
                table: "PestDiseaseKnowledgeEntries",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "PestDiseaseKnowledgeEntries",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Category", "CreatedAt", "UpdatedAt" },
                values: new object[] { 0, new DateTime(2026, 9, 25, 16, 53, 11, 44, DateTimeKind.Utc).AddTicks(9836), new DateTime(2026, 9, 25, 16, 53, 11, 44, DateTimeKind.Utc).AddTicks(9838) });

            migrationBuilder.UpdateData(
                table: "PestDiseaseKnowledgeEntries",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Category", "CreatedAt", "UpdatedAt" },
                values: new object[] { 0, new DateTime(2026, 9, 25, 16, 53, 11, 44, DateTimeKind.Utc).AddTicks(9841), new DateTime(2026, 9, 25, 16, 53, 11, 44, DateTimeKind.Utc).AddTicks(9841) });

            migrationBuilder.UpdateData(
                table: "PestDiseaseKnowledgeEntries",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Category", "CreatedAt", "UpdatedAt" },
                values: new object[] { 0, new DateTime(2026, 9, 25, 16, 53, 11, 44, DateTimeKind.Utc).AddTicks(9843), new DateTime(2026, 9, 25, 16, 53, 11, 44, DateTimeKind.Utc).AddTicks(9843) });

            migrationBuilder.UpdateData(
                table: "PestDiseaseKnowledgeEntries",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "Category", "CreatedAt", "UpdatedAt" },
                values: new object[] { 0, new DateTime(2026, 9, 25, 16, 53, 11, 44, DateTimeKind.Utc).AddTicks(9845), new DateTime(2026, 9, 25, 16, 53, 11, 44, DateTimeKind.Utc).AddTicks(9845) });

            migrationBuilder.UpdateData(
                table: "PestDiseaseKnowledgeEntries",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "Category", "CreatedAt", "UpdatedAt" },
                values: new object[] { 0, new DateTime(2026, 9, 25, 16, 53, 11, 44, DateTimeKind.Utc).AddTicks(9846), new DateTime(2026, 9, 25, 16, 53, 11, 44, DateTimeKind.Utc).AddTicks(9846) });

            migrationBuilder.UpdateData(
                table: "PestDiseaseKnowledgeEntries",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "Category", "CreatedAt", "UpdatedAt" },
                values: new object[] { 0, new DateTime(2026, 9, 25, 16, 53, 11, 44, DateTimeKind.Utc).AddTicks(9847), new DateTime(2026, 9, 25, 16, 53, 11, 44, DateTimeKind.Utc).AddTicks(9847) });

            migrationBuilder.UpdateData(
                table: "PestDiseaseKnowledgeEntries",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "Category", "CreatedAt", "UpdatedAt" },
                values: new object[] { 1, new DateTime(2026, 9, 25, 16, 53, 11, 44, DateTimeKind.Utc).AddTicks(9848), new DateTime(2026, 9, 25, 16, 53, 11, 44, DateTimeKind.Utc).AddTicks(9849) });

            // Backfill rows added at runtime via the admin API (not part of the HasData seed
            // above, so the UpdateData calls above don't touch them) — matched by name,
            // case-insensitive, against the categorisation verified against the live DB before
            // this migration was written. Also re-covers the 7 HasData rows harmlessly (same
            // values), so this block alone is correct even if the HasData diff above changes.
            migrationBuilder.Sql(
                """
                UPDATE "PestDiseaseKnowledgeEntries" SET "Category" = 0
                WHERE lower("Name") IN (
                    lower('Brown Planthopper'), lower('Rice Gall Midge'), lower('Rice Leaf Folder'),
                    lower('Rice Sheath Mite'), lower('Thrips'), lower('Yellow Stem Borer'),
                    lower('White-backed Planthopper'), lower('Green Leafhopper'),
                    lower('Rice Bug (Ear-cutting Bug)'), lower('Rice Case Worm'),
                    lower('Rice Armyworm'), lower('Rice Hispa'), lower('Rice Water Weevil'),
                    lower('Rice Grasshopper'), lower('Field Rat (Rodent Damage)')
                );
                """);

            migrationBuilder.Sql(
                """
                UPDATE "PestDiseaseKnowledgeEntries" SET "Category" = 1
                WHERE lower("Name") IN (
                    lower('Sheath Rot'), lower('Rice Blast'), lower('Brown Spot'),
                    lower('Bacterial Leaf Blight'), lower('Bacterial Leaf Streak'),
                    lower('Sheath Blight'), lower('Rice Tungro Disease'), lower('False Smut'),
                    lower('Narrow Brown Leaf Spot'), lower('Bakanae (Foolish Seedling Disease)')
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Category",
                table: "PestDiseaseKnowledgeEntries");

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
