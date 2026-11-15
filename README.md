# JoyLauncher

A Windows desktop launcher for BlueStacks 5, inspired by the clarity and controller-friendly navigation of console launchers.

**JoyLauncher is not an Android emulator or a replacement operating system.** It is a Windows application that discovers and launches Android apps through an existing BlueStacks installation.

## Product direction

- Minecraft is the featured flagship app, but the launcher is general-purpose.
- Support apps such as YouTube, YouTube Kids, Minecraft, and other apps installed in BlueStacks.
- Provide Home, Library, Games, Entertainment, Search, Store discovery, and Settings areas.
- Use a dark navy visual system, restrained blue accents, readable typography, and purposeful motion.
- Arrow keys move focus between visible launcher controls; text-field editing and Play-area input are left alone.
- XInput-compatible controllers support D-pad / left-stick navigation, A to select, B to return, and Start to toggle fullscreen while browsing the launcher.
- F11 toggles fullscreen; Alt+1 through Alt+7 switch the main sections; Escape returns to Home and then exits fullscreen.
- Keep preferences local and diagnostics clear.
- Save unhandled exception reports locally under `%LOCALAPPDATA%\\JoyLauncher\\Logs`; never upload crash logs automatically.
- Offer a manual, package-filtered Minecraft Android crash summary from Settings; read only the recent logcat window on demand and never clear, save, or upload Android logs.
- Avoid paid APIs, subscriptions, proprietary paid assets, and unnecessary background services.

## Runtime model

`Windows 11 → JoyLauncher → BlueStacks 5 → Android applications`

BlueStacks remains responsible for Android compatibility. JoyLauncher discovers candidate install locations and reads the local BlueStacks configuration for per-instance ADB endpoints. It does not assume one fixed install path or port.

## Technology

- C# and WPF
- .NET 10
- Local JSON preferences for favorites, pinned apps, recent launches, and library sort order
- BlueStacks process/registry/config discovery
- ADB commands with argument-safe process invocation, timeouts, and captured output

## Current implementation

**Windows launcher prototype**

- Dark navy desktop shell with Home, Library, Games, Entertainment, Search, Store, and Settings navigation.
- Home page with a featured Minecraft panel and clearly marked sample app tiles.
- Library search across app names, categories, and package IDs; dynamic category filters; and five sort modes (name, category, recent, favorites first, pinned first).
- Persistent favorites, pinned apps, recent-launch ordering, and sort preference stored in the current Windows user's local app-data folder.
- Directional keyboard focus navigation and optional XInput controller navigation for console-style browsing.
- Games and Entertainment category views.
- Search across sample entries or verified package identifiers, with live Library search and no-results feedback.
- Settings view for local discovery, ADB endpoints, connection state, and diagnostics.
- Escape returns to Home from a secondary page.
- Original geometric hero artwork drawn with editable WPF vector shapes.

**BlueStacks integration code**

- Detects a running `HD-Player` process and checks registry/install-location candidates.
- Looks for the bundled `HD-Adb.exe` or `adb.exe`.
- Reads per-instance ADB ports from the local BlueStacks configuration file.
- Lets the user select a discovered instance and request an ADB connection.
- Lists package identifiers returned by the connected Android runtime.
- Queries Android Package Manager for `MAIN` + `LAUNCHER` activities and, when supported, limits app cards to packages with a normal launcher entry.
- Falls back to the installed-package list when the Android shell does not support activity discovery, and labels that state as unverified.
- Resolves friendly names, categories, and descriptions for a broader set of common Android package IDs; unknown package suffixes are humanized while the full package ID remains visible in the description.
- Uses live package data in Library, Games, Entertainment, and Search.
- Enables the featured Minecraft action only when the exact Minecraft Android package ID is present in the verified package list.
- Provides launch actions only for package identifiers returned by the live ADB package list, and reports launch acceptance/failure separately from window-embedding status.
- Adds a JoyLauncher Play page that attempts true cross-process child-window hosting with `SetParent`; it checks DPI-awareness contexts first, sizes the child to the Play viewport, and restores the original top-level window when leaving the page or closing JoyLauncher.
- Loads publisher website favicons for supported apps, with initials as a no-network fallback. Failed icon requests are not cached permanently and can retry after a network problem. Source domains are listed in [Website logo sources](docs/WEBSITE_LOGO_SOURCES.md).
- Uses process argument lists instead of shell command concatenation and applies command timeouts.

**Still requiring target-machine validation**

- The user has confirmed that ADB connects and an app launch request reaches the standalone BlueStacks player. The child-window hosting attempt, launcher-activity filter, and target-machine crash behavior still need local validation.
- Cross-process window hosting is experimental and is not a BlueStacks-provided embedding API. JoyLauncher refuses the SetParent call when the host and player DPI-awareness contexts differ, because Windows documents that this can forcibly reset the child process DPI mode. Even with matching contexts, BlueStacks updates, multiple instances, graphics/window-manager behavior, or privilege boundaries can still prevent hosting. The standalone BlueStacks window remains the fallback.
- Verify Library search, sort preference persistence, publisher favicons, and launch feedback on the target laptop.
- Verify keyboard and XInput navigation, fullscreen behavior, common resolutions, and controller pass-through to BlueStacks while Play is active.
- Complete Minecraft package/activity verification and end-to-end embedded-play testing.
- The current versioned installer and portable ZIP have passed hosted Windows CI tests, but should still be installed and launched on the target laptop before a stable/public release is tagged.

Sample app cards are demonstration data. They are not evidence that those apps are installed. Live package identifiers are shown as verified only after a successful ADB package query.

## ADB setup

BlueStacks ADB is off by default on many installations. Enable it manually in BlueStacks **Settings → Advanced → Android Debug Bridge**, then save changes. See the [official BlueStacks ADB guide](https://support.bluestacks.com/hc/en-us/articles/23925869130381-How-to-enable-Android-Debug-Bridge-on-BlueStacks-5).

JoyLauncher does not change BlueStacks settings automatically.

## Build

On Windows with the .NET 10 SDK installed:

```powershell
dotnet build JoyLauncher.sln --configuration Release
```

## Windows release packages

The Windows build workflow creates a versioned per-user installer and a portable ZIP. The current preview package uses version `0.1.0-preview.1`.

- Installer: `JoyLauncher-Setup-0.1.0-preview.1-win-x64.exe`
- Portable: `JoyLauncher-Portable-0.1.0-preview.1-win-x64.zip`
- Metadata: `BUILD-INFO.txt`, `RELEASE_NOTES.md`, and `SHA256SUMS.txt`

Download the latest verified package from the [Windows build workflow](https://github.com/malek679599/JoyLauncher1/actions/workflows/windows-build.yml). Open a successful run, then download the `JoyLauncher-Release-Windows-x64` artifact. Workflow artifacts expire after 30 days. See [Release process](docs/RELEASE_PROCESS.md) for tag-based, versioned GitHub Releases.

The installer installs for the current Windows user under `%LOCALAPPDATA%\\Programs\\JoyLauncher`; it does not require administrator privileges. BlueStacks 5 is not bundled. JoyLauncher is currently unsigned, so Windows may show an unrecognized-publisher or reputation warning; do not bypass Windows security warnings.

## Validation policy

A successful source edit is not a successful build. A successful build is not proof that BlueStacks integration works. GitHub Actions verifies compilation, automated tests, startup on a hosted Windows runner, installer install/uninstall, package hashes, and release generation. Interactive behavior, crash resolution, and BlueStacks compatibility still require testing on the target Windows machine.

See [Project brief](docs/PROJECT_BRIEF.md), [Architecture](docs/ARCHITECTURE.md), [Roadmap](docs/ROADMAP.md), [Test plan](docs/TEST_PLAN.md), [Design system](docs/DESIGN_SYSTEM.md), and [Art kit](docs/ART_KIT.md).
