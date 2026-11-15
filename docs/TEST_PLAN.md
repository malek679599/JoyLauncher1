# JoyLauncher — Test Plan

## Test environments

- Target OS: Windows 11.
- Target runtime: BlueStacks 5 installed separately.
- Build target: .NET 10 WPF, Windows.
- Hardware baseline: Intel laptop with 16 GB RAM; keep memory and disk use modest.

## Current validation evidence

| Check | Result | Evidence |
|---|---|---|
| Windows Release build | Passed; 0 warnings and 0 errors | GitHub Actions run #103, commit `3ae05aa601b4aa91c3ffd0ccc3fc3d392c9027ca` |
| Automated regression suite | Passed; 17/17 tests | Run #103 |
| Self-contained Windows x64 publish | Passed | Run #103 |
| Hosted Windows startup smoke test | Passed; executable remained open for the 8-second check without BlueStacks | Run #103 |
| Inno Setup compiler integrity | Passed; pinned Inno Setup 7.1.1 installer SHA-256 and Authenticode publisher signature verified | Run #103 |
| Per-user Windows installer | Passed; installer compiled successfully | Run #103 |
| Installer lifecycle smoke test | Passed; silent install created the app and uninstaller, then silent uninstall removed the executable | Run #103 |
| Portable ZIP and release metadata | Passed; versioned ZIP, build info, release notes, and SHA-256 manifest generated | Run #103 |
| Release package integrity | Passed; every file listed in the release checksum manifest verified | Run #103 |
| Versioned release artifact | Uploaded as `JoyLauncher-Release-Windows-x64`; artifact ID `14173769008`, expires 15 December 2026 | [Run #103](https://github.com/malek679599/JoyLauncher1/actions/runs/43745381418) |
| Library sorting | Passed; name, category, recent, favorites-first, pinned-first, and invalid-mode fallback | `AppLibrarySorterTests.cs`, run #103 |
| App metadata resolution | Passed; known package mapping and unknown-package humanization | `AppMetadataCatalogTests.cs`, run #103 |
| Sort preference persistence | Passed; roundtrip, old settings without a sort field, and invalid sort normalization | `LocalPreferencesStoreTests.cs`, run #103 |
| ADB endpoint connection and package enumeration | User-confirmed working locally | User report, 14 Nov 2026 |
| ADB app launch to separate BlueStacks player | User-confirmed working; not embedded | User report, 14 Nov 2026 |
| Actual BlueStacks connection | User-confirmed working | User report, 14 Nov 2026 |
| In-launcher BlueStacks child-window hosting | Experimental guarded `SetParent` implementation; not yet locally tested | `ExternalWindowHost.cs`, `BlueStacksWindowLocator.cs` |
| Target-laptop startup, navigation, and package installation | Not yet confirmed on the user's ThinkPad | Hosted CI does not replace target-machine validation |
| Keyboard and XInput controller behavior | Implemented; target-machine verification pending | `Window_PreviewKeyDown`, `XInputGamepadService.cs` |

Run #103 passed on commit `3ae05aa601b4aa91c3ffd0ccc3fc3d392c9027ca` with 0 compiler warnings, 0 errors, all 17 tests, self-contained Windows x64 publishing, an 8-second startup smoke test, installer compilation, per-user install/uninstall smoke tests, portable package generation, and release checksum verification. The tag-triggered GitHub Release step was skipped because the run was for `main`, not a version tag. The artifact is retained for 30 days and expires 15 December 2026.

Hosted Windows checks do not prove that JoyLauncher works on the user's ThinkPad, that BlueStacks embeds successfully, or that the earlier user-reported crash is resolved. Those remain explicit manual release gates.
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
| Library | Search by name, category, or package identifier | The visible package grid updates and shows a no-results state when empty |
| Library | Change sort mode and restart JoyLauncher | Selected sort mode remains saved locally |
| Library | Sort by recent/favorite/pinned/category/name | The visible package order follows the selected sort mode |
| Library | Launch or fail to launch a verified package | The Library reports launch acceptance/failure separately from embedding status |
| Library | Unknown package identifier | Humanized suffix, Other category, and full package ID are shown |
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
| Embedded keyboard focus | Attach a player owned by a different UI thread | Input queues attach before SetFocus and detach even when focus fails; copied diagnostics report any Win32 failure |
| Performance | Idle on Home | No unnecessary busy loop or repeated process spawning |

## Crash log capture

Unhandled WPF dispatcher, AppDomain, and unobserved task exceptions are written locally to:
`%LOCALAPPDATA%\JoyLauncher\Logs\crash-*.log`

Crash reports are not uploaded automatically. They include the exception message and stack trace, Windows version, application version, process ID, and local timestamp. The WPF dispatcher handler deliberately does not mark the exception as handled; the application keeps its normal failure behavior while preserving the report.

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
15. Change the Library sort order, favorite and pin an app, close/reopen JoyLauncher, and verify all preferences persisted.
16. Search the Library by friendly name and package ID, then test each sort mode and category filter.
17. Install the versioned per-user installer, start JoyLauncher, uninstall it, and confirm the app is removed. The hosted CI already smoke-tests install/uninstall; this step confirms the target laptop.

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
8. A completed install/uninstall check of the versioned per-user installer on the target machine.


## Big Picture mode and embedded toolbar validation

- [ ] Open Big Picture from the sidebar or press F10; confirm full-screen presentation.
- [ ] Navigate the large app tiles with arrow keys or controller D-pad/left stick; A/Enter launches; B/Escape exits.
- [ ] Launch Minecraft from Big Picture; confirm JoyLauncher returns to Big Picture after leaving the game.
- [ ] In the embedded Play view, use **Hide top bar · F11** once. Confirm the BlueStacks toolbar disappears and the player remains parented to JoyLauncher.
- [ ] Use the same control again to restore the toolbar.
- [ ] Copy diagnostics and include the **BlueStacks embedded child-window inventory** if the toolbar remains visible.
