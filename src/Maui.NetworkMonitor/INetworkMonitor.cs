namespace Maui.NetworkMonitor;

/// <summary>
/// Observes OS network paths and validates real internet availability.
/// </summary>
public interface INetworkMonitor : IDisposable, IAsyncDisposable
{
    /// <summary>Most recently composed status. <see cref="NetworkStatus.Unknown"/> until the first evaluation.</summary>
    NetworkStatus Current { get; }

    /// <summary>True while OS callbacks and optional periodic probes are running.</summary>
    bool IsMonitoring { get; }

    /// <summary>Raised when the composed status changes in a meaningful way.</summary>
    event EventHandler<NetworkStatusChangedEventArgs>? StatusChanged;

    /// <summary>Starts platform listeners and an initial reachability check.</summary>
    void Start();

    /// <summary>Stops listeners and periodic probes. The last <see cref="Current"/> value is retained.</summary>
    void Stop();

    /// <summary>Immediately re-reads platform state and runs HTTP probes.</summary>
    Task<NetworkStatus> RefreshAsync(CancellationToken cancellationToken = default);
}
