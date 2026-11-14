# JoyLauncher — Test Plan

## Test environments

- Target OS: Windows 11.
- Target runtime: BlueStacks 5 installed separately.
- Build target: .NET 10 WPF, Windows.
- Hardware baseline: Intel laptop with 16 GB RAM; keep memory and disk use modest.

## Test matrix

| Area | Test | Expected result |
|---|---|---|
| Startup | Launch with BlueStacks open | Launcher opens and reports detected runtime state |
| Startup | Launch with BlueStacks closed | Launcher opens; runtime shows unavailable or stopped |
| Navigation | Use keyboard arrows, Tab, Enter, Escape | Focus remains visible and navigation is predictable |
| Library | Refresh installed apps | Loading and completion states are shown; no UI freeze |
| Launch | Launch a verified installed package | ADB/runtime response is checked and result is reported |
| Failure | ADB missing or unreachable | Clear diagnostic message; launcher remains usable |
| Failure | Package not installed | Clear message; no false success |
| Settings | Change and restart | Settings persist |
| Configuration | Malformed settings JSON | Safe defaults/recovery; no crash |
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
1. A successful Windows Release build.
2. A successful launch on the target machine.
3. Keyboard navigation checks.
4. BlueStacks detection and app-launch checks.
5. Failure-path checks.
6. A documented list of untested or unsupported behavior.
