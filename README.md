# JoyLauncher

A Windows desktop launcher for BlueStacks 5, inspired by the clarity and controller-friendly navigation of console launchers.

**JoyLauncher is not an Android emulator or a replacement operating system.** It is a Windows application that discovers and launches Android apps through an existing BlueStacks installation.

## Product direction

- Minecraft is the featured flagship app, but the launcher is general-purpose.
- Support apps such as YouTube, YouTube Kids, Minecraft, and other apps installed in BlueStacks.
- Provide Home, Library, Games, Entertainment, Search, Store discovery, and Settings areas.
- Use a dark navy visual system, restrained blue accents, readable typography, and purposeful motion.
- Keyboard navigation comes first; gamepad navigation is planned.
- F11 toggles fullscreen; Alt+1 through Alt+7 switch the main sections; Escape returns to Home and then exits fullscreen.
- Keep preferences local and diagnostics clear.
- Avoid paid APIs, subscriptions, proprietary paid assets, and unnecessary background services.

## Runtime model

`Windows 11 → JoyLauncher → BlueStacks 5 → Android applications`

BlueStacks remains responsible for Android compatibility. JoyLauncher discovers candidate install locations and reads the local BlueStacks configuration for per-instance ADB endpoints. It does not assume one fixed install path or port.

## Technology

- C# and WPF
- .NET 10
- Local JSON preferences for favorites, pinned apps, and recent launches
- BlueStacks process/registry/config discovery
- ADB commands with argument-safe process invocation, timeouts, and captured output

## Current implementation

**Windows launcher prototype**

- Dark navy desktop shell with Home, Library, Games, Entertainment, Search, Store, and Settings navigation.
- Home page with a featured Minecraft panel and clearly marked sample app tiles.
- Library category filters, sample app cards, and a live package-list view after ADB connection.
- Persistent favorites, pinned apps, and recently launched app ordering stored in the current Windows user's local app-data folder.
- Games and Entertainment category views.
- Search across sample entries or verified package identifiers.
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
- Resolves friendly names and categories for a small set of well-known Android package IDs; unknown packages remain identifiable by their package suffix and full ID.
- Uses live package data in Library, Games, Entertainment, and Search.
- Enables the featured Minecraft action only when the exact Minecraft Android package ID is present in the verified package list.
- Provides launch actions only for package identifiers returned by the live ADB package list.
- Adds a JoyLauncher Play page that attempts true cross-process child-window hosting with `SetParent`; it checks DPI-awareness contexts first, sizes the child to the Play viewport, and restores the original top-level window when leaving the page or closing JoyLauncher.
- Loads publisher website favicons for the initial known-package set, with initials as a no-network fallback. Source domains are listed in [Website logo sources](docs/WEBSITE_LOGO_SOURCES.md).
- Uses process argument lists instead of shell command concatenation and applies command timeouts.

**Still incomplete**

- The user confirmed that the Windows prototype opens, ADB connects, and an app launch is accepted on their local BlueStacks installation. The user confirmed ADB and app launch to the standalone BlueStacks player. The current child-window hosting attempt and launcher-activity filter still need local validation.
- Cross-process window hosting is experimental and is not a BlueStacks-provided embedding API. JoyLauncher refuses the SetParent call when the host and player DPI-awareness contexts differ, because Windows documents that this can forcibly reset the child process DPI mode. Even with matching contexts, BlueStacks updates, multiple instances, graphics/window-manager behavior, or privilege boundaries can still prevent hosting. The standalone BlueStacks window remains the fallback.
- Broader app-name and icon metadata, richer recent-activity history, and category/sort controls.
- Website favicons may be absent, generic, or low resolution. JoyLauncher uses initials when a usable icon cannot be loaded.
- Full Minecraft package/activity verification and end-to-end embedded-play testing.
- Full keyboard/gamepad navigation, fullscreen kiosk behavior, local JSON preferences, and a packaged installer.

Sample app cards are demonstration data. They are not evidence that those apps are installed. Live package identifiers are shown as verified only after a successful ADB package query.

## ADB setup

BlueStacks ADB is off by default on many installations. Enable it manually in BlueStacks **Settings → Advanced → Android Debug Bridge**, then save changes. See the [official BlueStacks ADB guide](https://support.bluestacks.com/hc/en-us/articles/23925869130381-How-to-enable-Android-Debug-Bridge-on-BlueStacks-5).

JoyLauncher does not change BlueStacks settings automatically.

## Build

On Windows with the .NET 10 SDK installed:

```powershell
dotnet build JoyLauncher.sln --configuration Release
```

## Validation policy

A successful source edit is not a successful build. A successful build is not proof that BlueStacks integration works. GitHub Actions runs the Windows Release build; interactive behavior and BlueStacks compatibility still require testing on the target Windows machine.

See [Project brief](docs/PROJECT_BRIEF.md), [Architecture](docs/ARCHITECTURE.md), [Roadmap](docs/ROADMAP.md), [Test plan](docs/TEST_PLAN.md), [Design system](docs/DESIGN_SYSTEM.md), and [Art kit](docs/ART_KIT.md).
