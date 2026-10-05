using System.Net;
using System.Net.Http;
using System.Net.Sockets;

namespace PaddyWise.Api.Agents.PestDisease;

/// <summary>
/// SSRF guard for CropAnalysisAgent.LoadImageAsync's outbound fetch of a farmer-supplied
/// ImageUrl (the "paste a photo URL" fallback on ObservationForm.tsx — see
/// Docs/PestDiseaseMonitoring/backend-guide.md's "Security notes"). Without this, the backend
/// would make a server-side GET to any URL a farmer types, including internal/loopback
/// addresses and cloud metadata endpoints (e.g. 169.254.169.254).
///
/// Wired in as a SocketsHttpHandler.ConnectCallback (see Program.cs's registration of
/// CropAnalysisAgent.ImageDownloadHttpClientName) rather than a pre-check on the URL string,
/// because a hostname-string check is trivially bypassed by DNS rebinding: the name could
/// resolve to a public IP when first checked and a private one by the time HttpClient actually
/// connects. ConnectCallback runs at the moment of the real TCP connect — for the initial
/// request and for every redirect hop HttpClient follows — so the IP being validated here is
/// the exact IP the socket is about to talk to, with no gap for the address to change in
/// between.
/// </summary>
public static class ObservationImageSsrfGuard
{
    public static async ValueTask<Stream> ConnectAsync(
        SocketsHttpConnectionContext context, CancellationToken cancellationToken)
    {
        var host = context.DnsEndPoint.Host;

        IPAddress[] addresses;
        try
        {
            addresses = await Dns.GetHostAddressesAsync(host, cancellationToken);
        }
        catch (Exception ex) when (ex is SocketException or ArgumentException)
        {
            throw new HttpRequestException($"Could not resolve host '{host}'.", ex);
        }

        // Skip (never connect to) any resolved address that lands in a private/internal/
        // reserved range, rather than only checking the first one — a malicious DNS answer
        // could list a public IP first and a private one later, or vice versa.
        var address = addresses.FirstOrDefault(a => !IsBlockedAddress(a))
            ?? throw new HttpRequestException(
                $"Refusing to connect to '{host}': no public IP address resolved.");

        var socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp)
        {
            NoDelay = true
        };

        try
        {
            await socket.ConnectAsync(address, context.DnsEndPoint.Port, cancellationToken);
            return new NetworkStream(socket, ownsSocket: true);
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }

    private static bool IsBlockedAddress(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6)
            address = address.MapToIPv4();

        if (IPAddress.IsLoopback(address) || address.Equals(IPAddress.Any) || address.Equals(IPAddress.IPv6Any))
            return true;

        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            var b = address.GetAddressBytes();
            return b[0] == 0                                   // 0.0.0.0/8
                || b[0] == 10                                  // 10.0.0.0/8 (private)
                || (b[0] == 100 && b[1] is >= 64 and <= 127)    // 100.64.0.0/10 (carrier-grade NAT)
                || b[0] == 127                                  // 127.0.0.0/8 (loopback)
                || (b[0] == 169 && b[1] == 254)                 // 169.254.0.0/16 (link-local, incl. cloud metadata)
                || (b[0] == 172 && b[1] is >= 16 and <= 31)      // 172.16.0.0/12 (private)
                || (b[0] == 192 && b[1] == 168)                 // 192.168.0.0/16 (private)
                || b[0] >= 224;                                 // 224.0.0.0/4 multicast + 240.0.0.0/4 reserved
        }

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
#pragma warning disable CS0618 // IsIPv6SiteLocal is obsolete but the range (fec0::/10) is still worth blocking
            if (address.IsIPv6LinkLocal || address.IsIPv6SiteLocal || address.IsIPv6Multicast)
                return true;
#pragma warning restore CS0618

            var b = address.GetAddressBytes();
            if ((b[0] & 0xFE) == 0xFC) // fc00::/7 (unique local)
                return true;
        }

        return false;
    }
}
