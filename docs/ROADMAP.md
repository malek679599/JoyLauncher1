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
- [ ] Detect BlueStacks installation candidates.
- [ ] Detect ADB and runtime endpoint without fixed-port assumptions.
- [ ] Add user-configurable fallback paths.
- [ ] Show accurate runtime connection states.
- [ ] List installed packages and normalize app metadata where possible.
- [ ] Launch selected Android apps.
- [ ] Handle disconnected runtime, missing packages, and command timeouts.

## Phase 4 — Library experience
- [x] Build preview app cards, category filters, and sample search.
- [ ] Replace preview data with verified installed-package discovery.
- [ ] Search, sort, pin, favourite, and filter discovered apps.
- [ ] Add recent-app tracking where reliable.
- [ ] Feature Minecraft without restricting the launcher to Minecraft.
- [ ] Add app details and verified launch status.

## Phase 5 — Quality and release
- [x] Add a Windows GitHub Actions Release build workflow.
- [x] Verify one Windows CI Release build succeeded at commit `09e9634`.
- [ ] Confirm the latest search/resource polish and README commits also pass CI.
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

- The app tiles in the current WPF prototype are sample data, not proof of installed apps.
- The Launch Minecraft button intentionally does not launch yet.
- BlueStacks process detection, ADB discovery, package enumeration, and app launching are not implemented.
- Figma Starter currently limits this workspace to three pages and has reached its Figma MCP call allowance for this session. Home and Library screens, Components, and Foundations are preserved; a separate Settings screen remains a later design task.
