namespace Maui.NetworkMonitor;

/// <summary>
/// Event data raised when <see cref="INetworkMonitor.Current"/> changes.
/// </summary>
public sealed class NetworkStatusChangedEventArgs : EventArgs
{
    /// <summary>
    /// Creates event data for a status transition.
    /// </summary>
    public NetworkStatusChangedEventArgs(NetworkStatus previous, NetworkStatus current, NetworkChangeKind changeKind)
    {
        Previous = previous ?? throw new ArgumentNullException(nameof(previous));
        Current = current ?? throw new ArgumentNullException(nameof(current));
        ChangeKind = changeKind;
    }

    /// <summary>Status before this change.</summary>
    public NetworkStatus Previous { get; }

    /// <summary>Status after this change.</summary>
    public NetworkStatus Current { get; }

    /// <summary>What kind of transition occurred.</summary>
    public NetworkChangeKind ChangeKind { get; }

    /// <summary>True when the primary transport switched, including Wi-Fi ↔ cellular.</summary>
    public bool IsTransportTransition => ChangeKind == NetworkChangeKind.TransportChanged
        && Previous.PrimaryTransport != Current.PrimaryTransport;
}
