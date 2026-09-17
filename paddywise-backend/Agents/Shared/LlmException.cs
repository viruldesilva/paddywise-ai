using System.Net;

namespace PaddyWise.Api.Agents.Shared;

/// <summary>
/// A non-2xx response from the LLM provider. Rate limiting (429) surfaces as this too —
/// it is never retried silently, so the caller decides whether to back off.
/// </summary>
public class LlmException : Exception
{
    public LlmException(HttpStatusCode statusCode, string body)
        : base($"LLM request failed with status {(int)statusCode} ({statusCode}).")
    {
        StatusCode = statusCode;
        Body = body;
    }

    public HttpStatusCode StatusCode { get; }

    /// <summary>The raw response body, for logging and diagnostics.</summary>
    public string Body { get; }
}
