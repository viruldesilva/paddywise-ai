using System.Net;
using System.Text.Json;
using Xunit;

namespace PaddyWise.Backend.Tests.FieldCultivation.Helpers;

/// <summary>
/// A plan request answers 202 with a Draft and PlanGenerationWorker generates it in the
/// background, so API tests poll GET /api/plans/{id} the way the mobile app does.
/// </summary>
public static class PlanPolling
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan Interval = TimeSpan.FromMilliseconds(50);

    /// <summary>Polls until the plan is no longer Draft and returns it as the API sent it.</summary>
    public static async Task<JsonElement> WaitUntilGeneratedAsync(HttpClient client, int planId)
    {
        var deadline = DateTime.UtcNow + Timeout;

        while (true)
        {
            var response = await client.GetAsync($"/api/plans/{planId}");
            var body = await response.Content.ReadAsStringAsync();
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"GET /api/plans/{planId} returned {response.StatusCode}: {body}");

            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.GetProperty("status").GetString() != "Draft")
                return doc.RootElement.Clone();

            Assert.True(DateTime.UtcNow < deadline, $"Plan {planId} was still Draft after {Timeout.TotalSeconds} s.");
            await Task.Delay(Interval);
        }
    }

    /// <summary>Reads the 202 a plan request answers with, checks it is a Draft, and waits for the result.</summary>
    public static async Task<JsonElement> AcceptedThenGeneratedAsync(HttpClient client, HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.Accepted, $"Expected Accepted, got {response.StatusCode}: {body}");

        using var doc = JsonDocument.Parse(body);
        Assert.Equal("Draft", doc.RootElement.GetProperty("status").GetString());
        Assert.EndsWith($"/api/plans/{doc.RootElement.GetProperty("id").GetInt32()}", response.Headers.Location?.ToString());

        return await WaitUntilGeneratedAsync(client, doc.RootElement.GetProperty("id").GetInt32());
    }
}
