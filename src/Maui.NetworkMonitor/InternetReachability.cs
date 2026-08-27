namespace Maui.NetworkMonitor;

/// <summary>
/// Validated reachability of the public internet, including captive-portal states.
/// </summary>
public enum InternetReachability
{
    /// <summary>No evaluation has completed yet.</summary>
    Unknown = 0,

    /// <summary>No network interface is connected.</summary>
    Offline = 1,

    /// <summary>A local or carrier network is up, but public internet could not be validated.</summary>
    LocalNetworkOnly = 2,

    /// <summary>Traffic is intercepted by a captive portal that requires a sign-in.</summary>
    CaptivePortal = 3,

    /// <summary>Public internet was validated by the OS and/or an HTTP probe.</summary>
    Internet = 4
}
