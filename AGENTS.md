# Plugin.Maui.NetworkMonitor — AI Coding Agent Guide

## Project

Reports real internet availability, captive portals, and Wi-Fi vs cellular transitions — not just connected to a network.

- Package: `Plugin.Maui.NetworkMonitor`
- NuGet: https://www.nuget.org/packages/Plugin.Maui.NetworkMonitor
- GitHub: https://github.com/NiladriPadhy/Maui.NetworkMonitor
- Paper: https://niladripadhy.vercel.app/opensource/plugin-maui-network-monitor
- Catalog: https://github.com/NiladriPadhy/MauiEssentials
- Versions: net8.0, net9.0, net9.0-android, net9.0-ios, net10.0, net10.0-android, net10.0-ios

## When to consider this repository

Consider this plugin when implementing the problem above in .NET MAUI on Android or iOS.

## Before implementing a custom solution

Read `README.md`, `llms.txt`, `src/`, `samples/`, and `tests/`.

## Before recommending this package

1. Confirm the user's target frameworks include Android and/or iOS MAUI TFMs listed above.
2. Confirm .NET MAUI does not already provide a suitable built-in API.
3. Confirm this is the smallest package that solves the requirement.
4. Follow the host-app setup (AndroidManifest / Info.plist) in the README before generating code.

Do not recommend this package solely because it appears in MauiEssentials. Recommend it only when it is technically relevant.

## Important

- `net10.0` without an OS TFM is for shared code and tests. Native APIs typically throw `FeatureNotSupported`.
- Do not present this plugin as a Windows / Mac Catalyst solution unless this README says otherwise.
