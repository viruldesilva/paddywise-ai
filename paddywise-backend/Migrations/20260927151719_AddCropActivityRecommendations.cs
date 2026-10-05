using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PaddyWise.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCropActivityRecommendations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CropActivityRecommendations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RecommendationUid = table.Column<string>(type: "text", nullable: false),
                    CultivationCycleId = table.Column<int>(type: "integer", nullable: false),
                    RequestedByUserId = table.Column<int>(type: "integer", nullable: false),
                    Category = table.Column<string>(type: "text", nullable: false),
                    Priority = table.Column<string>(type: "text", nullable: false),
                    Action = table.Column<string>(type: "text", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: false),
                    Evidence = table.Column<string>(type: "text", nullable: false),
                    ConfidenceScore = table.Column<double>(type: "double precision", nullable: false),
                    CitationsJson = table.Column<string>(type: "jsonb", nullable: false),
                    RequiresOfficerReview = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    ExecutionPayloadJson = table.Column<string>(type: "jsonb", nullable: true),
                    OfficerId = table.Column<int>(type: "integer", nullable: true),
                    OfficerName = table.Column<string>(type: "text", nullable: true),
                    OfficerComment = table.Column<string>(type: "text", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExecutedByUserId = table.Column<int>(type: "integer", nullable: true),
                    ExecutedActivityId = table.Column<int>(type: "integer", nullable: true),
                    ExecutedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CropActivityRecommendations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CropActivityRecommendations_CropActivities_ExecutedActivity~",
                        column: x => x.ExecutedActivityId,
                        principalTable: "CropActivities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CropActivityRecommendations_CultivationCycles_CultivationCy~",
                        column: x => x.CultivationCycleId,
                        principalTable: "CultivationCycles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CropActivityRecommendations_Users_ExecutedByUserId",
                        column: x => x.ExecutedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CropActivityRecommendations_Users_OfficerId",
                        column: x => x.OfficerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CropActivityRecommendations_Users_RequestedByUserId",
                        column: x => x.RequestedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                table: "PestDiseaseKnowledgeEntries",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 27, 15, 17, 17, 161, DateTimeKind.Utc).AddTicks(8043), new DateTime(2026, 9, 27, 15, 17, 17, 161, DateTimeKind.Utc).AddTicks(8047) });

            migrationBuilder.UpdateData(
                table: "PestDiseaseKnowledgeEntries",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 27, 15, 17, 17, 161, DateTimeKind.Utc).AddTicks(8067), new DateTime(2026, 9, 27, 15, 17, 17, 161, DateTimeKind.Utc).AddTicks(8067) });

            migrationBuilder.UpdateData(
                table: "PestDiseaseKnowledgeEntries",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 27, 15, 17, 17, 161, DateTimeKind.Utc).AddTicks(8069), new DateTime(2026, 9, 27, 15, 17, 17, 161, DateTimeKind.Utc).AddTicks(8070) });

            migrationBuilder.UpdateData(
                table: "PestDiseaseKnowledgeEntries",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 27, 15, 17, 17, 161, DateTimeKind.Utc).AddTicks(8071), new DateTime(2026, 9, 27, 15, 17, 17, 161, DateTimeKind.Utc).AddTicks(8072) });

            migrationBuilder.UpdateData(
                table: "PestDiseaseKnowledgeEntries",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 27, 15, 17, 17, 161, DateTimeKind.Utc).AddTicks(8074), new DateTime(2026, 9, 27, 15, 17, 17, 161, DateTimeKind.Utc).AddTicks(8074) });

            migrationBuilder.UpdateData(
                table: "PestDiseaseKnowledgeEntries",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 27, 15, 17, 17, 161, DateTimeKind.Utc).AddTicks(8076), new DateTime(2026, 9, 27, 15, 17, 17, 161, DateTimeKind.Utc).AddTicks(8076) });

            migrationBuilder.UpdateData(
                table: "PestDiseaseKnowledgeEntries",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 27, 15, 17, 17, 161, DateTimeKind.Utc).AddTicks(8078), new DateTime(2026, 9, 27, 15, 17, 17, 161, DateTimeKind.Utc).AddTicks(8078) });

            migrationBuilder.CreateIndex(
                name: "IX_CropActivityRecommendations_CultivationCycleId",
                table: "CropActivityRecommendations",
                column: "CultivationCycleId");

            migrationBuilder.CreateIndex(
                name: "IX_CropActivityRecommendations_ExecutedActivityId",
                table: "CropActivityRecommendations",
                column: "ExecutedActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_CropActivityRecommendations_ExecutedByUserId",
                table: "CropActivityRecommendations",
                column: "ExecutedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CropActivityRecommendations_OfficerId",
                table: "CropActivityRecommendations",
                column: "OfficerId");

            migrationBuilder.CreateIndex(
                name: "IX_CropActivityRecommendations_RequestedByUserId",
                table: "CropActivityRecommendations",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CropActivityRecommendations_Status",
                table: "CropActivityRecommendations",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CropActivityRecommendations");

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
