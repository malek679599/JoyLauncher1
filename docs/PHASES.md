# JoyLauncher — Main Development Phases

This file separates implemented work from the proof still required on the target Windows laptop. A successful CI build proves compilation and publishing, not interactive BlueStacks compatibility.

## Phase 1 — Windows launcher foundation

**State: prototype implemented; local release validation remains open.**

Scope: Windows desktop shell, navigation, Home/Library/Games/Entertainment/Search/Store/Settings pages, keyboard shortcuts, diagnostics, and self-contained Windows x64 publishing.

Exit criteria:
- [x] WPF shell and main navigation exist.
- [x] Windows x64 Release publishing runs in GitHub Actions.
- [ ] User verifies startup, navigation, fullscreen, and exit on the target laptop.

## Phase 2 — BlueStacks connectivity

**State: basic local connection and launch confirmed by the user.**

Scope: installation discovery, per-instance ADB configuration, package discovery, launch requests, app identity, and useful error reporting.

Exit criteria:
- [x] Discover BlueStacks and ADB configuration.
- [x] Connect to the user's BlueStacks ADB endpoint.
- [x] Read installed packages and request app launch.
- [ ] Verify launcher-activity filtering across the user's current BlueStacks instance and package set.

## Phase 3 — Embedded Play experience

**State: active blocker; experimental implementation updated, local result pending.**

Scope: show the BlueStacks player as a real child window inside the Play viewport, preserve mouse/keyboard interaction, track viewport size, and restore the player when leaving Play.

Engineering approach:
- Enumerate visible top-level windows belonging to the discovered HD-Player executable.
- Compare the BlueStacks player and JoyLauncher host DPI-awareness contexts before mutating either window.
- Refuse cross-process SetParent when the contexts differ; Windows documents that an incompatible cross-process parent change can forcibly reset the child process DPI-awareness mode.
- When contexts match, switch the player style to WS_CHILD, call SetParent, size it to the Play surface client area, and verify the parent HWND.
- Restore the original top-level style, extended style, owner, and screen bounds on Back, Escape, or launcher close.
- If child hosting is unavailable, keep the app launch functional in the standalone BlueStacks window and show the precise reason.

Exit criteria:
- [ ] BlueStacks player is visibly contained within the Play area.
- [ ] Mouse and keyboard input work inside the game.
- [ ] Resizing and fullscreen work without black/blank rendering.
- [ ] Back, Escape, and close restore a normal standalone player window.
- [ ] DPI mismatch and permission failures produce accurate diagnostics without corrupting BlueStacks.

## Phase 4 — Library and daily-use experience

**State: core discovery exists; management features remain planned.**

Scope: complete friendly metadata, publisher icons, search, sort, favorites, pinned apps, recents, and reliable launch feedback.

Exit criteria:
- [x] Show live packages and known package mappings.
- [x] Filter to MAIN/LAUNCHER activities when supported.
- [x] Load website favicons for the known publisher set.
- [ ] Add persistent favorites and pinned apps.
- [ ] Add sorting, category filters, recent apps, and broader metadata.
- [ ] Verify icon loading and launch feedback on the target machine.

## Phase 5 — Interface polish and controls

**State: partially implemented.**

Scope: final Home and Library composition, original JoyLauncher artwork, responsive layout, full keyboard focus navigation, and controller support.

Exit criteria:
- [x] Initial dark navy/blue design system and vector hero artwork.
- [ ] Complete Home and Library compositions.
- [ ] Finish keyboard focus navigation.
- [ ] Add gamepad/controller navigation.
- [ ] Verify readability at common desktop resolutions.

## Phase 6 — Release readiness

**State: open.**

Scope: local build, regression tests, clean packaging, release notes, integrity manifest, and explicit known limitations.

Exit criteria:
- [x] Windows CI build and self-contained artifact.
- [x] Build provenance and SHA-256 manifest generation.
- [ ] Complete all local Play tests.
- [ ] Complete navigation and failure-recovery checks.
- [ ] Package a versioned release with test evidence and known limitations.
