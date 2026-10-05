using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace PaddyWise.Backend.Tests.PestDisease.Helpers;

/// <summary>
/// Fake authentication for PestDiseaseApiFactory — reads X-Test-UserId / X-Test-Role headers
/// instead of validating a real JWT, so API tests can act as any user without needing a real
/// token issuer. Builds exactly the claims the controllers actually read
/// (ClaimTypes.NameIdentifier, ClaimTypes.Role), so [Authorize]/[Authorize(Roles=...)] and
/// User.FindFirst(ClaimTypes.NameIdentifier) behave the same as in production.
/// With neither header present, authentication genuinely fails (AuthenticateResult.NoResult)
/// rather than auto-authenticating — API-01 depends on an unauthenticated request staying
/// unauthenticated.
/// </summary>
public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "TestScheme";
    public const string UserIdHeader = "X-Test-UserId";
    public const string RoleHeader = "X-Test-Role";

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(UserIdHeader, out var userIdValues) ||
            !Request.Headers.TryGetValue(RoleHeader, out var roleValues))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userIdValues.ToString()),
            new Claim(ClaimTypes.Role, roleValues.ToString()),
            new Claim(ClaimTypes.Name, $"test-user-{userIdValues}"),
        };

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
