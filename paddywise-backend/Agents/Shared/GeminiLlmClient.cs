using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace PaddyWise.Api.Agents.Shared;

/// <summary>
/// Calls the Gemini REST generateContent endpoint and drives the function-calling loop.
/// Wire shapes below follow the Gemini REST reference: the model answers with
/// candidates[0].content.parts[].functionCall {id,name,args}; the client replies with a
/// role "user" content whose parts carry functionResponse {id,name,response}.
/// </summary>
public sealed class GeminiLlmClient : ILlmClient
{
    /// <summary>Name of the IHttpClientFactory client configured in Program.cs.</summary>
    public const string HttpClientName = "Gemini";

    /// <summary>
    /// gemini-2.5-flash is no longer served to callers new to it ("no longer available to
    /// new users"), which surfaces as a 404 from generateContent, so the default is the
    /// model Google names as its replacement. Override with Gemini:Model.
    /// </summary>
    public const string DefaultModel = "gemini-3.6-flash";

    /// <summary>The config key every caller falls back to when its own key is unset.</summary>
    public const string DefaultApiKeyConfigKey = "Gemini:ApiKey";

    private const string BaseUrl = "https://generativelanguage.googleapis.com/v1beta";

    /// <summary>Tool rounds allowed before we stop feeding results back and take the text.</summary>
    private const int MaxToolIterations = 8;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GeminiLlmClient> _logger;
    private readonly string _apiKeyConfigKey;

    /// <param name="apiKeyConfigKey">
    /// Which configuration key holds this instance's API key, e.g. "Gemini:ApiKey" (the
    /// default) or a per-agent key such as "Gemini:PestDiseaseApiKey". If that key is unset,
    /// this falls back to <see cref="DefaultApiKeyConfigKey"/> — so an agent that hasn't been
    /// given its own key still works off the shared one.
    /// </param>
    public GeminiLlmClient(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<GeminiLlmClient> logger,
        string apiKeyConfigKey = DefaultApiKeyConfigKey)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
        _apiKeyConfigKey = apiKeyConfigKey;
    }

    public Task<string> CompleteJsonAsync(
        string systemPrompt,
        string userPrompt,
        IReadOnlyList<LlmToolDefinition> tools,
        Func<string, string, Task<string>> toolExecutor,
        CancellationToken ct) =>
        CompleteAsync(systemPrompt, userPrompt, Array.Empty<LlmImagePart>(), tools, toolExecutor, ct);

    public Task<string> CompleteJsonWithImagesAsync(
        string systemPrompt,
        string userPrompt,
        IReadOnlyList<LlmImagePart> images,
        IReadOnlyList<LlmToolDefinition> tools,
        Func<string, string, Task<string>> toolExecutor,
        CancellationToken ct) =>
        CompleteAsync(systemPrompt, userPrompt, images, tools, toolExecutor, ct);

    private async Task<string> CompleteAsync(
        string systemPrompt,
        string userPrompt,
        IReadOnlyList<LlmImagePart> images,
        IReadOnlyList<LlmToolDefinition> tools,
        Func<string, string, Task<string>> toolExecutor,
        CancellationToken ct)
    {
        var apiKey = _configuration[_apiKeyConfigKey];
        if (string.IsNullOrWhiteSpace(apiKey) && _apiKeyConfigKey != DefaultApiKeyConfigKey)
        {
            // No dedicated key configured for this instance — fall back to the shared one
            // rather than failing every run that never opted into its own key.
            apiKey = _configuration[DefaultApiKeyConfigKey];
        }

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                $"{_apiKeyConfigKey} (and {DefaultApiKeyConfigKey} as a fallback) is missing. " +
                $"Set it with 'dotnet user-secrets set \"{_apiKeyConfigKey}\" \"<key>\"' for " +
                "local development, or the matching double-underscore environment variable in " +
                "deployment.");
        }

        var model = NormalizeModel(_configuration["Gemini:Model"]);

        // Images lead the user turn, followed by the text — Gemini reads either order, but
        // this keeps the prompt referring to "the attached image" naturally after it.
        var userParts = new List<GeminiPart>(images.Count + 1);
        foreach (var image in images)
        {
            userParts.Add(new GeminiPart
            {
                InlineData = new GeminiInlineData { MimeType = image.MimeType, Data = image.Base64Data }
            });
        }
        userParts.Add(new GeminiPart { Text = userPrompt });

        var request = new GeminiRequest
        {
            // system_instruction is a Content without a role.
            SystemInstruction = new GeminiContent
            {
                Parts = { new GeminiPart { Text = systemPrompt } }
            },
            Contents =
            {
                new GeminiContent
                {
                    Role = "user",
                    Parts = userParts
                }
            }
        };

        if (tools.Count > 0)
        {
            request.Tools = new List<GeminiTool>
            {
                new GeminiTool
                {
                    FunctionDeclarations = tools
                        .Select(t => new GeminiFunctionDeclaration
                        {
                            Name = t.Name,
                            Description = t.Description,
                            Parameters = ParseSchema(t.Name, t.ParametersJson)
                        })
                        .ToList()
                }
            };
        }

        for (var iteration = 0; ; iteration++)
        {
            var parts = await SendAsync(model, apiKey, request, ct);

            var calls = parts
                .Where(p => p.FunctionCall is not null)
                .Select(p => p.FunctionCall!)
                .ToList();

            // No tools asked for, or the budget is spent: take whatever text came back.
            if (calls.Count == 0 || iteration >= MaxToolIterations)
            {
                return ConcatenateText(parts);
            }

            request.Contents.Add(new GeminiContent { Role = "model", Parts = parts });

            var resultParts = new List<GeminiPart>(calls.Count);
            foreach (var call in calls)
            {
                var argsJson = call.Args?.ToJsonString() ?? "{}";
                var result = await toolExecutor(call.Name, argsJson);

                resultParts.Add(new GeminiPart
                {
                    FunctionResponse = new GeminiFunctionResponse
                    {
                        Id = call.Id,
                        Name = call.Name,
                        Response = WrapToolResult(result)
                    }
                });
            }

            request.Contents.Add(new GeminiContent { Role = "user", Parts = resultParts });
        }
    }

    private async Task<List<GeminiPart>> SendAsync(
        string model,
        string apiKey,
        GeminiRequest request,
        CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient(HttpClientName);

        // The endpoint is {BaseUrl}/models/{model}:generateContent, where {model} carries no
        // "models/" prefix of its own — NormalizeModel guarantees that.
        var requestUrl = $"{BaseUrl}/models/{model}:generateContent";

        // Safe to log: the key travels in a header, never in the URL.
        _logger.LogInformation("Gemini request URL: {RequestUrl}", requestUrl);

        using var message = new HttpRequestMessage(HttpMethod.Post, requestUrl);

        // Header only — never the query string, and never logged.
        message.Headers.Add("x-goog-api-key", apiKey);
        message.Content = new StringContent(
            JsonSerializer.Serialize(request, JsonOptions),
            Encoding.UTF8,
            "application/json");

        using var response = await client.SendAsync(message, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            // 429 lands here like any other failure: surfaced, not retried behind the caller's back.
            _logger.LogWarning(
                "Gemini {RequestUrl} returned {StatusCode}: {Body}",
                requestUrl,
                (int)response.StatusCode,
                body);

            throw new LlmException(response.StatusCode, body);
        }

        var parsed = JsonSerializer.Deserialize<GeminiGenerateContentResponse>(body, JsonOptions)
            ?? throw new LlmException(response.StatusCode, body);

        return parsed.Candidates?.FirstOrDefault()?.Content?.Parts ?? new List<GeminiPart>();
    }

    /// <summary>
    /// Turns whatever is configured into a bare model id. The models.list endpoint returns
    /// names like "models/gemini-3.6-flash", and pasting one of those in verbatim would build
    /// ".../models/models/gemini-3.6-flash:generateContent" — a 404.
    /// </summary>
    private static string NormalizeModel(string? configured)
    {
        var model = configured?.Trim();

        if (string.IsNullOrEmpty(model))
        {
            return DefaultModel;
        }

        return model.StartsWith("models/", StringComparison.OrdinalIgnoreCase)
            ? model["models/".Length..].Trim()
            : model;
    }

    private static string ConcatenateText(IEnumerable<GeminiPart> parts) =>
        string.Concat(parts.Where(p => p.Text is not null).Select(p => p.Text));

    private static JsonNode ParseSchema(string toolName, string parametersJson)
    {
        var node = string.IsNullOrWhiteSpace(parametersJson) ? null : JsonNode.Parse(parametersJson);

        if (node is not JsonObject schema)
        {
            throw new InvalidOperationException(
                $"Tool '{toolName}' has ParametersJson that is not a JSON Schema object.");
        }

        return schema;
    }

    /// <summary>
    /// functionResponse.response must be an object, so a tool result that is not one
    /// travels under a "result" key.
    /// </summary>
    private static JsonNode WrapToolResult(string resultJson)
    {
        if (!string.IsNullOrWhiteSpace(resultJson))
        {
            try
            {
                if (JsonNode.Parse(resultJson) is JsonObject obj)
                {
                    return obj;
                }
            }
            catch (JsonException)
            {
                // Not JSON at all — fall through and send it as a plain string.
            }
        }

        return new JsonObject { ["result"] = resultJson };
    }

    // ===== Gemini REST wire models =====

    private sealed class GeminiRequest
    {
        [JsonPropertyName("systemInstruction")]
        public GeminiContent? SystemInstruction { get; set; }

        [JsonPropertyName("contents")]
        public List<GeminiContent> Contents { get; set; } = new();

        [JsonPropertyName("tools")]
        public List<GeminiTool>? Tools { get; set; }
    }

    private sealed class GeminiTool
    {
        [JsonPropertyName("functionDeclarations")]
        public List<GeminiFunctionDeclaration> FunctionDeclarations { get; set; } = new();
    }

    private sealed class GeminiFunctionDeclaration
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("parameters")]
        public JsonNode? Parameters { get; set; }
    }

    private sealed class GeminiContent
    {
        [JsonPropertyName("role")]
        public string? Role { get; set; }

        [JsonPropertyName("parts")]
        public List<GeminiPart> Parts { get; set; } = new();
    }

    private sealed class GeminiPart
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }

        [JsonPropertyName("inlineData")]
        public GeminiInlineData? InlineData { get; set; }

        [JsonPropertyName("functionCall")]
        public GeminiFunctionCall? FunctionCall { get; set; }

        [JsonPropertyName("functionResponse")]
        public GeminiFunctionResponse? FunctionResponse { get; set; }

        /// <summary>
        /// Opaque signature Gemini 3.x attaches to a functionCall part. The model turn is
        /// echoed back verbatim in the tool loop, so this must survive the round trip:
        /// without it the next call fails with
        /// "Function call is missing a thought_signature in functionCall parts".
        /// </summary>
        [JsonPropertyName("thoughtSignature")]
        public string? ThoughtSignature { get; set; }
    }

    private sealed class GeminiInlineData
    {
        [JsonPropertyName("mimeType")]
        public string MimeType { get; set; } = string.Empty;

        [JsonPropertyName("data")]
        public string Data { get; set; } = string.Empty;
    }

    private sealed class GeminiFunctionCall
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("args")]
        public JsonNode? Args { get; set; }
    }

    private sealed class GeminiFunctionResponse
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("response")]
        public JsonNode? Response { get; set; }
    }

    private sealed class GeminiGenerateContentResponse
    {
        [JsonPropertyName("candidates")]
        public List<GeminiCandidate>? Candidates { get; set; }
    }

    private sealed class GeminiCandidate
    {
        [JsonPropertyName("content")]
        public GeminiContent? Content { get; set; }

        [JsonPropertyName("finishReason")]
        public string? FinishReason { get; set; }
    }
}
