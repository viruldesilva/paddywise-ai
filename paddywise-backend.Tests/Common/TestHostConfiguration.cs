using System.Runtime.CompilerServices;

namespace PaddyWise.Backend.Tests.Common;

/// <summary>
/// Makes every WebApplicationFactory test start the real Program.cs on a machine with no
/// user-secrets — a CI runner, or a fresh clone.
///
/// Program.cs reads <c>Jwt:Key</c> and <c>ConnectionStrings:DefaultConnection</c> at the top
/// of startup, before <c>builder.Build()</c>, and throws if either is missing. The test
/// factories supply their values through <c>ConfigureAppConfiguration</c>, which minimal
/// hosting only applies at <c>Build()</c> — too late for those two reads. On a developer
/// machine the user-secrets fill the gap; on CI nothing does, and every API test fails with
/// "Jwt:Key is missing or shorter than 32 characters".
///
/// Environment variables are read when the builder is created, so setting them here — once,
/// before any test runs — satisfies the startup guards. Nothing else changes: each factory's
/// own configuration is applied later and still wins at runtime, every factory swaps the
/// database for InMemory, and the JWT tests validate against TestJwtHelper.TestJwtKey through
/// their factory's PostConfigure. A value already set in the environment is left alone.
/// </summary>
internal static class TestHostConfiguration
{
    [ModuleInitializer]
    internal static void SetStartupSettings()
    {
        SetIfMissing("Jwt__Key", TestJwtHelper.TestJwtKey);
        SetIfMissing("ConnectionStrings__DefaultConnection",
            "Host=localhost;Database=unused-in-tests;Username=unused;Password=unused");
    }

    private static void SetIfMissing(string name, string value)
    {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(name)))
            Environment.SetEnvironmentVariable(name, value);
    }
}
