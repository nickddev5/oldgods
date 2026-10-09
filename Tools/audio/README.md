# Audio notes

All sound in The Old Gods is synthesized at startup by `Assets/OldGods/Runtime/Art/Audio.cs`: simple waves, filtered noise and envelopes. Nothing is downloaded, so there is nothing to license.

## Sounds

| Sound | Made of | When |
|---|---|---|
| Hit | 50 ms filtered noise tick | a weapon damages an enemy (at most every 35 ms) |
| Kill | falling sine thump with a little noise | an enemy dies to the player (at most every 50 ms) |
| Pickup / Gold | rising blip / two-note chime | XP gem or gold collected |
| Level up | three rising notes | the draft opens |
| Hurt / Death | low square pulse / long falling tone | the player takes damage / dies |
| Slam | low thud with noise | boss slams, shockwaves and volleys land |
| Boss wake | two-second low chord swell | a boss rises |
| Portal | rising fifths | the next-stage portal opens |
| Chest, Shrine, Click | chimes and a tick | chests, shrines and UI buttons |

## Music

One 16-second looping drone per biome (menu, Grey Steppe, Ash Wood, Drowned Coast, The Last Test): harmonics over a root note, slow swells, soft wind. Crossfades between stages.

## Replacing a sound

Drop a CC0 or self-recorded WAV into `Assets/OldGods/Audio/`, record it in `ArtSource/SOURCES.md`, and add an override field for that `Sfx` (one small change to `Audio.cs`: load the clip if set, else synthesize). Keep sounds short and quiet; the horde makes many of them.

Volumes (master, music, effects) live in the save's settings and the Settings page.
