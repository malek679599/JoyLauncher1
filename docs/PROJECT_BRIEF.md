# JoyLauncher — Product Brief

## Vision

Create a polished, controller-friendly, fullscreen launcher shell for Windows that makes BlueStacks 5 feel like a living-room console interface while remaining useful as a general Android app hub.

## Target experience

On launch, the user sees a cinematic but uncluttered Home screen. Minecraft receives a prominent featured placement, while recent and pinned apps remain one or two actions away. The same launcher can open entertainment and utility apps rather than behaving like a Minecraft-only frontend.

## Core navigation

1. Home — featured app, recently used apps, pinned apps, runtime status.
2. Library — all discovered Android apps, search, sorting, favourites.
3. Games — game-focused filtered library.
4. Entertainment — video and media apps such as YouTube and YouTube Kids.
5. Search — search installed apps and launcher settings.
6. Store — clearly marked discovery placeholder unless a legitimate supported catalogue source is implemented.
7. Settings — theme, startup behavior, runtime paths, diagnostics, library refresh.

## Product requirements

- Launch installed Android apps through BlueStacks 5.
- Detect BlueStacks installation and ADB availability without assuming a single installation path or port.
- Distinguish unavailable, disconnected, and ready runtime states.
- Discover installed apps where the available BlueStacks/ADB interface permits it.
- Allow pinning/favouriting and persist user preferences locally.
- Provide keyboard-first navigation and visible focus states.
- Provide clear error messages and a diagnostics view.
- Never silently delete user data or modify BlueStacks configuration without explicit approval.
- Keep the launcher responsive if BlueStacks is closed or ADB is unavailable.

## Visual direction

- Fullscreen-first, dark navy foundation.
- Blue accent used for selection and key actions, not decoration everywhere.
- Strong hierarchy: one featured item, compact content rails, clear page titles.
- Consistent icon sizing, spacing, corner radii, and focus treatment.
- Short transitions that can be disabled or reduced.
- High contrast and readable text at typical desktop/TV viewing distances.

## Non-goals

- Building an Android emulator.
- Replacing Windows or BlueStacks.
- Promising compatibility with every BlueStacks version or every Android app.
- Bypassing app licensing, Google Play restrictions, or game protections.
- Bundling copyrighted game art, logos, or media without permission.
- Requiring a paid cloud API or subscription.

## First usable release acceptance criteria

- App opens into a stable Home screen.
- Navigation works with keyboard.
- BlueStacks status is reported accurately.
- Installed app library can be refreshed.
- A selected app can be launched when its package and runtime are available.
- Missing runtime, missing package, and ADB failures produce actionable messages.
- User preferences persist after restart.
- A Windows Release build is produced and tested on the user's machine.
