# JoyLauncher

A fullscreen Windows launcher shell for BlueStacks 5, inspired by the clarity and console-first navigation of SteamOS.

**JoyLauncher is not an Android emulator or an operating system.** It is a Windows desktop application that discovers and launches apps through an existing BlueStacks installation.

## Product direction

- Minecraft is the featured flagship app, but the launcher is general-purpose.
- Support Android apps such as YouTube, YouTube Kids, Minecraft, and other apps installed in BlueStacks.
- Provide Home, Library, Games, Entertainment, Search, Store discovery, and Settings areas.
- Use a dark navy visual system, restrained blue accents, readable typography, and purposeful motion.
- Support keyboard navigation first; gamepad navigation is a planned enhancement.
- Use local configuration and robust diagnostics.
- Avoid paid APIs, subscriptions, proprietary paid assets, and unnecessary background services.

## Runtime model

`Windows 11 → JoyLauncher → BlueStacks 5 → Android applications`

BlueStacks remains responsible for Android compatibility. JoyLauncher will use supported local discovery and ADB capabilities where available. It must not assume fixed executable paths, ADB ports, or package identifiers.

## Technology direction

- C# and WPF
- .NET 10 LTS
- Git for version control
- Local JSON configuration
- BlueStacks 5 ADB integration, detected at runtime

## Current status

**Phase 0 — Project foundation.** Product requirements, architecture boundaries, roadmap, and test strategy are documented. No Windows build or BlueStacks integration has yet been verified.

## Build and test policy

A successful source edit is not a successful build. A successful build is not proof that BlueStacks integration works. Each milestone must be built and tested on the target Windows machine, with results recorded honestly.

See [Project brief](docs/PROJECT_BRIEF.md), [Architecture](docs/ARCHITECTURE.md), [Roadmap](docs/ROADMAP.md), [Test plan](docs/TEST_PLAN.md), and [Decision log](docs/DECISIONS.md).
