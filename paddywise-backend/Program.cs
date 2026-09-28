using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PaddyWise.Api.Agents.CropResource;
using PaddyWise.Api.Agents.FieldCultivation;
using PaddyWise.Api.Agents.PestDisease;
using PaddyWise.Api.Agents.Shared;
using PaddyWise.Api.Data;
using PaddyWise.Api.Services.CropResource;
using PaddyWise.Api.Services.FieldCultivation;
using PaddyWise.Api.Services.PestDisease;
using PaddyWise.Api.Services.ReportingApproval;
using PaddyWise.Api.Services.Shared;
using Resend;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// SERVICES & JSON OPTIONS
// ============================================================

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();


// ============================================================
// SWAGGER
// ============================================================

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer",
        new Microsoft.OpenApi.Models.OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
            Scheme = "Bearer",
            BearerFormat = "JWT",
            In = Microsoft.OpenApi.Models.ParameterLocation.Header,
            Description = "Enter your JWT access token."
        });

    options.AddSecurityRequirement(
        new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
        {
            {
                new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Reference =
                        new Microsoft.OpenApi.Models.OpenApiReference
                        {
                            Type =
                                Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                },
                Array.Empty<string>()
            }
        });
});


// ============================================================
// JWT CONFIGURATION
// ============================================================

var jwtKey = builder.Configuration["Jwt:Key"];

if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 32)
{
    throw new InvalidOperationException(
        "Jwt:Key is missing or shorter than 32 characters. " +
        "Set it using user-secrets or Jwt__Key environment variable.");
}


// ============================================================
// DATABASE CONFIGURATION
// ============================================================

var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "DefaultConnection is missing. " +
        "Configure ConnectionStrings:DefaultConnection.");
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseNpgsql(
        connectionString,
        npgsqlOptions =>
        {
            // Neon/PostgreSQL transient connection failures
            npgsqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorCodesToAdd: null);
        });

    // Useful during development
    if (builder.Environment.IsDevelopment())
    {
        options.EnableDetailedErrors();
        options.EnableSensitiveDataLogging(false);
    }
});


// ============================================================
// AUTH SERVICES
// ============================================================

builder.Services.AddScoped<IAuthService, AuthService>();


// ============================================================
// RESEND EMAIL SERVICE
// ============================================================

builder.Services.AddHttpClient<IResend, ResendClient>();
builder.Services.Configure<ResendClientOptions>(options =>
{
    options.ApiToken = builder.Configuration["Resend:ApiKey"]!;
});
builder.Services.AddScoped<IEmailService, EmailService>();


// ============================================================
// FIELD CULTIVATION SERVICES (Component 1)
// ============================================================

builder.Services.AddScoped<IFieldService, FieldService>();
builder.Services.AddScoped<ICycleService, CycleService>();
builder.Services.AddScoped<ICultivationPlanService, CultivationPlanService>();


// ============================================================
// CROP RESOURCE SERVICES (Component 2)
// ============================================================

builder.Services.AddScoped<ICropActivityService, CropActivityService>();


// ============================================================
// PEST & DISEASE MONITORING SERVICES (Component 3)
// ============================================================

builder.Services.AddScoped<IObservationService, ObservationService>();
builder.Services.AddScoped<IPestDiseaseReportService, PestDiseaseReportService>();
builder.Services.AddScoped<IPestDiseaseKnowledgeService, PestDiseaseKnowledgeService>();
builder.Services.AddScoped<IPhotoStorageService, AzureBlobPhotoStorageService>();


// ============================================================
// REPORTING & APPROVAL SERVICES (Component 4)
// ============================================================

builder.Services.AddScoped<IRevisionDraftService, RevisionDraftService>();
builder.Services.AddScoped<PaddyWise.Api.Services.ReportingApproval.Agents.IValidationAgentService, PaddyWise.Api.Services.ReportingApproval.Agents.ValidationAgentService>();


// ============================================================
// GEMINI / LLM
// ============================================================

builder.Services.AddHttpClient(
    GeminiLlmClient.HttpClientName,
    client =>
    {
        // Raised from 120s: a 25-entry PestDiseaseKnowledge list (up from the original 7)
        // makes the system prompt large enough that gemini-3.1-pro-preview's very first
        // response — before any get_pest_knowledge tool round trip — can itself exceed
        // 120s. This buys more time; it doesn't establish the call was ever going to
        // finish rather than genuinely hang. Revisit if the knowledge base keeps growing.
        client.Timeout = TimeSpan.FromSeconds(240);
    });

builder.Services.AddScoped<ILlmClient, GeminiLlmClient>();

// Component 3's own Gemini key (Gemini:PestDiseaseApiKey) and model override
// (Gemini:PestDiseaseModel), each falling back to the shared Gemini:ApiKey / Gemini:Model if unset.
builder.Services.AddKeyedScoped<ILlmClient, GeminiLlmClient>(AgentNames.PestDiseaseDiagnosis, (sp, _) =>
    new GeminiLlmClient(
        sp.GetRequiredService<IHttpClientFactory>(),
        sp.GetRequiredService<IConfiguration>(),
        sp.GetRequiredService<ILogger<GeminiLlmClient>>(),
        "Gemini:PestDiseaseApiKey",
        "Gemini:PestDiseaseModel"));


// ============================================================
// AGENTIC AI
// ============================================================

// Component 1 (Cultivation Planning Coordinator)
builder.Services.AddScoped<
    IAgent<PlanAgentInput, CultivationPlanOutput>,
    CultivationPlanningAgent>();

// Component 2 (Crop Resource & Activity Analysis)
builder.Services.AddScoped<
    ICropActivityAnalysisService,
    ResourceAnalysisAgent>();

builder.Services.AddKeyedScoped<
    IAgent<DelegatedTask, DelegatedTaskResult>,
    ResourceAnalysisAgent>(
        AgentNames.ResourceAnalysis);

// Component 3 (Pest & Disease Diagnosis)
builder.Services.AddKeyedScoped<
    IAgent<DelegatedTask, DelegatedTaskResult>,
    CropAnalysisAgent>(
        AgentNames.PestDiseaseDiagnosis);

// Component 4 (Scheduling & Validation - Stub)
builder.Services.AddKeyedScoped<
    IAgent<DelegatedTask, DelegatedTaskResult>,
    SchedulingValidationAgentStub>(
        AgentNames.SchedulingValidation);


// ============================================================
// JWT AUTHENTICATION
// ============================================================

builder.Services.AddAuthentication(
    JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,

                ValidIssuer =
                    builder.Configuration["Jwt:Issuer"],

                ValidAudience =
                    builder.Configuration["Jwt:Audience"],

                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtKey)),

                ClockSkew = TimeSpan.FromMinutes(1)
            };
    });

builder.Services.AddAuthorization();


// ============================================================
// CORS
// ============================================================

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy
            .SetIsOriginAllowed(origin => new Uri(origin).Host == "localhost")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});


// ============================================================
// BUILD APPLICATION
// ============================================================

var app = builder.Build();


// ============================================================
// DATABASE CONNECTION TEST
// ============================================================

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();

    var db = scope.ServiceProvider
        .GetRequiredService<ApplicationDbContext>();

    try
    {
        Console.WriteLine("========================================");
        Console.WriteLine("Testing PostgreSQL / Neon connection...");
        Console.WriteLine("========================================");

        var canConnect = await db.Database.CanConnectAsync();

        if (canConnect)
        {
            Console.WriteLine("DATABASE CONNECTION: SUCCESS");
        }
        else
        {
            Console.WriteLine("DATABASE CONNECTION: FAILED");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine("DATABASE CONNECTION: FAILED");
        Console.WriteLine("----------------------------------------");
        Console.WriteLine(ex.Message);
        Console.WriteLine("----------------------------------------");
    }
}


// ============================================================
// HTTP PIPELINE
// ============================================================

// First, so it wraps everything below: any unhandled exception gets logged server-side and
// answered with a clean generic message — never a stack trace or the request's own headers
// (which include the caller's Authorization Bearer token). Without this, ASP.NET Core's
// Development-only Developer Exception Page — auto-enabled by WebApplication.CreateBuilder,
// with no explicit call anywhere in this file — leaks exactly that on any exception type this
// codebase doesn't happen to catch explicitly (confirmed twice: a Gemini HttpClient timeout,
// then an Azure Blob Storage RequestFailedException).
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
        context.RequestServices.GetRequiredService<ILogger<Program>>()
            .LogError(exception, "Unhandled exception on {Method} {Path}.",
                context.Request.Method, context.Request.Path);

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(
            new { message = "An unexpected error occurred. Please try again." });
    });
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowReactApp");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();


// ============================================================
// RUN
// ============================================================

app.Run();
