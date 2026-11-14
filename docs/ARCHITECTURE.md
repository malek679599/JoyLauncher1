# JoyLauncher — Architecture

## Runtime boundary

JoyLauncher is a Windows WPF application. BlueStacks 5 is a separate Android runtime process.

`Windows 11 → JoyLauncher (WPF) → BlueStacks 5 ADB / supported launch interface → Android apps`

The launcher must not pretend to provide Android compatibility itself.

## Proposed solution structure

```
JoyLauncher/
  src/
    JoyLauncher.App/
      App.xaml
      App.xaml.cs
      MainWindow.xaml
      MainWindow.xaml.cs
      Views/
      ViewModels/
      Themes/
      Assets/
    JoyLauncher.Core/
      Models/
      Services/
      Abstractions/
    JoyLauncher.Infrastructure/
      BlueStacks/
      Configuration/
      Diagnostics/
  tests/
    JoyLauncher.Core.Tests/
  docs/
  assets/
    source/
    generated/
```

The project split may be simplified if that reduces build risk, but UI code, runtime integration, and persistence should remain separated.

## Technology

- C# / WPF for native Windows desktop UI.
- .NET 10 LTS target.
- MVVM-style separation for navigation and view state.
- System.Text.Json for local settings and user library metadata.
- .NET process APIs for carefully bounded process discovery and launch.
- ADB invocation through a dedicated service with timeouts, output capture, and cancellation.

## BlueStacks discovery

1. Check common install locations as hints, not assumptions.
2. Inspect registered installation information only when available.
3. Allow the user to select an executable or ADB path in Settings.
4. Discover ADB endpoints dynamically where possible.
5. Never hardcode a port, package name, or install path as the only supported option.
6. Show the source and confidence of detected runtime information in diagnostics.

ADB commands must use argument-safe process invocation rather than concatenated shell commands. Every operation needs a timeout and a useful error result.

## Core service contracts

- IBlueStacksDiscovery — finds candidate installations and runtime endpoints.
- IAdbClient — checks connectivity, lists packages, and requests app launch.
- IAppCatalogService — normalizes installed apps into launcher models.
- ISettingsStore — loads and saves local settings safely.
- INavigationService — controls selected page and focus.
- IDiagnosticsService — collects non-secret runtime and application diagnostics.

## Data and privacy

- Store settings under the user's local application data directory.
- Do not store passwords, tokens, or Google account credentials.
- Do not upload app lists or diagnostics.
- Keep logs local and avoid recording sensitive command output.
- Validate JSON on load and recover safely from malformed configuration.

## Reliability

- BlueStacks unavailable must not crash the launcher.
- ADB timeout must not freeze the UI.
- Use async operations for process and I/O work.
- Catch and classify expected failures at service boundaries.
- Never report a launch as successful until a process/ADB result supports that claim.
