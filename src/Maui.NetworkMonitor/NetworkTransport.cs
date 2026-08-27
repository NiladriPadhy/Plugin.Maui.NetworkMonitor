namespace Maui.NetworkMonitor;

/// <summary>
/// Physical or logical transport used by the active network path.
/// </summary>
public enum NetworkTransport
{
    /// <summary>No usable network interface is available.</summary>
    None = 0,

    /// <summary>A network is present but the transport could not be classified.</summary>
    Unknown = 1,

    /// <summary>IEEE 802.11 Wi-Fi.</summary>
    WiFi = 2,

    /// <summary>Cellular / WWAN (LTE, 5G, etc.).</summary>
    Cellular = 3,

    /// <summary>Wired Ethernet.</summary>
    Ethernet = 4,

    /// <summary>Bluetooth, VPN, or another non-primary transport.</summary>
    Other = 5
}
