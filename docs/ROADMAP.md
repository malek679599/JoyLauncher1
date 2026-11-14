# JoyLauncher — Roadmap

## Phase 0 — Foundation
- [x] Create project repository.
- [x] Record product scope and non-goals.
- [x] Record architecture and runtime boundaries.
- [x] Define milestone test strategy.
- [ ] Confirm Windows development environment and free disk space.

## Phase 1 — Windows shell
- [ ] Create .NET 10 WPF solution.
- [ ] Implement fullscreen shell and window controls.
- [ ] Implement navigation rail and keyboard focus.
- [ ] Build Home, Library, Games, Entertainment, Search, and Settings views.
- [ ] Add responsive empty, loading, and error states.
- [ ] Add local theme and settings persistence.

## Phase 2 — Art and design system
- [ ] Define color, typography, spacing, radii, and motion tokens.
- [ ] Create original JoyLauncher logo and app icon.
- [ ] Create reusable app tile, featured hero, navigation item, and status components.
- [ ] Add original placeholder art with clear replacement paths.
- [ ] Verify contrast and readability at common desktop resolutions.

## Phase 3 — BlueStacks integration
- [ ] Detect BlueStacks installation candidates.
- [ ] Detect ADB and runtime endpoint without fixed-port assumptions.
- [ ] Add user-configurable fallback paths.
- [ ] Show runtime connection state.
- [ ] List installed packages and normalize app metadata where possible.
- [ ] Launch selected Android apps.
- [ ] Handle disconnected runtime, missing packages, and command timeouts.

## Phase 4 — Library experience
- [ ] Search, sort, pin, favourite, and filter installed apps.
- [ ] Add recent-app tracking where reliable.
- [ ] Feature Minecraft without restricting the launcher to Minecraft.
- [ ] Add app details and launch status.

## Phase 5 — Quality and release
- [ ] Add automated unit tests for core logic.
- [ ] Run static checks and Release build on Windows.
- [ ] Test against the user's installed BlueStacks 5.
- [ ] Test common keyboard navigation paths.
- [ ] Test missing runtime and recovery paths.
- [ ] Package a versioned Windows build.
- [ ] Publish release notes and known limitations.

## Working rules

- One implementation owner per file/task at a time.
- Review before replacing working code.
- Keep commits small and descriptive.
- Never claim a feature is tested unless the test was actually run.
- Prefer reversible changes and preserve the user's existing BlueStacks setup.
- No paid APIs, subscriptions, or paid assets.
