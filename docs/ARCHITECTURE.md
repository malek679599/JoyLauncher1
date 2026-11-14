# JoyLauncher — Architecture

## Runtime boundary

JoyLauncher is a Windows WPF application. BlueStacks 5 is a separate Android runtime process.

`Windows 11 → JoyLauncher (WPF) → BlueStacks 5 ADB → Android applications`

JoyLauncher is not an emulator and does not replace BlueStacks. BlueStacks remains responsible for Android compatibility and application execution.

## Current solution structure

```
JoyLauncher/
  src/
    JoyLauncher.App/
      App.xaml
      MainWindow.xaml
      MainWindow.xaml.cs
      Services/
        BlueStacksDiscoveryService.cs
        AdbClient.cs
  docs/
  .github/workflows/
```

The first prototype keeps the code in one WPF project to reduce build friction. Runtime discovery and ADB process handling are separated from the view code in dedicated services. Further extraction into Core/Infrastructure projects can happen after the first local BlueStacks validation.

## Technology

- C# / WPF on .NET 10.
- System.Text.Json is planned for local preferences.
- .NET process and registry APIs for read-only Windows discovery.
- ADB invoked through ProcessStartInfo.ArgumentList, never through a concatenated shell command.
- Async process waits, bounded timeouts, captured stdout/stderr, and process cleanup on timeout.

## BlueStacks discovery

`BlueStacksDiscoveryService` checks multiple sources rather than relying on one hardcoded path:

1. Running `HD-Player` process metadata, when Windows permits reading it.
2. BlueStacks registry installation values in 64-bit and 32-bit registry views.
3. BlueStacks uninstall registration data.
4. Common install-directory hints, used only as candidates.
5. The BlueStacks configuration file for per-instance ADB port entries.

The service searches for the bundled `HD-Adb.exe` first and `adb.exe` as a fallback. It parses configured instance endpoints and shows each detected instance in Settings. It does not assume that one port is valid for every installation or instance.

Discovery is read-only. It does not change registry values, BlueStacks settings, configuration files, or user data.

## ADB connection and package discovery

The user must enable Android Debug Bridge in BlueStacks Settings → Advanced and save the setting. JoyLauncher does not toggle this setting automatically.

For a selected discovered instance, `AdbClient`:

1. Invokes `adb connect host:port` using separate process arguments.
2. Runs `adb devices -l` and requires the selected endpoint to appear in the ready `device` state.
3. Runs `pm list packages` against that exact serial.
4. Parses package identifiers and deduplicates them.
5. Returns explicit errors and timeout results instead of reporting false success.

The first live Library view displays verified package identifiers. Friendly labels, icons, and category classification are not implemented yet.

## App launch

A launch button is created only for a package identifier returned by the live package query. The service invokes Android's launcher intent through the ADB `monkey` command with an argument-safe process call and a bounded timeout.

A zero exit code means the launch request was accepted by the command; it is not proof that the app completed startup or remained open. Errors and timeouts remain visible in launcher status.

The featured Minecraft Home button remains a placeholder until a package can be identified from verified metadata or the user selects the correct package. No package identifier is hardcoded as the only supported option.

## Reliability and privacy

- BlueStacks unavailable must not crash the launcher.
- ADB timeout must not freeze the UI.
- Multiple detected instances are presented separately.
- No app is marked installed before a successful package query.
- No app is launchable until its package identifier is in the verified result list.
- Settings, package lists, and diagnostics stay local.
- Do not store passwords, tokens, or Google account credentials.
- Do not upload app lists or diagnostics.
- Never modify BlueStacks configuration without explicit approval.

## Remaining architecture work

- User-selectable fallback paths for the player and ADB executable.
- Friendly application names and icons.
- Persistent pins, recent apps, and local JSON settings.
- Unit tests for discovery parsing and ADB output handling.
- More complete diagnostics and cancellation support.
- Keyboard/gamepad navigation and packaged Windows release.


## BlueStacks Play-area docking

JoyLauncher uses a borderless top-level BlueStacks player window positioned over the Play viewport. The player remains a separate Windows process and is associated as an owned window of JoyLauncher so it can stay above the launcher and minimize/restore with it. JoyLauncher updates its screen position and size when the main window moves, resizes, maximizes, restores, or activates.

This is visual window docking, not a BlueStacks-supported in-process embedding API. It intentionally avoids cross-process SetParent because Windows documents DPI-awareness compatibility risks for cross-process parent changes. The player is restored to its original owner, styles, and bounds when the user leaves the Play page or closes JoyLauncher.