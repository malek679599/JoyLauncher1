# JoyLauncher — Test Plan

## Test environments

- Target OS: Windows 11.
- Target runtime: BlueStacks 5 installed separately.
- Build target: .NET 10 WPF, Windows.
- Hardware baseline: Intel laptop with 16 GB RAM; keep memory and disk use modest.

## Current validation evidence

| Check | Result | Evidence |
|---|---|---|
| Windows Release CI build | Passed | GitHub Actions run #4, commit `09e9634` |
| WPF shell and navigation source | Committed | `src/JoyLauncher.App/MainWindow.xaml` and `.xaml.cs` |
| Home and Library Figma composition | Created and visually reviewed | JoyLauncher Figma file |
| Local Windows build | Not yet run | Requires target Windows machine |
| BlueStacks process/ADB discovery | Not implemented | Pending Phase 3 |
| Installed package enumeration | Not implemented | Pending Phase 3 |
| Real Android app launch | Not implemented | Launch action remains intentionally disabled |
| Gamepad navigation | Not implemented | Planned |

A later source polish commit and subsequent documentation commits have separate CI runs. Do not treat an earlier green run as proof that later commits have passed.

## Test matrix

| Area | Test | Expected result |
|---|---|---|
| Startup | Launch with BlueStacks open | Launcher opens; until detection exists, status remains clearly pending |
| Startup | Launch with BlueStacks closed | Launcher opens and does not crash |
| Navigation | Select each sidebar destination | Correct view and active navigation state are displayed |
| Navigation | Press Escape from a secondary view | Returns to Home |
| Library | Change category filter | Only matching sample preview cards are shown |
| Search | Search by app name or category | Matching sample cards update; empty state appears when no match exists |
| Launch | Select a verified installed package | Future ADB integration reports the real launch result |
| Failure | ADB missing or unreachable | Future integration shows a clear diagnostic; launcher remains usable |
| Failure | Package not installed | Future integration shows a clear message and no false success |
| Settings | Change and restart | Future local preferences persist |
| Configuration | Malformed settings JSON | Future implementation recovers safely or reports an actionable error |
| Display | Resize and fullscreen | Layout remains usable and readable |
| Performance | Idle on Home | No unnecessary busy loop or repeated process spawning |

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
4. BlueStacks detection and app-launch checks.
5. Failure-path checks.
6. A documented list of untested or unsupported behavior.
