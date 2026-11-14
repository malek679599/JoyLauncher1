# Website logo sources

JoyLauncher uses publisher website favicons for the known apps below. The app does not copy or bundle third-party logo files into the repository. It requests each site's favicon through Google's S2 favicon endpoint at runtime and keeps the image in memory for the current session.

| Android package | Publisher website used for its favicon |
|---|---|
| `com.mojang.minecraftpe` | https://www.minecraft.net |
| `com.google.android.youtube` | https://www.youtube.com |
| `com.google.android.apps.youtube.kids` | https://www.youtubekids.com |
| `com.google.android.apps.youtube.music` | https://music.youtube.com |
| `com.google.android.videos` | https://tv.google |
| `com.android.chrome` | https://www.google.com/chrome |
| `com.google.android.gm` | https://mail.google.com |
| `com.google.android.apps.maps` | https://maps.google.com |
| `com.google.android.apps.photos` | https://photos.google.com |
| `com.android.vending` | https://play.google.com |
| `com.google.android.googlequicksearchbox` | https://www.google.com |

The favicon endpoint is a third-party convenience service, not a guaranteed publisher API. It may return a small icon, a generic fallback, or no usable image. JoyLauncher therefore keeps an initials-based local fallback and does not make launching dependent on image downloads.

Publisher names and logos remain the property of their respective owners. Their presence only identifies an installed app; it does not imply endorsement by or affiliation with those publishers.
