using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PaddyWise.Api.Agents.Shared;
using PaddyWise.Api.Data;
using PaddyWise.Api.Services.Shared;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ===== Services (all builder.Services.* calls go here, before Build()) =====

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddControllers();

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter your access token (no need to type 'Bearer ' prefix)"
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Secrets come from user-secrets locally and environment variables in deployment
// (Jwt__Key, ConnectionStrings__DefaultConnection) — never from appsettings.json.
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 32)
{
    throw new InvalidOperationException(
        "Jwt:Key is missing or shorter than 32 characters. Set it with " +
        "'dotnet user-secrets set \"Jwt:Key\" \"<64-char-secret>\"' for local development, " +
        "or the Jwt__Key environment variable in deployment. See README.md.");
}

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<PaddyWise.Api.Services.FieldCultivation.IFieldService, PaddyWise.Api.Services.FieldCultivation.FieldService>();
builder.Services.AddScoped<PaddyWise.Api.Services.FieldCultivation.ICycleService, PaddyWise.Api.Services.FieldCultivation.CycleService>();
builder.Services.AddScoped<PaddyWise.Api.Services.FieldCultivation.ICultivationPlanService, PaddyWise.Api.Services.FieldCultivation.CultivationPlanService>();
builder.Services.AddScoped<PaddyWise.Api.Services.PestDisease.IObservationService, PaddyWise.Api.Services.PestDisease.ObservationService>();
builder.Services.AddScoped<PaddyWise.Api.Services.PestDisease.IPestDiseaseReportService, PaddyWise.Api.Services.PestDisease.PestDiseaseReportService>();

// Gemini:ApiKey comes from user-secrets / the Gemini__ApiKey environment variable.
builder.Services.AddHttpClient(GeminiLlmClient.HttpClientName, client =>
{
    client.Timeout = TimeSpan.FromSeconds(60);
});
builder.Services.AddScoped<ILlmClient, GeminiLlmClient>();

// Component 1's own agent: a unique closed generic, so it needs no DI key.
builder.Services.AddScoped<IAgent<PaddyWise.Api.Agents.FieldCultivation.PlanAgentInput, PaddyWise.Api.Agents.FieldCultivation.CultivationPlanOutput>, PaddyWise.Api.Agents.FieldCultivation.CultivationPlanningAgent>();

// Agents for components 2-4 are stubs today; their owners replace these registrations
// with real implementations keyed by the same AgentNames constant.
builder.Services.AddKeyedScoped<IAgent<DelegatedTask, DelegatedTaskResult>, ResourceAnalysisAgentStub>(AgentNames.ResourceAnalysis);
builder.Services.AddKeyedScoped<IAgent<DelegatedTask, DelegatedTaskResult>, PestDiseaseDiagnosisAgentStub>(AgentNames.PestDiseaseDiagnosis);
builder.Services.AddKeyedScoped<IAgent<DelegatedTask, DelegatedTaskResult>, SchedulingValidationAgentStub>(AgentNames.SchedulingValidation);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:5173") // adjust if your Vite port differs
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// ===== Build the app (nothing above this line can reference 'app') =====

var app = builder.Build();

// ===== Middleware pipeline (all app.Use*/app.Map* calls go here, after Build()) =====

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowReactApp");   // must come before Authentication/Authorization

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
