using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PaddyWise.Api.Agents.Shared;
using PaddyWise.Api.Data;
using PaddyWise.Api.Services.Shared;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// SERVICES
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
// FIELD CULTIVATION SERVICES
// ============================================================

builder.Services.AddScoped<
    PaddyWise.Api.Services.FieldCultivation.IFieldService,
    PaddyWise.Api.Services.FieldCultivation.FieldService>();

builder.Services.AddScoped<
    PaddyWise.Api.Services.FieldCultivation.ICycleService,
    PaddyWise.Api.Services.FieldCultivation.CycleService>();

builder.Services.AddScoped<
    PaddyWise.Api.Services.FieldCultivation.ICultivationPlanService,
    PaddyWise.Api.Services.FieldCultivation.CultivationPlanService>();


// ============================================================
// CROP RESOURCE SERVICES
// ============================================================

builder.Services.AddScoped<
    PaddyWise.Api.Services.CropResource.ICropActivityService,
    PaddyWise.Api.Services.CropResource.CropActivityService>();


// ============================================================
// GEMINI / LLM
// ============================================================

builder.Services.AddHttpClient(
    GeminiLlmClient.HttpClientName,
    client =>
    {
        client.Timeout = TimeSpan.FromSeconds(60);
    });

builder.Services.AddScoped<ILlmClient, GeminiLlmClient>();


// ============================================================
// AGENTIC AI
// ============================================================

// Component 1
builder.Services.AddScoped<
    IAgent<
        PaddyWise.Api.Agents.FieldCultivation.PlanAgentInput,
        PaddyWise.Api.Agents.FieldCultivation.CultivationPlanOutput>,
    PaddyWise.Api.Agents.FieldCultivation.CultivationPlanningAgent>();


// Components 2–4
builder.Services.AddKeyedScoped<
    IAgent<DelegatedTask, DelegatedTaskResult>,
    ResourceAnalysisAgentStub>(
        AgentNames.ResourceAnalysis);

builder.Services.AddKeyedScoped<
    IAgent<DelegatedTask, DelegatedTaskResult>,
    PestDiseaseDiagnosisAgentStub>(
        AgentNames.PestDiseaseDiagnosis);

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
            .WithOrigins("http://localhost:5173")
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
//
// This will immediately tell you whether your API can connect
// to Neon PostgreSQL when the application starts.
//

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
