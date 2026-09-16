using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PaddyWise.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddDivisionsAndFieldOwnership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "Area",
                table: "Fields",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Fields",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "DivisionId",
                table: "Fields",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "FarmerId",
                table: "Fields",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Fields",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Fields",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateTable(
                name: "Divisions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    District = table.Column<string>(type: "text", nullable: false),
                    Province = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Divisions", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Divisions",
                columns: new[] { "Id", "District", "Name", "Province" },
                values: new object[,]
                {
                    { 1, "Polonnaruwa", "Medirigiriya", "North Central" },
                    { 2, "Anuradhapura", "Nuwaragam Palatha", "North Central" },
                    { 3, "Anuradhapura", "Tambuttegama", "North Central" },
                    { 4, "Ampara", "Ampara Central", "Eastern" },
                    { 5, "Kurunegala", "Kurunegala West", "North Western" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Fields_DivisionId",
                table: "Fields",
                column: "DivisionId");

            migrationBuilder.CreateIndex(
                name: "IX_Fields_FarmerId",
                table: "Fields",
                column: "FarmerId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Fields_Area_Positive",
                table: "Fields",
                sql: "\"Area\" > 0");

            migrationBuilder.AddForeignKey(
                name: "FK_Fields_Divisions_DivisionId",
                table: "Fields",
                column: "DivisionId",
                principalTable: "Divisions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Fields_Users_FarmerId",
                table: "Fields",
                column: "FarmerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Fields_Divisions_DivisionId",
                table: "Fields");

            migrationBuilder.DropForeignKey(
                name: "FK_Fields_Users_FarmerId",
                table: "Fields");

            migrationBuilder.DropTable(
                name: "Divisions");

            migrationBuilder.DropIndex(
                name: "IX_Fields_DivisionId",
                table: "Fields");

            migrationBuilder.DropIndex(
                name: "IX_Fields_FarmerId",
                table: "Fields");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Fields_Area_Positive",
                table: "Fields");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Fields");

            migrationBuilder.DropColumn(
                name: "DivisionId",
                table: "Fields");

            migrationBuilder.DropColumn(
                name: "FarmerId",
                table: "Fields");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Fields");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Fields");

            migrationBuilder.AlterColumn<decimal>(
                name: "Area",
                table: "Fields",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(10,2)",
                oldPrecision: 10,
                oldScale: 2);
        }
    }
}
