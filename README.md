# Maui.NetworkMonitor

[Repository](https://github.com/NiladriPadhy/Maui.NetworkMonitor) · [NuGet](https://www.nuget.org/packages/Plugin.Maui.NetworkMonitor)

MAUI library for **Android** and **iOS** that reports *real* internet availability, not just “connected to a network”.

It watches native path changes, classifies Wi-Fi vs cellular, and uses HTTP probes (plus Android captive-portal capabilities) to detect:

- Validated public internet
- Captive portals (hotel / airport / guest Wi-Fi sign-in)
- Offline and local-network-only states
- Wi-Fi ↔ mobile transitions

## Install

[Plugin.Maui.NetworkMonitor](https://www.nuget.org/packages/Plugin.Maui.NetworkMonitor)

```bash
dotnet add package Plugin.Maui.NetworkMonitor
```

Target frameworks shipped: `net8.0`, `net9.0`, and `net10.0`, with `net9.0`/`net10.0` Android and iOS packs.

## Usage

```csharp
using Maui.NetworkMonitor;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();

        builder.Services.AddNetworkMonitor(options =>
        {
            options.EnableHttpProbe = true;
            options.EnableCaptivePortalDetection = true;
            options.ReprobeInterval = TimeSpan.FromSeconds(30);
        });

        return builder.Build();
    }
}
```

```csharp
public sealed class ConnectivityViewModel
{
    public ConnectivityViewModel(INetworkMonitor monitor)
    {
        monitor.StatusChanged += (_, e) =>
        {
            if (e.Current.IsCaptivePortal)
            {
                // Prompt the user to sign in.
            }

            if (e.IsTransportTransition)
            {
                // e.g. Wi-Fi → Cellular
            }
        };
    }
}
```

Manual use without DI:

```csharp
using var monitor = NetworkMonitor.Create();
monitor.Start();
var status = await monitor.RefreshAsync();
```

## What `NetworkStatus` tells you

| Property | Meaning |
| --- | --- |
| `HasInternet` | Public internet was validated |
| `IsCaptivePortal` | Sign-in page is intercepting traffic |
| `Reachability` | `Offline`, `LocalNetworkOnly`, `CaptivePortal`, `Internet` |
| `PrimaryTransport` | `WiFi`, `Cellular`, `Ethernet`, … |
| `IsExpensive` / `IsConstrained` | Metered / Low Data Mode |
| `ActiveTransports` | All transports on the current path |

`NetworkChangeKind.TransportChanged` is raised for Wi-Fi ↔ cellular handoffs.

## How detection works

1. **Android** — `ConnectivityManager.NetworkCallback` plus `NET_CAPABILITY_VALIDATED` and `NET_CAPABILITY_CAPTIVE_PORTAL`.
2. **iOS** — `NWPathMonitor` for path status, Wi-Fi/cellular, expensive, and constrained flags.
3. **HTTP probes** — known generate_204 / hotspot-detect endpoints. A 204 or expected success body is internet; a redirect, unexpected HTML, or TLS interception on a connected path is treated as a captive portal.

Probes use `SocketsHttpHandler` with redirects disabled so portal login pages are visible.

## Permissions

The Android package declares:

- `INTERNET`
- `ACCESS_NETWORK_STATE`
- `ACCESS_WIFI_STATE`

No extra iOS entitlements are required. Optional ATS exceptions for the HTTP probe hosts improve captive-portal accuracy if the OS blocks cleartext:

```xml
<key>NSAppTransportSecurity</key>
<dict>
  <key>NSExceptionDomains</key>
  <dict>
    <key>captive.apple.com</key>
    <dict>
      <key>NSExceptionAllowsInsecureHTTPLoads</key>
      <true/>
    </dict>
    <key>connectivitycheck.gstatic.com</key>
    <dict>
      <key>NSExceptionAllowsInsecureHTTPLoads</key>
      <true/>
    </dict>
    <key>www.msftconnecttest.com</key>
    <dict>
      <key>NSExceptionAllowsInsecureHTTPLoads</key>
      <true/>
    </dict>
  </dict>
</dict>
```

## Pack

```bash
dotnet pack src/Maui.NetworkMonitor/Maui.NetworkMonitor.csproj -c Release -o artifacts
```

## Sample

`samples/Maui.NetworkMonitor.Sample` is a MAUI Android/iOS app that shows live reachability, transport, and a change log.

## Support

> If this plugin saved you a weekend of native plumbing, consider buying me a coffee.
> Your support keeps it maintained, documented, and free.

[![Buy Me A Coffee](https://img.shields.io/badge/Buy%20Me%20a%20Coffee-ffdd00?style=for-the-badge&logo=buy-me-a-coffee&logoColor=black)](https://buymeacoffee.com/npadhy)

This library stays open source. A coffee helps cover time for bug fixes, new features, and docs.
