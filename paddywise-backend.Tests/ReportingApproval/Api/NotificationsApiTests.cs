using System.Net;
using PaddyWise.Backend.Tests.FieldCultivation.Api;
using PaddyWise.Backend.Tests.FieldCultivation.Helpers;
using PaddyWise.Backend.Tests.ReportingApproval.Helpers;
using Xunit;

namespace PaddyWise.Backend.Tests.ReportingApproval.Api;

/// <summary>
/// NotificationsController through the real HTTP pipeline. Virul's NotificationsControllerTests
/// call the controller directly; these add routing, real authentication, the query filters,
/// ordering and the 50-item cap.
/// </summary>
[Trait("Component", "ReportingApproval")]
public class NotificationsApiTests
{
    private const string Farmer = "Farmer";

    private static async Task SeedAsync(FieldCultivationApiFactory factory, params PaddyWise.Api.Entities.Shared.Notification[] items)
    {
        await using var db = factory.CreateDbContext();
        db.Notifications.AddRange(items);
        await db.SaveChangesAsync();
    }

    [Theory]
    [InlineData("GET", "/api/notifications")]
    [InlineData("GET", "/api/notifications/unread-count")]
    [InlineData("PUT", "/api/notifications/1/read")]
    [InlineData("PUT", "/api/notifications/read-all")]
    public async Task RAAPI01a_NoLogin_Returns401(string method, string url)
    {
        using var factory = new FieldCultivationApiFactory();
        using var client = factory.CreateClient();

        var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), url));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RAAPI01b_EachUserSeesOnlyTheirOwn_NewestFirst()
    {
        using var factory = new FieldCultivationApiFactory();
        var now = DateTime.UtcNow;
        await SeedAsync(factory,
            RaSeed.Notification(1, "older", now.AddHours(-2)),
            RaSeed.Notification(1, "newer", now),
            RaSeed.Notification(2, "someone else's", now.AddHours(1)));

        using var farmer = factory.CreateClientAs(1, Farmer);
        var body = await ApiAssert.JsonAsync(await farmer.GetAsync("/api/notifications"), HttpStatusCode.OK);

        Assert.Equal(new[] { "newer", "older" }, body.EnumerateArray().Select(n => n.GetProperty("title").GetString()));
    }

    [Fact]
    public async Task RAAPI02a_UnreadOnlyAndTypeFilters()
    {
        using var factory = new FieldCultivationApiFactory();
        var now = DateTime.UtcNow;
        await SeedAsync(factory,
            RaSeed.Notification(1, "read plan", now, isRead: true),
            RaSeed.Notification(1, "unread plan", now.AddMinutes(-1)),
            RaSeed.Notification(1, "unread activity", now.AddMinutes(-2), type: "CropActivityReview"));

        using var farmer = factory.CreateClientAs(1, Farmer);
        async Task<string?[]> Titles(string query) =>
            (await ApiAssert.JsonAsync(await farmer.GetAsync("/api/notifications" + query), HttpStatusCode.OK))
                .EnumerateArray().Select(n => n.GetProperty("title").GetString()).ToArray();

        Assert.Equal(new[] { "unread plan", "unread activity" }, await Titles("?unreadOnly=true"));
        Assert.Equal(new[] { "unread activity" }, await Titles("?type=CropActivityReview"));
        Assert.Equal(new[] { "unread activity" }, await Titles("?unreadOnly=true&type=CropActivityReview"));
        Assert.Equal(3, (await Titles("?unreadOnly=false")).Length);
    }

    [Fact]
    public async Task RAAPI02b_TheListIsCappedAt50_KeepingTheNewest()
    {
        using var factory = new FieldCultivationApiFactory();
        var now = DateTime.UtcNow;
        await SeedAsync(factory, Enumerable.Range(0, 51)
            .Select(i => RaSeed.Notification(1, $"n{i}", now.AddMinutes(-i))).ToArray());

        using var farmer = factory.CreateClientAs(1, Farmer);
        var body = await ApiAssert.JsonAsync(await farmer.GetAsync("/api/notifications"), HttpStatusCode.OK);

        Assert.Equal(50, body.GetArrayLength());
        Assert.Equal("n0", body[0].GetProperty("title").GetString());
        Assert.DoesNotContain(body.EnumerateArray(), n => n.GetProperty("title").GetString() == "n50");
    }

    [Fact]
    public async Task RAAPI03_MarkReadAndReadAll_KeepTheUnreadCountInStep()
    {
        using var factory = new FieldCultivationApiFactory();
        var now = DateTime.UtcNow;
        int first, others;
        await using (var db = factory.CreateDbContext())
        {
            var a = RaSeed.Notification(1, "a", now);
            var b = RaSeed.Notification(1, "b", now);
            var c = RaSeed.Notification(1, "c", now);
            var x = RaSeed.Notification(2, "x", now);
            db.Notifications.AddRange(a, b, c, x);
            await db.SaveChangesAsync();
            first = a.Id;
            others = x.Id;
        }

        using var farmer = factory.CreateClientAs(1, Farmer);
        async Task<int> Unread() =>
            (await ApiAssert.JsonAsync(await farmer.GetAsync("/api/notifications/unread-count"), HttpStatusCode.OK))
                .GetProperty("unreadCount").GetInt32();

        Assert.Equal(3, await Unread());

        var marked = await ApiAssert.JsonAsync(await farmer.PutAsync($"/api/notifications/{first}/read", null), HttpStatusCode.OK);
        Assert.True(marked.GetProperty("isRead").GetBoolean());
        Assert.Equal(first, marked.GetProperty("id").GetInt32());
        Assert.Equal(2, await Unread());

        Assert.Equal($"Notification #{others} not found.",
            await ApiAssert.MessageAsync(await farmer.PutAsync($"/api/notifications/{others}/read", null), HttpStatusCode.NotFound));

        var all = await ApiAssert.JsonAsync(await farmer.PutAsync("/api/notifications/read-all", null), HttpStatusCode.OK);
        Assert.Equal(2, all.GetProperty("count").GetInt32());
        Assert.Equal(0, await Unread());

        using var other = factory.CreateClientAs(2, Farmer);
        Assert.Equal(1, (await ApiAssert.JsonAsync(await other.GetAsync("/api/notifications/unread-count"), HttpStatusCode.OK))
            .GetProperty("unreadCount").GetInt32());
    }
}
