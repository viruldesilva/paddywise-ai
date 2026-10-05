using System.Net;
using System.Net.Http.Json;
using PaddyWise.Api.Entities.Shared;
using PaddyWise.Backend.Tests.FieldCultivation.Api;
using PaddyWise.Backend.Tests.FieldCultivation.Helpers;
using PaddyWise.Backend.Tests.ReportingApproval.Helpers;
using Xunit;

namespace PaddyWise.Backend.Tests.ReportingApproval.Api;

/// <summary>
/// ReviewsController and AdminController over HTTP — the cases Virul's controller tests
/// (which call the controllers directly) leave open.
/// </summary>
[Trait("Component", "ReportingApproval")]
public class ReviewsAndAdminApiTests
{
    [Fact]
    public async Task RAAPI04a_DraftComments_AreNotForFieldOfficers()
    {
        using var factory = new FieldCultivationApiFactory();
        using var fieldOfficer = factory.CreateClientAs(91, "FieldOfficer");

        Assert.Equal(HttpStatusCode.Forbidden, (await fieldOfficer.GetAsync("/api/reviews/plans/1/draft-revision-comment")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await fieldOfficer.GetAsync("/api/reviews/reports/1/draft-revision-comment")).StatusCode);
    }

    [Fact]
    public async Task RAAPI04b_ThePlanDraft_ComesFromTheRealServiceAndTheLlm()
    {
        using var factory = new FieldCultivationApiFactory();
        int planId;
        await using (var db = factory.CreateDbContext())
        {
            var cycle = await FcTestDb.SeedFarmerCycleAsync(db, 1, "QA Farmer");
            planId = (await RaSeed.PlanAsync(db, cycle, 1, "{\"summary\":\"Plan.\",\"steps\":[]}",
                validationErrorsJson: "[\"Step 1 is in the past.\"]")).Id;
        }
        factory.UseLlm((_, _, _, _) => Task.FromResult("• Pros: timely. • Cons: late water. • Recommendation: irrigate earlier."));

        using var officer = factory.CreateClientAs(90, "AgriculturalOfficer");
        var body = await ApiAssert.JsonAsync(
            await officer.GetAsync($"/api/reviews/plans/{planId}/draft-revision-comment"), HttpStatusCode.OK);

        Assert.Equal("• Pros: timely. • Cons: late water. • Recommendation: irrigate earlier.",
            body.GetProperty("draftComment").GetString());
        Assert.Contains("Step 1 is in the past.", Assert.Single(factory.CapturedCalls).UserPrompt);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RAAPI05a_RejectingAnOfficer_WorksWithOrWithoutAReason(bool withReason)
    {
        using var factory = new FieldCultivationApiFactory();
        await using (var db = factory.CreateDbContext())
        {
            var officer = await FcTestDb.SeedUserAsync(db, 40, "Pending Officer", UserRole.AgriculturalOfficer);
            officer.AccountStatus = AccountStatus.PendingApproval;
            await db.SaveChangesAsync();
        }

        using var admin = factory.CreateClientAs(1, "Admin");
        var response = withReason
            ? await admin.PostAsJsonAsync("/api/admin/officer-requests/40/reject", new { reason = "Could not verify employment." })
            : await admin.PostAsync("/api/admin/officer-requests/40/reject", null);

        Assert.Equal("Officer account application rejected.", await ApiAssert.MessageAsync(response, HttpStatusCode.OK));
        await using var check = factory.CreateDbContext();
        Assert.Equal(AccountStatus.Rejected, (await check.Users.FindAsync(40))!.AccountStatus);
    }

    [Fact]
    public async Task RAAPI05b_ThePendingList_HoldsOnlyPendingOfficers_AndNoPasswordHash()
    {
        using var factory = new FieldCultivationApiFactory();
        await using (var db = factory.CreateDbContext())
        {
            foreach (var (id, name, role, status) in new[]
                     {
                         (40, "Pending Officer", UserRole.AgriculturalOfficer, AccountStatus.PendingApproval),
                         (41, "Approved Officer", UserRole.AgriculturalOfficer, AccountStatus.Approved),
                         (42, "Rejected Officer", UserRole.AgriculturalOfficer, AccountStatus.Rejected),
                         (43, "Pending Field Officer", UserRole.FieldOfficer, AccountStatus.PendingApproval),
                     })
            {
                var user = await FcTestDb.SeedUserAsync(db, id, name, role);
                user.AccountStatus = status;
            }
            await db.SaveChangesAsync();
        }

        using var admin = factory.CreateClientAs(1, "Admin");
        var response = await admin.GetAsync("/api/admin/officer-requests");
        var raw = await response.Content.ReadAsStringAsync();
        var body = await ApiAssert.JsonAsync(response, HttpStatusCode.OK);

        Assert.Equal("Pending Officer", Assert.Single(body.EnumerateArray()).GetProperty("name").GetString());
        Assert.DoesNotContain("passwordHash", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("unused-in-tests", raw);
    }
}
