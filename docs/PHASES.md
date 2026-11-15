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

**State: embedding implementation remains under target-machine verification; dashboard redesign does not change the embedding mechanism.**

Scope: show the BlueStacks player as a real child window inside the Play viewport, preserve mouse/keyboard interaction, track viewport size, and restore the player when leaving Play.

Engineering approach:
- Enumerate visible top-level windows belonging to the discovered HD-Player executable.
- Compare the BlueStacks player and JoyLauncher host DPI-awareness contexts before mutating either window.
- Refuse cross-process SetParent when the contexts differ; Windows documents that an incompatible cross-process parent change can forcibly reset the child process DPI-awareness mode.
- When contexts match, switch the player style to WS_CHILD, call SetParent, size it to the Play surface client area, and verify the parent HWND.
- Restore the original top-level style, extended style, owner, and screen bounds on Back, Escape, or launcher close.
- When the player belongs to another UI thread, temporarily attach input queues before SetFocus, verify focus, and always detach in a finally block.
- If child hosting is unavailable, keep the app launch functional in the standalone BlueStacks window and show the precise reason.

Exit criteria:
- [ ] User confirms the BlueStacks player is visibly contained within the Play area.
- [ ] Confirm mouse and keyboard input work inside the game after the latest DPI-manifest build.
- [ ] Verify resizing and fullscreen work without black/blank rendering.
- [ ] Remove the BlueStacks player toolbar from the embedded Play surface; use the documented F11 immersive toggle as the first reversible test and inspect native child HWNDs before attempting any targeted hide.
- [ ] Back, Escape, and close restore a normal standalone player window.
- [ ] DPI mismatch and permission failures produce accurate diagnostics without corrupting BlueStacks.

## Phase 4 — Library and daily-use experience

**State: library features implemented and covered by Windows CI; target-machine verification remains.**

Scope: complete friendly metadata, publisher icons, search, sort, favorites, pinned apps, recents, and reliable launch feedback.

Exit criteria:
- [x] Show live packages and known package mappings.
- [x] Filter to MAIN/LAUNCHER activities when supported.
- [x] Load website favicons for the known publisher set.
- [x] Add persistent favorites and pinned apps.
- [x] Track and surface recently launched apps.
- [x] Add five sorting modes, dynamic category filters, and search by name/category/package ID.
- [x] Add broader friendly package metadata, humanized fallback names, and retryable publisher-icon failures.
- [x] Persist the selected sort mode alongside favorites, pins, and recents; legacy preference files keep the default sort mode.
- [x] Add automated tests for sorting, metadata resolution, and preference persistence.
- [x] Show launch acceptance/failure in the Library separately from embedding status.
- [ ] Verify sort preference persistence, publisher icons, and launch feedback on the target machine.

## Phase 5 — Interface polish and controls

**State: redesigned cinematic Big Picture dashboard implemented in source; target-machine verification remains open.**

Scope: final Home and Library composition, original JoyLauncher artwork, responsive layout, spatial keyboard focus navigation, and Xbox-compatible controller support.

Exit criteria:
- [x] Initial dark navy/blue design system and vector hero artwork.
- [x] Add a full-screen console dashboard with dynamic app-aware hero backdrop, utility bar, featured wide card, vertical poster carousel, feed tabs, and controller prompt footer.
- [x] Preserve live BlueStacks package data and app launch actions in the new Big Picture layout.
- [x] Add pointer-hover and keyboard/controller-focus backdrop updates; label preview and locally launchable apps distinctly.
- [ ] Verify dashboard layout, hover transitions, poster scrolling, and utility actions at 1280×720, 1366×768, and 1920×1080.
- [x] Add directional keyboard focus navigation while preserving text-field and Play-area input.
- [x] Add XInput D-pad/left-stick navigation, A/select, B/back, and Start/fullscreen handling.
- [ ] Verify keyboard and controller behavior on the target laptop; test that gamepad input passes through to BlueStacks while Play is active.
- [ ] Verify readability at common desktop resolutions.

## Phase 6 — Release readiness

**State: versioned preview packages built and verified in hosted Windows CI; target-machine acceptance and final release publication remain open.**

Scope: local build, regression tests, clean packaging, release notes, integrity manifest, and explicit known limitations.

Exit criteria:
- [x] Windows CI build and self-contained artifact.
- [x] Build provenance and SHA-256 manifest generation.
- [x] Produce a versioned self-contained portable ZIP and per-user Windows installer.
- [x] Verify the installer performs a silent install and clean uninstall on a hosted Windows runner.
- [x] Verify release-package SHA-256 hashes and upload the versioned package bundle.
- [x] Include release notes with requirements, known limitations, and unsigned-binary disclosure.
- [x] Add tag-triggered GitHub Release publishing for version tags.
- [ ] Install and launch the preview packages on the target ThinkPad.
- [ ] Complete all local Play tests, navigation checks, and failure-recovery checks.
- [ ] Publish a stable versioned release only after the target-machine acceptance gate passes.
