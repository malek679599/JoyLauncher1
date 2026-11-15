# JoyLauncher — Test Plan

## Test environments

- Target OS: Windows 11.
- Target runtime: BlueStacks 5 installed separately.
- Build target: .NET 10 WPF, Windows.
- Hardware baseline: Intel laptop with 16 GB RAM; keep memory and disk use modest.

## Current validation evidence

| Check | Result | Evidence |
|---|---|---|
| Windows Release CI build and self-contained publish | Passed | GitHub Actions run #54, commit `1e6fe5c` |
| WPF shell, navigation, Library and Search | Compiled in Windows CI | Commit `d4e9aa2` |
| BlueStacks discovery and ADB configuration parsing | Compiled in Windows CI; local behavior unverified | `BlueStacksDiscoveryService.cs` |
| ADB endpoint connection and package enumeration | User-confirmed working locally | User report, 14 Nov 2026 |
| ADB app launch to separate BlueStacks player | User-confirmed working; not embedded | User report, 14 Nov 2026 |
| Figma foundations and AppTile component | Created; full Home/Library composition incomplete | JoyLauncher Figma file |
| Local Windows build | Not yet run | Requires target Windows machine |
| Actual BlueStacks connection | User-confirmed working | User report, 14 Nov 2026 |
| In-launcher BlueStacks child-window hosting | Experimental guarded `SetParent` implementation; not yet locally tested | `ExternalWindowHost.cs` and `BlueStacksWindowLocator.cs` |
| Website favicon display | Implemented; network result still needs visual verification | `WebsiteLogoService.cs` |
| Known publisher website favicons | Implemented; visual/network test pending | `WebsiteLogoService.cs` |
| Android launcher-activity discovery | Implemented using `cmd package query-activities --components`; local BlueStacks behavior pending | `AdbClient.cs` |
| Unsupported package-manager fallback | Implemented; displays all installed packages with an unverified status | `AdbClient.cs`, Settings page |
| Keyboard directional navigation | Implemented in the WPF shell; target-machine behavior pending | `Window_PreviewKeyDown` and spatial focus navigation |
| XInput controller navigation | Implemented for D-pad/left stick, A, B/Back, and Start; target-machine behavior pending | `XInputGamepadService.cs` |

Run #54 passed the Windows Release build and self-contained publish for the embedded-play and favicon code. The current head adds only a diagnostics-report field; its run #59 is queued. Artifact `JoyLauncher-Windows-x64` from run #54 expires 28 November 2026.

## Test matrix

| Area | Test | Expected result |
|---|---|---|
| Startup | Launch with BlueStacks open | Launcher opens and reports discovered player/configuration evidence accurately |
| Startup | Launch with BlueStacks closed | Launcher opens and does not crash |
| Discovery | BlueStacks in a non-default install directory | Process/registry/config evidence finds it, or reports which source was unavailable |
| Discovery | Multiple configured BlueStacks instances | Each discovered endpoint is shown separately |
| ADB | ADB disabled in BlueStacks | Clear instructions to enable it; no false connected state |
| ADB | Select a configured endpoint | ADB connects and the exact selected serial is confirmed as ready |
| Library | Launcher-activity query works | Library shows only installed packages returned with Android MAIN/LAUNCHER components |
| Library | Launcher-activity query is unsupported | Library remains available, shows all installed packages as a fallback, and clearly marks launchability as unverified |
| Library | Package query returns no entries | Empty live state is shown; sample cards are not presented as installed |
| Category pages | Games and Entertainment after connection | Pages filter live package data instead of showing sample tiles |
| Search | Search a verified package identifier or mapped app name | Matching live package entries appear |
| Minecraft | Exact Minecraft package ID has a MAIN/LAUNCHER activity | Featured Minecraft action becomes enabled and targets the discovered package |
| Minecraft | Activity query unsupported | Featured Minecraft uses compatibility fallback and Settings labels launchability as unverified |
| Minecraft | Package ID is absent | Featured Minecraft action stays disabled and explains the requirement |
| Launch | Launch a package returned by the live query | ADB launch command result is reported, then JoyLauncher attempts to attach the BlueStacks window |
| Embedded play | Launch a game from a card with matching DPI-awareness contexts | BlueStacks player becomes a child of the JoyLauncher Play surface and fills its client area |
| Embedded play | Launch when DPI-awareness contexts differ | JoyLauncher refuses SetParent before changing the BlueStacks window and reports both contexts and DPI values |
| Embedded play | Click Back to JoyLauncher | Player detaches, and its original standalone styles, owner, and bounds are restored |
| Embedded play | Press Escape while the launcher has focus | Returns to Home and restores the standalone player window |
| Embedded play | Resize JoyLauncher or press F11 | Embedded child HWND resizes with the hosting surface; verify at the same and different monitor DPI scales |
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
| Keyboard navigation | Use arrow keys while a button is focused | Focus moves to the nearest visible control in that direction; search text editing keeps its arrow-key behavior |
| Controller | Connect an XInput-compatible controller and use D-pad / left stick | Focus moves through visible launcher controls |
| Controller | Press A, B, and Start | A activates the focused button, B returns from a secondary page, and Start toggles fullscreen |
| Controller / Play | Launch an app and use the controller in the Play area | JoyLauncher does not issue launcher navigation actions while the Play page is active; confirm BlueStacks still receives controller input |
| Performance | Idle on Home | No unnecessary busy loop or repeated process spawning |

## Manual local validation sequence

1. Build with `dotnet build JoyLauncher.sln --configuration Release`.
2. Start BlueStacks 5.
3. In BlueStacks, enable **Settings → Advanced → Android Debug Bridge** and save changes.
4. Open JoyLauncher and select Settings.
5. Refresh local detection and verify the discovered instance and endpoint.
6. Select that instance's Connect action.
7. Open Library and confirm package identifiers appear.
8. Launch one known installed package from its verified card and confirm it opens in BlueStacks.
9. Confirm the player becomes a child window inside JoyLauncher, not a separate overlay.
10. Check Settings diagnostics for matching DPI-awareness contexts; if they differ, confirm JoyLauncher refuses attachment and leaves BlueStacks unchanged.
11. Click Back to JoyLauncher and confirm the standalone player window is restored with its original style and size.
12. Resize JoyLauncher, press F11, and test keyboard/mouse input in the embedded game.
13. Check that known publisher favicons load while online and initials remain when offline.
14. Repeat with BlueStacks closed or ADB disabled and verify useful error states.

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


## Big Picture mode and embedded toolbar validation

- [ ] Open Big Picture from the sidebar or press F10; confirm full-screen presentation.
- [ ] Navigate the large app tiles with arrow keys or controller D-pad/left stick; A/Enter launches; B/Escape exits.
- [ ] Launch Minecraft from Big Picture; confirm JoyLauncher returns to Big Picture after leaving the game.
- [ ] In the embedded Play view, use **Hide top bar · F11** once. Confirm the BlueStacks toolbar disappears and the player remains parented to JoyLauncher.
- [ ] Use the same control again to restore the toolbar.
- [ ] Copy diagnostics and include the **BlueStacks embedded child-window inventory** if the toolbar remains visible.
