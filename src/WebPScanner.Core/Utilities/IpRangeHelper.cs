using System.Net;
using System.Net.Sockets;
// ReSharper disable UseUtf8StringLiteral

namespace WebPScanner.Core.Utilities;

/// <summary>
/// Utility class for IP address range operations including CIDR matching
/// and private/reserved IP detection for SSRF prevention.
/// </summary>
public static class IpRangeHelper
{
    /// <summary>
    /// IPv4 private and reserved CIDR ranges that should be blocked for SSRF prevention.
    /// </summary>
    private static readonly (byte[] Network, int PrefixLength, string Description)[] IPv4PrivateRanges =
    [
        ([10, 0, 0, 0], 8, "Private network (RFC 1918)"),
        ([172, 16, 0, 0], 12, "Private network (RFC 1918)"),
        ([192, 168, 0, 0], 16, "Private network (RFC 1918)"),
        ([127, 0, 0, 0], 8, "Loopback (RFC 1122)"),
        ([169, 254, 0, 0], 16, "Link-local (RFC 3927)"),
        ([0, 0, 0, 0], 8, "Current network (RFC 1122)"),
        ([100, 64, 0, 0], 10, "Shared address space / CGNAT (RFC 6598)"),
        ([192, 0, 0, 0], 24, "IETF Protocol Assignments (RFC 6890)"),
        ([192, 0, 2, 0], 24, "Documentation TEST-NET-1 (RFC 5737)"),
        ([198, 51, 100, 0], 24, "Documentation TEST-NET-2 (RFC 5737)"),
        ([203, 0, 113, 0], 24, "Documentation TEST-NET-3 (RFC 5737)"),
        ([224, 0, 0, 0], 4, "Multicast (RFC 5771)"),
        ([240, 0, 0, 0], 4, "Reserved for future use (RFC 1112)"),
        ([198, 18, 0, 0], 15, "Benchmark testing (RFC 2544)"),
        ([192, 88, 99, 0], 24, "6to4 anycast relay (RFC 3068, deprecated)")
    ];

    /// <summary>
    /// Checks if an IP address string is within a CIDR range string.
    /// </summary>
    /// <param name="ipAddress">The IP address to check (e.g., "192.168.1.100").</param>
    /// <param name="cidrRange">The CIDR range (e.g., "192.168.1.0/24").</param>
    /// <returns>True if the IP is within the range, false otherwise.</returns>
    public static bool IsInCidrRange(string ipAddress, string cidrRange)
    {
        try
        {
            var parts = cidrRange.Split('/');
            if (parts.Length != 2 || !int.TryParse(parts[1], out var prefixLength))
                return false;

            if (!IPAddress.TryParse(parts[0], out var networkAddress) ||
                !IPAddress.TryParse(ipAddress, out var clientAddress))
                return false;

            return IsInCidrRange(clientAddress, networkAddress, prefixLength);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Checks if an IP address is within a CIDR range defined by network address and prefix length.
    /// </summary>
    /// <param name="address">The IP address to check.</param>
    /// <param name="network">The network address.</param>
    /// <param name="prefixLength">The prefix length (e.g., 24 for /24).</param>
    /// <returns>True if the IP is within the range, false otherwise.</returns>
    private static bool IsInCidrRange(IPAddress address, IPAddress network, int prefixLength)
    {
        var networkBytes = network.GetAddressBytes();
        var addressBytes = address.GetAddressBytes();

        if (networkBytes.Length != addressBytes.Length)
            return false;

        var totalBits = networkBytes.Length * 8;
        if (prefixLength < 0 || prefixLength > totalBits)
            return false;

        for (var i = 0; i < prefixLength; i++)
        {
            var byteIndex = i / 8;
            var bitIndex = 7 - i % 8;
            var mask = (byte)(1 << bitIndex);

            if ((networkBytes[byteIndex] & mask) != (addressBytes[byteIndex] & mask))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Checks if an IP address is within a CIDR range using pre-parsed network bytes.
    /// This is the most performant overload for repeated checks against the same range.
    /// </summary>
    /// <param name="addressBytes">The IP address bytes to check.</param>
    /// <param name="networkBytes">The network address bytes.</param>
    /// <param name="prefixLength">The prefix length.</param>
    /// <returns>True if the IP is within the range, false otherwise.</returns>
    private static bool IsInCidrRange(byte[] addressBytes, byte[] networkBytes, int prefixLength)
    {
        if (networkBytes.Length != addressBytes.Length)
            return false;

        var totalBits = addressBytes.Length * 8;
        if (prefixLength < 0 || prefixLength > totalBits)
            return false;

        for (var i = 0; i < prefixLength; i++)
        {
            var byteIndex = i / 8;
            var bitIndex = 7 - i % 8;
            var mask = (byte)(1 << bitIndex);

            if ((networkBytes[byteIndex] & mask) != (addressBytes[byteIndex] & mask))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Determines if an IP address is private or reserved (for SSRF prevention).
    /// Covers RFC 1918 private ranges, loopback, link-local, CGNAT, documentation ranges, etc.
    /// </summary>
    /// <param name="address">The IP address to check.</param>
    /// <returns>True if the address is private or reserved, false if it's a public address.</returns>
    public static bool IsPrivateOrReservedIp(IPAddress address)
    {
        // Handle IPv6 mapped IPv4 addresses
        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        var bytes = address.GetAddressBytes();

        // IPv4 checks
        // ReSharper disable once ConvertIfStatementToSwitchStatement
        // ReSharper disable once InvertIf
        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            foreach (var (network, prefixLength, _) in IPv4PrivateRanges)
            {
                if (IsInCidrRange(bytes, network, prefixLength))
                    return true;
            }
        }

        return address.AddressFamily switch
        {
            // IPv6 checks
            // ::1/128 - Loopback
            AddressFamily.InterNetworkV6 when IPAddress.IsLoopback(address) => true,
            // fe80::/10 - Link-local
            AddressFamily.InterNetworkV6 when bytes[0] == 0xfe && (bytes[1] & 0xc0) == 0x80 => true,
            // fc00::/7 - Unique local address
            AddressFamily.InterNetworkV6 when (bytes[0] & 0xfe) == 0xfc => true,
            // ::/128 - Unspecified address
            AddressFamily.InterNetworkV6 when address.Equals(IPAddress.IPv6None) => true,
            // ff00::/8 - Multicast
            AddressFamily.InterNetworkV6 when bytes[0] == 0xff => true,
            // fec0::/10 - Site-local (deprecated but still reserved, RFC 3879)
            AddressFamily.InterNetworkV6 when bytes[0] == 0xfe && (bytes[1] & 0xc0) == 0xc0 => true,
            // 2001:db8::/32 - Documentation (RFC 3849)
            AddressFamily.InterNetworkV6 when bytes[0] == 0x20 && bytes[1] == 0x01 && bytes[2] == 0x0d &&
                                              bytes[3] == 0xb8 => true,
            _ => false
        };
    }

    /// <summary>
    /// Determines if an IP address string is private or reserved (for SSRF prevention).
    /// </summary>
    /// <param name="ipAddress">The IP address string to check.</param>
    /// <returns>True if the address is private/reserved or unparseable, false if it's a valid public address.</returns>
    public static bool IsPrivateOrReservedIp(string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
            return true; // Treat empty/null as unsafe

        return !IPAddress.TryParse(ipAddress, out var address) ||
               // Treat unparseable as unsafe
               IsPrivateOrReservedIp(address);
    }
}
