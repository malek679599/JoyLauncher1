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
| Actual BlueStacks connection and app launch | Not yet verified | Requires local ADB-enabled instance |
| Friendly app names and icons | Not implemented | Package IDs are shown for now |
| Gamepad navigation | Not implemented | Planned |

Run #42 is the latest confirmed green build for commit `6b79504`. The artifact `JoyLauncher-Windows-x64` is available from that run until 28 November 2026.

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
| Launch | Launch a package returned by the live query | ADB launch command result is reported; command acceptance is not overstated as proof of app startup |
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
9. Repeat with BlueStacks closed or ADB disabled and verify useful error states.

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
