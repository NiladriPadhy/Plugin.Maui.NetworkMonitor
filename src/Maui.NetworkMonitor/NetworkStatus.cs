namespace Maui.NetworkMonitor;

/// <summary>
/// Immutable snapshot of connectivity, transport, and validated internet state.
/// </summary>
public sealed class NetworkStatus : IEquatable<NetworkStatus>
{
    /// <summary>Status used before the first evaluation completes.</summary>
    public static NetworkStatus Unknown { get; } = new()
    {
        Timestamp = DateTimeOffset.MinValue,
        Reachability = InternetReachability.Unknown,
        PrimaryTransport = NetworkTransport.Unknown
    };

    /// <summary>Status that represents no connected network path.</summary>
    public static NetworkStatus Offline { get; } = new()
    {
        Timestamp = DateTimeOffset.MinValue,
        Reachability = InternetReachability.Offline,
        PrimaryTransport = NetworkTransport.None
    };

    /// <summary>When this snapshot was produced.</summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>True when at least one network interface is connected.</summary>
    public bool IsConnected { get; init; }

    /// <summary>True when public internet was validated (OS and/or HTTP probe).</summary>
    public bool HasInternet { get; init; }

    /// <summary>True when a captive portal is intercepting HTTP traffic.</summary>
    public bool IsCaptivePortal { get; init; }

    /// <summary>Validated reachability classification.</summary>
    public InternetReachability Reachability { get; init; } = InternetReachability.Unknown;

    /// <summary>Preferred transport of the current default path (Wi-Fi wins over cellular).</summary>
    public NetworkTransport PrimaryTransport { get; init; } = NetworkTransport.Unknown;

    /// <summary>All transports currently contributing to the path.</summary>
    public IReadOnlyList<NetworkTransport> ActiveTransports { get; init; } = [];

    /// <summary>True when the OS marks the path as expensive or metered (typically cellular).</summary>
    public bool IsExpensive { get; init; }

    /// <summary>True when Low Data Mode / constrained networking is active.</summary>
    public bool IsConstrained { get; init; }

    /// <summary>Native interface name when the platform exposes one (for example <c>wlan0</c>).</summary>
    public string? InterfaceName { get; init; }

    /// <summary>True when the primary transport is Wi-Fi.</summary>
    public bool IsWiFi => PrimaryTransport == NetworkTransport.WiFi;

    /// <summary>True when the primary transport is cellular.</summary>
    public bool IsCellular => PrimaryTransport == NetworkTransport.Cellular;

    /// <summary>
    /// Compares two snapshots while ignoring <see cref="Timestamp"/>.
    /// </summary>
    public bool IsEquivalentTo(NetworkStatus? other)
    {
        if (other is null)
        {
            return false;
        }

        return IsConnected == other.IsConnected
            && HasInternet == other.HasInternet
            && IsCaptivePortal == other.IsCaptivePortal
            && Reachability == other.Reachability
            && PrimaryTransport == other.PrimaryTransport
            && IsExpensive == other.IsExpensive
            && IsConstrained == other.IsConstrained
            && string.Equals(InterfaceName, other.InterfaceName, StringComparison.Ordinal)
            && ActiveTransports.SequenceEqual(other.ActiveTransports);
    }

    /// <inheritdoc />
    public bool Equals(NetworkStatus? other) => IsEquivalentTo(other);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is NetworkStatus other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(IsConnected);
        hash.Add(HasInternet);
        hash.Add(IsCaptivePortal);
        hash.Add(Reachability);
        hash.Add(PrimaryTransport);
        hash.Add(IsExpensive);
        hash.Add(IsConstrained);
        hash.Add(InterfaceName);
        foreach (var transport in ActiveTransports)
        {
            hash.Add(transport);
        }

        return hash.ToHashCode();
    }

    /// <inheritdoc />
    public override string ToString() =>
        $"{Reachability} transport={PrimaryTransport} internet={HasInternet} captive={IsCaptivePortal} expensive={IsExpensive}";
}
