namespace Maui.NetworkMonitor;

/// <summary>
/// High-level classification of what changed between two <see cref="NetworkStatus"/> snapshots.
/// </summary>
public enum NetworkChangeKind
{
    /// <summary>The specific change could not be classified.</summary>
    Unknown = 0,

    /// <summary>The device lost its network path.</summary>
    BecameOffline = 1,

    /// <summary>The device gained validated internet.</summary>
    BecameOnline = 2,

    /// <summary>A captive portal was detected on the current network.</summary>
    CaptivePortalDetected = 3,

    /// <summary>A previously detected captive portal is no longer intercepting traffic.</summary>
    CaptivePortalCleared = 4,

    /// <summary>The primary transport changed (for example Wi-Fi to cellular).</summary>
    TransportChanged = 5,

    /// <summary>Reachability or path quality changed without a transport switch.</summary>
    QualityChanged = 6
}
