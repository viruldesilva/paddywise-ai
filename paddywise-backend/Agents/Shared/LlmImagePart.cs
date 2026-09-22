namespace PaddyWise.Api.Agents.Shared;

/// <summary>One inline image attached to a vision-capable prompt.</summary>
public sealed record LlmImagePart(string MimeType, string Base64Data);
