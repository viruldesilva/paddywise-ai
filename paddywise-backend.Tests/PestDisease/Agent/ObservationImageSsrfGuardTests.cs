using System.Net;
using System.Reflection;
using PaddyWise.Api.Agents.PestDisease;
using Xunit;

namespace PaddyWise.Backend.Tests.PestDisease.Agent;

/// <summary>
/// ObservationImageSsrfGuard.IsBlockedAddress is private static — invoked here via
/// reflection, purely a test-side technique that needed no production code change. This
/// tests the actual IP-range check deterministically and with zero network I/O; the public
/// ConnectAsync entry point itself would require a real DNS resolution and a real TCP connect
/// to prove "a public address is allowed", which isn't deterministic/CI-safe.
/// </summary>
[Trait("Component", "PestDisease")]
public class ObservationImageSsrfGuardTests
{
    private static bool IsBlocked(string ip)
    {
        var method = typeof(ObservationImageSsrfGuard).GetMethod(
            "IsBlockedAddress", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        var address = IPAddress.Parse(ip);
        return (bool)method!.Invoke(null, new object[] { address })!;
    }

    [Theory]
    [InlineData("127.0.0.1")]      // loopback
    [InlineData("10.0.0.5")]       // private (10.0.0.0/8)
    [InlineData("192.168.1.1")]    // private (192.168.0.0/16)
    [InlineData("169.254.169.254")] // link-local / cloud metadata
    [InlineData("::1")]            // IPv6 loopback
    public void AG13a_BlockedAddresses_AreRefused(string ip)
    {
        Assert.True(IsBlocked(ip));
    }

    [Fact]
    public void AG13b_APublicAddress_IsAllowed()
    {
        Assert.False(IsBlocked("8.8.8.8"));
    }
}
