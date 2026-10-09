# The Megabonk look, and what The Old Gods takes from it

Researched 2026-10-09 for the graphics pass. Megabonk (Vedinad, Unity, 2025) is the closest commercial reference for a 3D horde-survival roguelike. We copy how it reads on screen, not its jokes: The Old Gods keeps a straight tone.

## What the sources say

- **Engine and maker:** a solo developer, Vedinad, in Unity; development began in August 2024 ([Wikipedia](https://en.wikipedia.org/wiki/Megabonk)).
- **Store tags:** "Pixel Graphics", "3D", "Retro" ([Steam page](https://store.steampowered.com/app/3405340/Megabonk/)).
- **Reviewers:** low-poly models, pixelated textures and sprite effects, a 1990s look ([Mezha review](https://mezha.ua/en/reviews/megabonk-review-305493/)); low-poly style that favours clarity and energy over realism; the art style and pixelated UI borrow from Vampire Survivors (Destructoid, via search summary). IGN praised the weapons' bombastic visual presentation and criticised the lack of changing scenery (via Wikipedia).
- No developer post or interview about lighting, shaders or post-processing was found. Everything below about rendering is **inferred from the six Steam screenshots** ([store API](https://store.steampowered.com/api/appdetails?appids=3405340)), not stated by the developer.

## What the screenshots show (inference)

| Area | Megabonk | The Old Gods before this pass |
|---|---|---|
| Atmosphere | Dense fog in one saturated mood colour per map: teal forest, warm sand desert, violet night, red boss arena. Distance melts into it. | Grey or brown fog, weak, mostly unseen. |
| Sky | Gradient sky the fog colour at the horizon, flat cloud silhouettes, far hills fading into haze. | Solid background colour. |
| Ground | Low-res point-filtered textures: chunky pixel noise on grass and sand, so no large area is flat colour. | Big flat-shaded triangles in one colour each. |
| Characters | Low-poly, pixel-textured, with a dark edge that separates them from the ground. | Vertex-coloured, no edge; brown enemies vanish on brown ground. |
| Light | Bright, high ambient, soft shadows, saturated. | Dimmer, desaturated Ash Wood and Coast. |
| Post | Mild bloom on effects; motion blur in one trailer shot. | No bloom or grade. |
| Readability | Enemy hue contrasts the ground (grey mummies on sand, green goblins on blue). Camera sits low behind the player, horizon in view. | Camera pitch 24 degrees; enemies close to ground colour. |
| UI | Pixel font, black boxes with white text, round minimap, thin red health bar under the player. | Clean sans-serif HUD. |

## What we took

1. **Sky dome** (`Runtime/Art/SkyDome.cs`, `Shaders/Sky.shader`): gradient, two rings of far hills fading into haze, flat clouds. Horizon = fog colour.
2. **Biome mood colours**: fog, ambient and sun retuned so each stage has one dominant hue (blue-green steppe, violet-dusk Ash Wood, teal coast). Values are PLACEHOLDER.
3. **Outlines** on enemies, the player, gods and bosses (inverted hull, pixel width, thinner far away).
4. **Pixel-art textures** (`Runtime/Art/PixelTexture.cs`), added at Nick's request: one code-generated, point-filtered detail map with a few tones per channel, projected onto every model without UVs and multiplied into its vertex colours. The colour blocks stay; each gains coarse texels, about 10 cm on the ground and 7 cm on characters.
5. **Bloom, neutral tonemapping, colour grade and vignette**, built in code (`Runtime/Art/SceneLook.cs`).
6. **Lower camera**: the default pitch went from 24 to 15 degrees (Nick, 2026-10-09), so the horizon, far hills and sky are in view as in Megabonk. `-cameraPitch N` lets screenshots try other values.

## What we did not take, and why

- **Pixel font:** the HUD keeps its clean type for now.
- **Meme props, sunglasses, skateboards:** tone.
- **Motion blur:** hurts readability in a crowd.
