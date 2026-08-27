namespace Maui.NetworkMonitor;

/// <summary>
/// Configuration for <see cref="NetworkMonitor"/>.
/// </summary>
public sealed class NetworkMonitorOptions
{
    /// <summary>
    /// Endpoints used to confirm public internet and detect captive portals.
    /// Probes stop at the first confirmed internet result.
    /// </summary>
    public IList<ProbeEndpoint> ProbeEndpoints { get; } =
    [
        ProbeEndpoint.Generate204("http://connectivitycheck.gstatic.com/generate_204"),
        ProbeEndpoint.Generate204("https://www.gstatic.com/generate_204"),
        ProbeEndpoint.SuccessContent("http://captive.apple.com/hotspot-detect.html", "Success"),
        ProbeEndpoint.SuccessContent("http://www.msftconnecttest.com/connecttest.txt", "Microsoft Connect Test"),
        ProbeEndpoint.SuccessContent("https://www.cloudflare.com/cdn-cgi/trace", "h=")
    ];

    /// <summary>Per-endpoint timeout for HTTP probes. Default is 4 seconds.</summary>
    public TimeSpan ProbeTimeout { get; set; } = TimeSpan.FromSeconds(4);

    /// <summary>
    /// How often to re-run HTTP probes while monitoring.
    /// Use <see cref="TimeSpan.Zero"/> to disable periodic re-probes. Default is 30 seconds.
    /// </summary>
    public TimeSpan ReprobeInterval { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Delay used to coalesce bursty OS network callbacks before probing. Default is 300 ms.
    /// </summary>
    public TimeSpan EventDebounce { get; set; } = TimeSpan.FromMilliseconds(300);

    /// <summary>When true, HTTP probes refine OS signals for captive portals and real internet.</summary>
    public bool EnableHttpProbe { get; set; } = true;

    /// <summary>When true, unexpected redirects, HTML, or TLS interception count as a captive portal.</summary>
    public bool EnableCaptivePortalDetection { get; set; } = true;

    /// <summary>When true, <see cref="ServiceCollectionExtensions.AddNetworkMonitor"/> starts the monitor immediately.</summary>
    public bool StartAutomatically { get; set; } = true;

    /// <summary>
    /// When true, <see cref="INetworkMonitor.StatusChanged"/> is posted to the
    /// <see cref="SynchronizationContext"/> captured at <see cref="INetworkMonitor.Start"/>.
    /// </summary>
    public bool RaiseEventsOnCapturedContext { get; set; } = true;

    internal void Validate()
    {
        if (ProbeTimeout <= TimeSpan.Zero)
        {
            throw new InvalidOperationException($"{nameof(ProbeTimeout)} must be greater than zero.");
        }

        if (ReprobeInterval < TimeSpan.Zero)
        {
            throw new InvalidOperationException($"{nameof(ReprobeInterval)} cannot be negative.");
        }

        if (EventDebounce < TimeSpan.Zero)
        {
            throw new InvalidOperationException($"{nameof(EventDebounce)} cannot be negative.");
        }

        if (EnableHttpProbe && ProbeEndpoints.Count == 0)
        {
            throw new InvalidOperationException($"{nameof(ProbeEndpoints)} cannot be empty when HTTP probing is enabled.");
        }
    }
}
