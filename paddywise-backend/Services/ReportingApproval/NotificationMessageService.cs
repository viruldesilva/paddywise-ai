using System.Diagnostics;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PaddyWise.Api.Agents.Shared;
using PaddyWise.Api.Data;
using PaddyWise.Api.Entities.FieldCultivation;
using PaddyWise.Api.Entities.PestDisease;
using PaddyWise.Api.Entities.Shared;

namespace PaddyWise.Api.Services.ReportingApproval;

public class NotificationMessageService : INotificationMessageService
{
    public const string AgentName = "NotificationMessageAgent";
    public const string PlanNotificationTitle = "Cultivation Plan Update";
    public const string ReportNotificationTitle = "Pest & Disease Report Update";

    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(10);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ApplicationDbContext _context;
    private readonly ILlmClient _llm;
    private readonly ILogger<NotificationMessageService> _logger;

    public NotificationMessageService(
        ApplicationDbContext context,
        ILlmClient llm,
        ILogger<NotificationMessageService> logger)
    {
        _context = context;
        _llm = llm;
        _logger = logger;
    }

    public async Task<Notification?> GenerateAndCreateCultivationPlanNotificationAsync(int planId, CancellationToken ct = default)
    {
        try
        {
            var plan = await _context.CultivationPlans
                .FirstOrDefaultAsync(p => p.Id == planId, ct);

            if (plan == null)
            {
                _logger.LogWarning("CultivationPlan {PlanId} not found when generating notification.", planId);
                return null;
            }

            var fallbackMessage = $"Your cultivation plan status has been updated to {plan.Status}. Check the app for details.";
            var userPrompt = plan.Status switch
            {
                PlanStatus.Approved =>
                    $"Write a short, encouraging 1-2 sentence message telling a farmer their cultivation plan for '{plan.Objective}' has been approved and is ready to follow.",
                PlanStatus.Rejected or PlanStatus.RevisionRequested =>
                    $"Write a short, clear, respectful 1-2 sentence message telling a farmer their cultivation plan needs changes, based on this officer feedback: '{plan.OfficerComment}'. Keep it encouraging, not discouraging.",
                _ =>
                    $"Write a short, clear, respectful 1-2 sentence message telling a farmer their cultivation plan status is now '{plan.Status}'."
            };

            var systemPrompt = "You are an expert Agricultural Extension Officer assistant for Sri Lankan paddy farming. " +
                               "Write a short, plain-language notification message for the farmer. " +
                               "Do not use markdown formatting or bullet points. Keep it strictly to 1-2 clear, respectful, plain-language sentences.";

            var correlationId = Guid.NewGuid().ToString("N");
            var inputJson = JsonSerializer.Serialize(new
            {
                planId = plan.Id,
                objective = plan.Objective,
                status = plan.Status.ToString(),
                officerComment = plan.OfficerComment
            }, JsonOptions);

            var stopwatch = Stopwatch.StartNew();
            string rawOutput = string.Empty;
            bool success = false;
            string? error = null;
            string message = fallbackMessage;

            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(DefaultTimeout);

                var reply = await _llm.CompleteJsonAsync(
                    systemPrompt,
                    userPrompt,
                    Array.Empty<LlmToolDefinition>(),
                    (_, _) => Task.FromResult("{}"),
                    cts.Token);

                stopwatch.Stop();

                if (!string.IsNullOrWhiteSpace(reply))
                {
                    rawOutput = reply.Trim();
                    message = rawOutput;
                    success = true;
                }
                else
                {
                    error = "LLM returned an empty response.";
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogWarning(ex, "Plain-language notification generation failed for plan {PlanId} (correlation {CorrelationId}).", planId, correlationId);
                error = ex.Message;
                message = fallbackMessage;
            }

            // Persist AgentRunLog audit trail
            try
            {
                _context.AgentRunLogs.Add(new AgentRunLog
                {
                    CultivationPlanId = planId,
                    AgentName = AgentName,
                    CorrelationId = correlationId,
                    InputJson = inputJson,
                    ToolCallsJson = "[]",
                    RawOutput = rawOutput,
                    Success = success,
                    Error = error,
                    DurationMs = (int)stopwatch.ElapsedMilliseconds,
                    CreatedAt = DateTime.UtcNow
                });
                await _context.SaveChangesAsync(CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to persist AgentRunLog for plan {PlanId} (correlation {CorrelationId}).", planId, correlationId);
            }

            // Persist Notification
            var notification = new Notification
            {
                UserId = plan.RequestedByUserId,
                Title = PlanNotificationTitle,
                Message = message,
                Type = "CultivationPlanReview",
                Status = plan.Status.ToString(),
                OfficerComment = plan.OfficerComment,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync(CancellationToken.None);

            return notification;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error in GenerateAndCreateCultivationPlanNotificationAsync for plan {PlanId}.", planId);
            return null;
        }
    }

    public async Task<Notification?> GenerateAndCreatePestDiseaseReportNotificationAsync(int reportId, CancellationToken ct = default)
    {
        try
        {
            var report = await _context.PestDiseaseReports
                .Include(r => r.CropObservation)
                .FirstOrDefaultAsync(r => r.Id == reportId, ct);

            if (report == null || report.CropObservation == null)
            {
                _logger.LogWarning("PestDiseaseReport {ReportId} or its CropObservation not found when generating notification.", reportId);
                return null;
            }

            var fallbackMessage = $"Your pest and disease report status has been updated to {report.Status}. Check the app for details.";
            var userPrompt = report.Status switch
            {
                PestDiseaseReportStatus.Approved =>
                    $"Write a short, encouraging 1-2 sentence message telling a farmer their pest and disease report regarding '{report.PossibleIssue}' has been reviewed and approved by the agricultural officer.",
                PestDiseaseReportStatus.Rejected or PestDiseaseReportStatus.RevisionRequested =>
                    $"Write a short, clear, respectful 1-2 sentence message telling a farmer their pest and disease report needs changes, based on this officer feedback: '{report.OfficerComment}'. Keep it encouraging, not discouraging.",
                _ =>
                    $"Write a short, clear, respectful 1-2 sentence message telling a farmer their pest and disease report status is now '{report.Status}'."
            };

            var systemPrompt = "You are an expert Agricultural Extension Officer assistant for Sri Lankan paddy farming. " +
                               "Write a short, plain-language notification message for the farmer. " +
                               "Do not use markdown formatting or bullet points. Keep it strictly to 1-2 clear, respectful, plain-language sentences.";

            var correlationId = Guid.NewGuid().ToString("N");
            var inputJson = JsonSerializer.Serialize(new
            {
                reportId = report.Id,
                cropObservationId = report.CropObservationId,
                possibleIssue = report.PossibleIssue,
                status = report.Status.ToString(),
                officerComment = report.OfficerComment
            }, JsonOptions);

            var stopwatch = Stopwatch.StartNew();
            string rawOutput = string.Empty;
            bool success = false;
            string? error = null;
            string message = fallbackMessage;

            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(DefaultTimeout);

                var reply = await _llm.CompleteJsonAsync(
                    systemPrompt,
                    userPrompt,
                    Array.Empty<LlmToolDefinition>(),
                    (_, _) => Task.FromResult("{}"),
                    cts.Token);

                stopwatch.Stop();

                if (!string.IsNullOrWhiteSpace(reply))
                {
                    rawOutput = reply.Trim();
                    message = rawOutput;
                    success = true;
                }
                else
                {
                    error = "LLM returned an empty response.";
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogWarning(ex, "Plain-language notification generation failed for report {ReportId} (correlation {CorrelationId}).", reportId, correlationId);
                error = ex.Message;
                message = fallbackMessage;
            }

            // Persist DiagnosisRunLog audit trail
            try
            {
                _context.DiagnosisRunLogs.Add(new DiagnosisRunLog
                {
                    CropObservationId = report.CropObservationId,
                    AgentName = AgentName,
                    CorrelationId = correlationId,
                    InputJson = inputJson,
                    ToolCallsJson = "[]",
                    RawOutput = rawOutput,
                    Success = success,
                    Error = error,
                    DurationMs = (int)stopwatch.ElapsedMilliseconds,
                    CreatedAt = DateTime.UtcNow
                });
                await _context.SaveChangesAsync(CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to persist DiagnosisRunLog for report {ReportId} (correlation {CorrelationId}).", reportId, correlationId);
            }

            // Persist Notification
            var notification = new Notification
            {
                UserId = report.CropObservation.ReportedByUserId,
                Title = ReportNotificationTitle,
                Message = message,
                Type = "PestDiseaseReportReview",
                Status = report.Status.ToString(),
                OfficerComment = report.OfficerComment,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync(CancellationToken.None);

            return notification;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error in GenerateAndCreatePestDiseaseReportNotificationAsync for report {ReportId}.", reportId);
            return null;
        }
    }
}
