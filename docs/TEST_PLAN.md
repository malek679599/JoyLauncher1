# JoyLauncher — Test Plan

## Test environments

- Target OS: Windows 11.
- Target runtime: BlueStacks 5 installed separately.
- Build target: .NET 10 WPF, Windows.
- Hardware baseline: Intel laptop with 16 GB RAM; keep memory and disk use modest.

## Current validation evidence

| Check | Result | Evidence |
|---|---|---|
| Windows Release CI build and self-contained publish | Passed | GitHub Actions run #42, commit `6b79504` |
| WPF shell, navigation, Library and Search | Compiled in Windows CI | Commit `d4e9aa2` |
| BlueStacks discovery and ADB configuration parsing | Compiled in Windows CI; local behavior unverified | `BlueStacksDiscoveryService.cs` |
| ADB endpoint connection and package enumeration | Compiled in Windows CI; not yet tested against user's BlueStacks | `AdbClient.cs` |
| Verified package launch request | Compiled in Windows CI; not yet tested against user's BlueStacks | `AdbClient.LaunchPackageAsync` |
| Figma foundations and AppTile component | Created; full Home/Library composition incomplete | JoyLauncher Figma file |
| Local Windows build | Not yet run | Requires target Windows machine |
| Actual BlueStacks connection | User-confirmed working | User report, 14 Nov 2026 |
| In-launcher BlueStacks window hosting | New code; not yet locally tested | `ExternalWindowHost.cs` and `BlueStacksWindowLocator.cs` |
| Website favicon display | Implemented; network result still needs visual verification | `WebsiteLogoService.cs` |
| Friendly app names and icons | Not implemented | Package IDs are shown for now |
| Gamepad navigation | Not implemented | Planned |

Run #46 was the last green CI build before the embedded-window and favicon changes. Verify the latest candidate build before distribution.

## Test matrix

| Area | Test | Expected result |
|---|---|---|
| Startup | Launch with BlueStacks open | Launcher opens and reports discovered player/configuration evidence accurately |
| Startup | Launch with BlueStacks closed | Launcher opens and does not crash |
| Discovery | BlueStacks in a non-default install directory | Process/registry/config evidence finds it, or reports which source was unavailable |
| Discovery | Multiple configured BlueStacks instances | Each discovered endpoint is shown separately |
| ADB | ADB disabled in BlueStacks | Clear instructions to enable it; no false connected state |
| ADB | Select a configured endpoint | ADB connects and the exact selected serial is confirmed as ready |
| Library | Successful package query | Library shows only package IDs returned by the connected runtime, with known friendly labels where mapped |
| Library | Package query returns no entries | Empty live state is shown; sample cards are not presented as installed |
| Category pages | Games and Entertainment after connection | Pages filter live package data instead of showing sample tiles |
| Search | Search a verified package identifier or mapped app name | Matching live package entries appear |
| Minecraft | Exact Minecraft package ID is present | Featured Minecraft action becomes enabled and targets the verified package |
| Minecraft | Package ID is absent | Featured Minecraft action stays disabled and explains the requirement |
| Launch | Launch a package returned by the live query | ADB launch command result is reported, then JoyLauncher attempts to attach the BlueStacks window |
| Embedded play | Launch a game from a card | BlueStacks player appears in the JoyLauncher Play page |
| Embedded play | Click Back to JoyLauncher | Player detaches and its original standalone window is restored |
| Embedded play | Press Escape while the launcher has focus | Returns to Home and restores the standalone player window |
| Embedded play | Resize JoyLauncher or press F11 | Embedded player resizes with the hosting surface |
| Embedded play | Embedding is unavailable | Clear fallback text appears; Retry and Back remain usable |
| Publisher logos | Open known app cards online | Publisher website favicons appear; failed requests leave initials visible |
| Failure | ADB missing or unreachable | Clear diagnostic; launcher remains usable |
| Failure | ADB command times out | Process is stopped and UI remains responsive |
| Failure | Package cannot be launched | Clear command error; no false success |
| Navigation | Select each sidebar destination | Correct view and active navigation state are displayed |
| Navigation | Press Escape from a secondary view | Returns to Home |
| Navigation | Press Alt+1 through Alt+7 | Switches to Home, Library, Games, Entertainment, Search, Store, and Settings respectively |
| Display | Press F11 | Toggles borderless fullscreen; Escape exits fullscreen after returning to Home |
| Display | Resize and fullscreen | Layout remains usable and readable |
| Performance | Idle on Home | No unnecessary busy loop or repeated process spawning |

## Manual local validation sequence

1. Build with `dotnet build JoyLauncher.sln --configuration Release`.
2. Start BlueStacks 5.
3. In BlueStacks, enable **Settings → Advanced → Android Debug Bridge** and save changes.
4. Open JoyLauncher and select Settings.
5. Refresh local detection and verify the discovered instance and endpoint.
6. Select that instance's Connect action.
7. Open Library and confirm package identifiers appear.
8. Launch one known installed package from its verified card.
9. Confirm the game appears inside JoyLauncher, not only in a separate BlueStacks window.
10. Click Back to JoyLauncher and confirm the standalone player window is restored.
11. Resize JoyLauncher, press F11, and test keyboard/mouse input in the embedded game.
12. Check that known publisher favicons load while online and initials remain when offline.
13. Repeat with BlueStacks closed or ADB disabled and verify useful error states.

## Evidence required

For each milestone record:
- Commit SHA.
- Windows version.
- .NET SDK/runtime version.
- Build command and result.
- Test performed and observed result.
- BlueStacks version and connection state when relevant.
- Known failures and reproduction steps.

## Release gate

A release candidate is not considered validated until it has:
1. A successful Windows Release build of the candidate commit.
2. A successful launch on the target machine.
3. Keyboard navigation checks.
4. BlueStacks detection and ADB connection checks.
5. Package enumeration and verified app-launch checks.
6. Failure-path checks.
7. A documented list of untested or unsupported behavior.
