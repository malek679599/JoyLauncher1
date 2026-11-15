# JoyLauncher — Visual Design System

## Design principles

1. Console-first: clear focus, large targets, predictable navigation.
2. Cinematic restraint: strong featured artwork, calm surfaces, limited effects.
3. Information hierarchy: featured content first, then recently used and pinned apps.
4. Consistency: shared spacing, typography, icon geometry, and interaction states.
5. Accessibility: visible keyboard focus, readable contrast, reduced-motion option.

## Initial design tokens

These are starting values to be validated in the running app.

### Color

| Token | Value | Purpose |
|---|---|---|
| Background | #080D18 | Main application background |
| Surface | #101A2B | Navigation and content panels |
| Surface Elevated | #17243A | Cards and selected surfaces |
| Accent | #4D8DFF | Primary actions and focus |
| Accent Soft | #19345F | Selected navigation background |
| Text Primary | #F4F7FC | Main text |
| Text Secondary | #A8B5C9 | Supporting text |
| Text Muted | #72819A | Metadata |
| Border | #26364E | Subtle separators |
| Success | #42C98A | Ready/healthy state |
| Warning | #F2B85B | Attention state |
| Danger | #F07878 | Error state |

### Typography

- Use Segoe UI / Segoe UI Variable where available.
- Page title: 28–34 px, semibold.
- Featured title: 30–42 px, bold.
- Section title: 18–22 px, semibold.
- App title: 14–16 px, semibold.
- Metadata: 12–13 px.
- Avoid overly small text on a TV-distance layout.

### Layout and spacing

- Base spacing unit: 4 px.
- Common gaps: 8, 12, 16, 24, 32 px.
- Navigation rail target width: 220–248 px.
- Content horizontal padding: 32–48 px.
- App tile target: approximately 160–210 px wide depending on available space.
- Use consistent rounded corners, starting at 12 px for cards and 8 px for controls.

### Interaction states

Every interactive control needs default, hover, focused, pressed, disabled, and (where relevant) selected states. Keyboard focus must be clearly visible and must not rely on color alone.

### Motion

- Use short transitions, generally 120–200 ms.
- Avoid parallax, camera-like movement, or continuous animation.
- Respect a reduced-motion setting.
- Motion must never delay navigation or app launching.

## Big Picture / Gaming Mode composition

- Full-screen, controller-first layout with a cinematic, app-aware backdrop.
- Right-aligned utility controls for search, settings, ADB connection state, local clock, and profile mark.
- One wide featured app card followed by a horizontal rail of vertical poster cards.
- Backdrop palette and hero identity update when a card receives pointer hover or keyboard/controller focus.
- Centered What's New, Friends, and Recommended tabs; unavailable social data is labelled instead of simulated.
- Footer shows MENU, OPTIONS, A SELECT, and B BACK prompts.
- Cover artwork is generated from original gradients, geometry, and publisher favicon assets; the app does not claim Steam Deck Verified status.

## Home screen composition

- Left: persistent navigation rail.
- Top: page context and runtime status.
- Main hero: Minecraft feature card with title, short description, and Launch action.
- Below hero: Continue / Recent and Pinned apps rails.
- Lower content: Games and Entertainment collections.
- Use original abstract artwork until licensed/user-provided artwork is available.

## Operating-mode boundary

Big Picture Mode is a full-screen Windows WPF shell that launches Android apps through BlueStacks 5 and ADB. It is not SteamOS, Gamescope, KDE Plasma, Proton, a Linux boot environment, or an OS-level suspend/resume implementation. Windows remains the host operating system and BlueStacks remains the Android runtime.

## Responsive behavior

Support 1280×720, 1366×768, 1920×1080, and resizable windowed mode. Content must remain navigable at smaller sizes; rails may scroll horizontally rather than shrinking app tiles beyond readability.


## Big Picture hero artwork

- The hero lazily requests the free Unsplash image by Mansy Graphics only after Big Picture Mode is opened.
- A translucent app-specific gradient tint follows the selected tile's category palette, so hover and keyboard focus continue to change the hero presentation.
- The image is optional. If the network is unavailable or the image cannot be decoded, the bundled WPF gradients, rings and initials remain as the offline fallback.
- The launcher does not download third-party game covers or logos. Verified installed-app logos continue to use the existing website favicon service.
