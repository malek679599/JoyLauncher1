# JoyLauncher

A Windows desktop launcher shell for BlueStacks 5, inspired by the clarity and controller-friendly navigation of console launchers.

**JoyLauncher is not an Android emulator or a replacement operating system.** It is a Windows application that will discover and launch Android apps through an existing BlueStacks installation.

## Product direction

- Minecraft is the featured flagship app, but the launcher is general-purpose.
- Support apps such as YouTube, YouTube Kids, Minecraft, and other apps installed in BlueStacks.
- Provide Home, Library, Games, Entertainment, Search, Store discovery, and Settings areas.
- Use a dark navy visual system, restrained blue accents, readable typography, and purposeful motion.
- Keyboard navigation comes first; gamepad navigation is planned.
- Keep preferences local and diagnostics clear.
- Avoid paid APIs, subscriptions, proprietary paid assets, and unnecessary background services.

## Runtime model

`Windows 11 → JoyLauncher → BlueStacks 5 → Android applications`

BlueStacks remains responsible for Android compatibility. JoyLauncher will use local discovery and ADB capabilities where available. It must not assume fixed executable paths, ADB ports, or package identifiers.

## Technology

- C# and WPF
- .NET 10
- Local JSON configuration (planned)
- BlueStacks 5 runtime discovery and ADB integration (planned)

## Current implementation

**Phase 1 — WPF navigation prototype**

- Dark navy desktop shell with Home, Library, Games, Entertainment, Search, Store, and Settings navigation.
- Home page with a featured Minecraft panel and clearly marked sample app tiles.
- Library category filters and app cards.
- Games and Entertainment category views.
- Search across the sample catalogue.
- Settings and diagnostics information layout.
- Escape returns to Home from a secondary page.
- Windows GitHub Actions build workflow.

**Not implemented yet**

- BlueStacks process or executable detection.
- ADB endpoint discovery and connection.
- Installed Android package enumeration.
- Real app icons, recent activity, pin persistence, or app launching.
- Gamepad navigation, fullscreen kiosk behavior, or packaged installer.

Sample app cards are demonstration data. They are not evidence that those apps are installed on the user's BlueStacks instance.

## Build

On Windows with the .NET 10 SDK installed:

```powershell
dotnet build JoyLauncher.sln --configuration Release
```

## Validation policy

A successful source edit is not a successful build. A successful build is not proof that BlueStacks integration works. The Windows CI workflow builds the solution; interactive behavior and BlueStacks compatibility still require testing on the target Windows machine.

See [Project brief](docs/PROJECT_BRIEF.md), [Architecture](docs/ARCHITECTURE.md), [Roadmap](docs/ROADMAP.md), [Test plan](docs/TEST_PLAN.md), [Design system](docs/DESIGN_SYSTEM.md), and [Art kit](docs/ART_KIT.md).
