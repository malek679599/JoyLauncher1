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
- [ ] Add full keyboard/gamepad focus navigation.
- [ ] Add fullscreen startup and display-size refinements.
- [ ] Add local theme and settings persistence.

## Phase 2 — Art and design system
- [x] Define the initial dark navy/blue color, spacing, radius, and typography tokens.
- [x] Create reusable app tile, action button, and navigation item components in Figma.
- [x] Create editable Home and Library screen layouts in Figma.
- [x] Create original abstract Minecraft-feature artwork placeholder from editable shapes.
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
- [ ] Add user-configurable fallback paths.
- [ ] Test discovery and ADB connection against the user's BlueStacks installation.
- [ ] Resolve friendly app names, icons, and category metadata.

## Phase 4 — Library experience
- [x] Build preview app cards, category filters, and sample search.
- [x] Populate the Library with package identifiers after a successful ADB package query.
- [x] Allow launch requests only for verified package identifiers.
- [ ] Resolve friendly app names and icons.
- [ ] Search, sort, pin, favourite, and filter discovered apps.
- [ ] Add recent-app tracking where reliable.
- [ ] Match Minecraft to the correct verified package without relying on an unverified guess.
- [ ] Add app details and runtime launch feedback.

## Phase 5 — Quality and release
- [x] Add a Windows GitHub Actions Release build workflow.
- [x] Verify Windows CI Release builds succeeded through commit `d4e9aa2` (run #19).
- [ ] Confirm later timeout-cleanup and documentation commits also pass CI.
- [ ] Run a Release build on the user's Windows machine.
- [ ] Test against the user's installed BlueStacks 5.
- [ ] Test keyboard navigation and failure recovery.
- [ ] Add automated tests for runtime discovery and app metadata.
- [ ] Package a versioned Windows build.
- [ ] Publish release notes and known limitations.

## Working rules

- One implementation owner per file/task at a time.
- Review before replacing working code.
- Keep commits small and descriptive.
- Never claim a feature is tested unless the test was actually run.
- Prefer reversible changes and preserve the user's existing BlueStacks setup.
- No paid APIs, subscriptions, or paid assets.

## Current boundaries

- Home sample tiles are demonstration data; the Library switches to verified package identifiers only after a successful ADB query.
- The featured Minecraft Home button remains a placeholder until Minecraft is matched to a verified package.
- BlueStacks discovery and ADB package/launch code are implemented but still require local BlueStacks validation.
- Figma Starter currently limits this workspace to three pages and has reached its Figma MCP call allowance for this session. Home and Library screens, Components, and Foundations are preserved; a separate Settings screen remains a later design task.
