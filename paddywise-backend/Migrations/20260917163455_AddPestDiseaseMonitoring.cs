using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PaddyWise.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPestDiseaseMonitoring : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CropObservations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CultivationCycleId = table.Column<int>(type: "integer", nullable: false),
                    ReportedByUserId = table.Column<int>(type: "integer", nullable: false),
                    ObservationType = table.Column<int>(type: "integer", nullable: false),
                    CropStage = table.Column<int>(type: "integer", nullable: false),
                    Symptoms = table.Column<string>(type: "text", nullable: false),
                    Severity = table.Column<int>(type: "integer", nullable: false),
                    ImageUrl = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CropObservations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CropObservations_CultivationCycles_CultivationCycleId",
                        column: x => x.CultivationCycleId,
                        principalTable: "CultivationCycles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CropObservations_Users_ReportedByUserId",
                        column: x => x.ReportedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PestDiseaseKnowledgeEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Symptoms = table.Column<string>(type: "text", nullable: false),
                    FavorableConditions = table.Column<string>(type: "text", nullable: true),
                    CropStages = table.Column<string>(type: "text", nullable: true),
                    ManagementGuidance = table.Column<string>(type: "text", nullable: false),
                    Source = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PestDiseaseKnowledgeEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PestDiseaseReports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CropObservationId = table.Column<int>(type: "integer", nullable: false),
                    PossibleIssue = table.Column<string>(type: "text", nullable: false),
                    Confidence = table.Column<decimal>(type: "numeric(3,2)", precision: 3, scale: 2, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    OfficerId = table.Column<int>(type: "integer", nullable: true),
                    OfficerComment = table.Column<string>(type: "text", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PestDiseaseReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PestDiseaseReports_CropObservations_CropObservationId",
                        column: x => x.CropObservationId,
                        principalTable: "CropObservations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PestDiseaseReports_Users_OfficerId",
                        column: x => x.OfficerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "PestDiseaseKnowledgeEntries",
                columns: new[] { "Id", "CreatedAt", "CropStages", "FavorableConditions", "ManagementGuidance", "Name", "Source", "Symptoms", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 9, 17, 16, 34, 55, 157, DateTimeKind.Utc).AddTicks(5200), "Nursery, Tillering", "Dry weather, drought-stressed nurseries.", "Maintain adequate field water level; apply an approved insecticide only once infestation passes the economic threshold.", "Thrips", "Sri Lanka Department of Agriculture", "Silvery streaks and curling on young leaves; stunted growth in seedlings.", new DateTime(2026, 9, 17, 16, 34, 55, 157, DateTimeKind.Utc).AddTicks(5203) },
                    { 2, new DateTime(2026, 9, 17, 16, 34, 55, 157, DateTimeKind.Utc).AddTicks(5207), "Tillering, PanicleInitiation", "Dense planting, excess nitrogen, continuous flooding, high humidity.", "Avoid excess nitrogen; alternate wetting and drying; favor resistant varieties; targeted insecticide only at economic threshold.", "Brown Planthopper", "Sri Lanka Department of Agriculture", "Yellowing and drying of leaves from the base upward (\"hopperburn\"); stunted, wilting tillers.", new DateTime(2026, 9, 17, 16, 34, 55, 157, DateTimeKind.Utc).AddTicks(5207) },
                    { 3, new DateTime(2026, 9, 17, 16, 34, 55, 157, DateTimeKind.Utc).AddTicks(5209), "Tillering, Flowering", "Continuous rice cropping without fallow, high nitrogen.", "Remove and destroy egg masses and post-harvest stubble; use light traps; targeted insecticide once dead-heart incidence passes threshold.", "Yellow Stem Borer", "Sri Lanka Department of Agriculture", "Dead heart (dried central shoot) during vegetative growth; whitehead (empty, upright panicle) at the reproductive stage.", new DateTime(2026, 9, 17, 16, 34, 55, 157, DateTimeKind.Utc).AddTicks(5209) },
                    { 4, new DateTime(2026, 9, 17, 16, 34, 55, 157, DateTimeKind.Utc).AddTicks(5210), "Tillering, PanicleInitiation", "High nitrogen, dense canopy, high humidity.", "Balanced nitrogen application; conserve natural enemies; insecticide only above the recommended damage threshold.", "Rice Leaf Folder", "Sri Lanka Department of Agriculture", "Leaves folded longitudinally and webbed together; white/transparent streaks where larvae scrape and feed inside the fold.", new DateTime(2026, 9, 17, 16, 34, 55, 157, DateTimeKind.Utc).AddTicks(5211) },
                    { 5, new DateTime(2026, 9, 17, 16, 34, 55, 157, DateTimeKind.Utc).AddTicks(5212), "PanicleInitiation, Flowering", "Warm, humid conditions and dense planting.", "Avoid excess nitrogen and overly dense planting; miticide only under severe, confirmed infestation.", "Rice Sheath Mite", "Sri Lanka Department of Agriculture", "Brown to black lesions on the leaf sheath near the waterline; can cause unfilled or discolored grains.", new DateTime(2026, 9, 17, 16, 34, 55, 157, DateTimeKind.Utc).AddTicks(5212) },
                    { 6, new DateTime(2026, 9, 17, 16, 34, 55, 157, DateTimeKind.Utc).AddTicks(5213), "Nursery, Tillering", "High humidity, shaded or low-lying fields, continuous rice cropping.", "Synchronize planting across the area; use resistant varieties; remove wild grasses acting as alternate hosts.", "Rice Gall Midge", "Sri Lanka Department of Agriculture", "Affected tiller produces a tubular \"silvershoot\"/onion-leaf gall instead of a normal leaf whorl and no panicle.", new DateTime(2026, 9, 17, 16, 34, 55, 157, DateTimeKind.Utc).AddTicks(5214) },
                    { 7, new DateTime(2026, 9, 17, 16, 34, 55, 157, DateTimeKind.Utc).AddTicks(5215), "PanicleInitiation, Flowering", "High humidity, excess nitrogen, dense planting.", "Avoid excess nitrogen; ensure adequate spacing/drainage for airflow; treat seed and apply fungicide at booting stage if severe.", "Sheath Rot", "Sri Lanka Department of Agriculture", "Reddish-brown lesions on the flag leaf sheath enclosing the panicle; panicle may fail to emerge fully or grains are discolored.", new DateTime(2026, 9, 17, 16, 34, 55, 157, DateTimeKind.Utc).AddTicks(5215) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_CropObservations_CultivationCycleId",
                table: "CropObservations",
                column: "CultivationCycleId");

            migrationBuilder.CreateIndex(
                name: "IX_CropObservations_ReportedByUserId",
                table: "CropObservations",
                column: "ReportedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PestDiseaseKnowledgeEntries_Name",
                table: "PestDiseaseKnowledgeEntries",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PestDiseaseReports_CropObservationId",
                table: "PestDiseaseReports",
                column: "CropObservationId");

            migrationBuilder.CreateIndex(
                name: "IX_PestDiseaseReports_OfficerId",
                table: "PestDiseaseReports",
                column: "OfficerId");

            migrationBuilder.CreateIndex(
                name: "IX_PestDiseaseReports_Status",
                table: "PestDiseaseReports",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PestDiseaseKnowledgeEntries");

            migrationBuilder.DropTable(
                name: "PestDiseaseReports");

            migrationBuilder.DropTable(
                name: "CropObservations");
        }
    }
}
