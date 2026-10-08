using System.Net;
using System.Text.Json;
using Xunit;

namespace PaddyWise.Backend.Tests.FieldCultivation.Api;

/// <summary>
/// The two 400 shapes the web and mobile clients unwrap: the controllers' own
/// <c>{ message }</c>, and ASP.NET's automatic model validation <c>{ errors: {...} }</c>
/// (ValidationProblemDetails), which is returned before the controller runs.
/// </summary>
internal static class ApiAssert
{
    public static async Task<string> MessageAsync(HttpResponseMessage response, HttpStatusCode expected)
    {
        Assert.Equal(expected, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(doc.RootElement.TryGetProperty("message", out var message), "Expected a { message } body.");
        return message.GetString()!;
    }

    public static async Task<string> ModelErrorsAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        Assert.True(doc.RootElement.TryGetProperty("errors", out _), "Expected a ValidationProblemDetails { errors } body.");
        Assert.False(doc.RootElement.TryGetProperty("message", out _));
        return body;
    }

    public static async Task<JsonElement> JsonAsync(HttpResponseMessage response, HttpStatusCode expected)
    {
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(expected == response.StatusCode, $"Expected {expected}, got {response.StatusCode}: {body}");
        using var doc = JsonDocument.Parse(body);
        return doc.RootElement.Clone();
    }
}
