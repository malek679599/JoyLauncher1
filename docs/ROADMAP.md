# JoyLauncher — Roadmap

## Phase 0 — Foundation
- [x] Create project repository.
- [x] Record product scope and non-goals.
- [x] Record architecture and runtime boundaries.
- [x] Record milestone test strategy.
- [ ] Confirm Windows development environment and free disk space.

## Phase 1 — Windows shell
- [x] Create .NET 10 WPF solution.
- [x] Implement the dark desktop shell and sidebar navigation.
- [x] Build the Home view with a featured Minecraft area.
- [x] Build Library, Games, Entertainment, Search, Store placeholder, and Settings views.
- [x] Add sample-data labels and honest runtime-pending messages.
- [x] Add Escape-to-Home behavior from secondary pages.
- [x] Add directional keyboard focus navigation and XInput controller navigation.
- [x] Add F11 fullscreen, Big Picture mode, and controller/keyboard fullscreen controls.
- [x] Persist local favorites, pins, recent launches, sort preference, and experimental-embedding preference.

## Phase 2 — Art and design system
- [x] Define the initial dark navy/blue color, spacing, radius, and typography tokens.
- [x] Create the Figma workspace, Foundations and Components pages, design tokens, text styles, and an AppTile component.
- [ ] Complete and review the Home and Library screen compositions in Figma (blocked by current Figma automation allowance).
- [ ] Create approved feature artwork and transfer it into the WPF shell.
- [ ] Finalize original JoyLauncher logo and Windows app icon.
- [ ] Add production app icons and replacement-ready feature artwork.
- [ ] Verify contrast and readability at common desktop resolutions.
- [ ] Transfer and reconcile the approved Figma design into WPF styles.

## Phase 3 — BlueStacks integration
- [x] Detect BlueStacks installation candidates from process, registry, and install-directory evidence.
- [x] Find the bundled ADB executable when present.
- [x] Read per-instance ADB endpoint configuration without a fixed-port assumption.
- [x] Show detected, configured, connected, and failed states separately.
- [x] Connect to a selected endpoint and list verified Android package identifiers.
- [x] Add launch requests for packages returned by the live ADB query.
- [x] Add bounded ADB command timeouts and process cleanup.
- [x] Add user-triggered, package-filtered Android crash summaries for Minecraft without clearing, saving, or uploading raw logcat data.
- [x] Implement guarded cross-process child-window hosting with DPI-awareness preflight, diagnostics, and a restore path when leaving Play (target-machine validation remains open).
- [ ] Validate actual child-window rendering, input, resizing, fullscreen, DPI behavior, and window restoration on the user's BlueStacks setup.
- [ ] Add user-configurable fallback paths.
- [x] Test discovery and ADB connection against the user's BlueStacks installation (user-confirmed).

- [x] Add a first-pass publisher website favicon catalog for known packages, with initials as a local fallback.
- [x] Query Android Package Manager for MAIN/LAUNCHER activities and filter normal app entries when supported.
- [ ] Resolve remaining friendly app names, native app icons, and category metadata.

## Phase 4 — Library experience
- [x] Build preview app cards, category filters, and sample search.
- [x] Populate the Library with package identifiers after a successful ADB package query.
- [x] Allow launch requests only for verified package identifiers.
- [x] Add publisher website favicons for the initial known-package set.
- [x] Filter package results to normal launcher activities when the Android runtime supports it.
- [ ] Resolve remaining app names and higher-quality app-specific icons.
- [x] Search, sort, pin, favourite, and filter discovered apps.
- [x] Track recent launch requests locally and sort by recency (ADB acceptance is not proof that the app stayed open).
- [ ] Match Minecraft to the correct verified package without relying on an unverified guess.
- [x] Show launch-command acceptance/failure separately from embedding status; full app-details view remains open.

## Phase 5 — Quality and release
- [x] Add a Windows GitHub Actions Release build workflow.
- [x] Verify Windows CI Release build, tests, self-contained publish, startup, installer lifecycle, portable ZIP, and checksums (run #113, commit `966b311`).
- [ ] Run a Release build and install the artifact on the user's Windows machine.
- [x] Verify local ADB connection and an app launch to the separate BlueStacks window (user-confirmed).
- [ ] Validate in-launcher embedding and full input/restore behavior.
- [ ] Test keyboard navigation and failure recovery.
- [x] Add automated tests for package metadata, sorting, preferences, diagnostics, and focus coordination.\n- [ ] Add direct automated tests for BlueStacks discovery parsing and ADB package/launchability output.
- [x] Generate versioned per-user installer and portable ZIP artifacts in CI.
- [x] Generate release notes and build/checksum metadata with each CI artifact; tag-based public release still requires a version tag.

## Working rules

- One implementation owner per file/task at a time.
- Review before replacing working code.
- Keep commits small and descriptive.
- Never claim a feature is tested unless the test was actually run.
- Prefer reversible changes and preserve the user's existing BlueStacks setup.
- No paid APIs, subscriptions, or paid assets.

## Current status (16 November 2026)\n\n- Latest source: `966b311e20f64a522abc45961e63836fafa6c8f2`.\n- Latest hosted Windows CI: [run #113](https://github.com/malek679599/JoyLauncher1/actions/runs/43770531865), successful; 24/24 tests, 0 build warnings, 0 build errors, self-contained publish, startup smoke test, installer install/uninstall, portable package, and checksum verification.\n- The CI artifact is available until 15 December 2026.\n- Still unverified on the target laptop: Minecraft crash cause/resolution, keyboard/gamepad behavior, Big Picture navigation, and true BlueStacks window hosting/input/restore.\n\n## Current boundaries

- Before a successful ADB query, the Home view uses clearly labelled preview content. After connection, app cards are based on package IDs returned by the selected BlueStacks instance.
- The featured Minecraft action is enabled only when the exact Minecraft package ID is present in that live package list.
- User-confirmed local validation covers the launcher opening, ADB connection, and app launch to the standalone BlueStacks player. The in-launcher external-window host compiles in CI (run #54) but still needs local testing; launcher-activity discovery is now attempted; older Android shells fall back to all installed package IDs and clearly mark launchability as unverified. Window re-parenting is best-effort and is not a BlueStacks-supported embed API.
- The Figma workspace currently contains Foundations and Components pages, design tokens, text styles, and an AppTile component. A complete Home/Library composition has not yet been completed; further Figma automation was blocked by the workspace's current usage allowance.
