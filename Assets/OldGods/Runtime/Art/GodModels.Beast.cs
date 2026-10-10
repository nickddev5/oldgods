using UnityEngine;

namespace OldGods.Runtime
{
    public static partial class GodModels
    {
        /// <summary>The Beast god is taller and heavier than the other gods: a broad chest, thick limbs and a wide shoulder line.</summary>
        static FigureSpec BeastSpec => new FigureSpec
        {
            LegLength = 0.9f, LegThickness = 0.085f, HipWidth = 0.11f,
            TorsoLength = 0.56f, TorsoWidth = 0.25f, TorsoDepth = 0.15f, Hunch = 5f,
            ShoulderWidth = 0.29f, ArmLength = 0.6f, ArmThickness = 0.066f, HandSize = 0.068f,
            HeadSize = 0.13f, NeckLength = 0.05f, Sides = 8,
        };

        /// <summary>
        /// The Beast god (Nick's concept sheet, 2026-10-10): dark reddish-brown skin with pale stripes,
        /// long crimson hair with gold-cuffed braids, glowing red eyes and a spiked gold halo. A bone
        /// beast skull sits on the left shoulder over a cream fur mantle, and the left arm is a crimson
        /// crystal beast arm with long bone claws. Bare chest with a fang totem, a layered belt with a
        /// sun-star buckle, crimson banners front and back, fur and bone tassels at the hips, wrapped
        /// legs with fur-and-bone knee guards and clawed sandals.
        /// </summary>
        static void Beast(Figure f)
        {
            var p = f.P;
            Color fur = p.Metal * 0.96f, bone = p.Metal, stripe = p.Metal * 0.9f;
            // The beast arm is too heavy to bend at the elbow; it swings whole from the shoulder.
            f.RigidLeftArm = true;
            f.Body(p.Cloth2, p.Skin, p.Skin).Face();
            BeastMuscles(f, stripe);
            BeastLegs(f, fur, bone);
            BeastBanners(f, stripe);
            f.Belt(p.Leather, p.Trim, 2, 0.05f);
            BeastBelt(f);
            BeastHipTassels(f, fur, bone);
            BeastRightArm(f, stripe);
            BeastArm(f, bone);
            BeastMantle(f, fur);
            BeastSkull(f, bone);
            BeastTotem(f, bone);
            f.Hair(p.Hair, 1.04f);
            BeastHair(f);
            BeastFace(f, stripe);
            BeastHalo(f);
        }

        /// <summary>A heavy chest, abdomen and shoulder line, with pale stripes painted across the chest.</summary>
        static void BeastMuscles(Figure f, Color stripe)
        {
            var s = f.Spec;
            var k = f.Kit;
            Color skin = f.P.Skin;
            k.Body();
            float front = s.TorsoDepth * 0.85f;
            for (int side = -1; side <= 1; side += 2)
            {
                // Pectorals, traps and two rows of abdominal blocks.
                k.Block(f.Torso(side * 0.1f, s.TorsoLength * 0.72f, front), new Vector3(0.19f, 0.13f, 0.08f), skin, 0.5f, f.TorsoRotation * Quaternion.Euler(0f, side * 12f, side * 4f));
                k.Block(f.Torso(side * 0.14f, s.TorsoLength * 0.95f, -0.01f), new Vector3(0.16f, 0.08f, 0.15f), skin, 0.5f, f.TorsoRotation * Quaternion.Euler(0f, 0f, side * -22f));
                for (int row = 0; row < 3; row++)
                    k.Block(f.Torso(side * 0.048f, s.TorsoLength * (0.5f - row * 0.11f), front - 0.005f), new Vector3(0.08f, 0.065f, 0.05f), skin * 0.97f, 0.5f, f.TorsoRotation);
                // Two chevron stripes on each side of the chest, angled down toward the centre.
                for (int i = 0; i < 2; i++)
                {
                    var at = f.Torso(side * 0.11f, s.TorsoLength * (0.75f - i * 0.07f), front + 0.043f);
                    k.Box(at, new Vector3(0.12f, 0.014f, 0.008f), stripe, 1f, f.TorsoRotation * Quaternion.Euler(0f, side * 12f, side * 18f));
                }
                // Stripes down the flanks.
                for (int i = 0; i < 3; i++)
                {
                    var at = f.Torso(side * s.TorsoWidth * 0.9f, s.TorsoLength * (0.55f - i * 0.08f), s.TorsoDepth * 0.55f);
                    k.Box(at, new Vector3(0.07f, 0.012f, 0.008f), stripe, 1f, f.TorsoRotation * Quaternion.Euler(0f, side * 55f, side * 25f));
                }
            }
        }

        /// <summary>Baggy trousers, leather wraps up the shins, fur cuffs, fur-and-bone knee guards (a gold spike on the right) and clawed sandals.</summary>
        static void BeastLegs(Figure f, Color fur, Color bone)
        {
            var s = f.Spec;
            f.Kit.Body();
            f.Kit.Ball(f.Pelvis + Vector3.up * 0.03f, new Vector3(s.TorsoWidth * 1.04f, s.TorsoLength * 0.22f, s.TorsoDepth * 1.14f), f.P.Cloth2, 12, 6, f.TorsoRotation);
            var p = f.P;
            var k = f.Kit;
            bool smooth = k.Smooth;
            for (int side = 0; side < 2; side++)
            {
                bool left = side == 0;
                f.LegPart(left);
                float sx = left ? -1f : 1f;
                Vector3 knee = left ? f.KneeL : f.KneeR, ankle = left ? f.AnkleL : f.AnkleR;
                Vector3 hip = left ? f.HipL : f.HipR;
                var dir = (knee - ankle).normalized;
                // Baggy trousers over the thigh, bloused at the knee where the wraps begin.
                k.Limb(hip + Vector3.up * 0.05f, knee, s.LegThickness * 2.0f, s.LegThickness * 1.6f, p.Cloth2, 8);
                k.Ball(knee, Vector3.one * s.LegThickness * 1.6f, p.Cloth2, 8, 4);
                k.Ball(Vector3.Lerp(knee, ankle, 0.14f), new Vector3(1.6f, 1.2f, 1.6f) * s.LegThickness, p.Cloth2 * 0.92f, 8, 4);
                var top = Vector3.Lerp(ankle, knee, 0.8f);
                k.Limb(ankle + Vector3.up * 0.02f, top, s.LegThickness * 1.1f, s.LegThickness * 1.4f, p.Leather, 8);
                // The strips wind round the shin, so each band leans a little.
                for (int i = 0; i < 6; i++)
                {
                    var c = Vector3.Lerp(ankle, top, 0.12f + i * 0.155f);
                    var axis = (dir + new Vector3(sx * 0.25f, 0f, (i % 2 == 0 ? 0.2f : -0.2f))).normalized;
                    float r = s.LegThickness * Mathf.Lerp(1.17f, 1.47f, i / 5f);
                    k.Limb(c - axis * 0.013f, c + axis * 0.013f, r, r, p.Leather * (i % 2 == 0 ? 1.35f : 0.8f), 8);
                }
                k.Smooth = false;
                // Fur cuff at the ankle.
                k.Ball(ankle + new Vector3(0f, 0.035f, 0f), new Vector3(0.105f, 0.05f, 0.105f), fur, 9, 4, null, 0.22f, side + 3);
                // Knee guard: a fur pad, a bone plate, and a gold spike on the right knee. It sits just
                // below the knee bend so it turns with the shin in one piece instead of folding.
                k.Ball(knee + new Vector3(0f, -0.075f, 0.07f), new Vector3(0.1f, 0.075f, 0.07f), fur, 8, 4, null, 0.2f, side + 7);
                var plate = knee + new Vector3(0f, -0.085f, 0.12f);
                k.Block(plate, new Vector3(0.11f, 0.12f, 0.045f), bone, 0.5f, Quaternion.Euler(-10f, 0f, 0f));
                if (left) k.Gem(plate + new Vector3(0f, 0f, 0.03f), new Vector3(0.02f, 0.03f, 0.015f), bone * 0.9f);
                else k.Limb(plate + new Vector3(0f, 0.01f, 0.02f), plate + new Vector3(0f, 0.04f, 0.11f), 0.026f, 0f, p.Trim, 5);
                // Sandal sole and straps over the bare foot, with three bone claws at the toes.
                k.Box(new Vector3(ankle.x, 0.012f, ankle.z + 0.075f), new Vector3(0.15f, 0.024f, 0.27f), p.Leather * 0.6f);
                k.Limb(new Vector3(ankle.x, 0.04f, ankle.z + 0.1f) - Vector3.right * 0.08f, new Vector3(ankle.x, 0.04f, ankle.z + 0.1f) + Vector3.right * 0.08f, 0.012f, 0.012f, p.Leather, 4);
                for (int t = -1; t <= 1; t++)
                {
                    var root = new Vector3(ankle.x + t * 0.035f, 0.035f, ankle.z + 0.2f);
                    k.Limb(root, root + new Vector3(t * 0.008f, -0.03f, 0.07f), 0.014f, 0f, bone, 4);
                }
                k.Smooth = smooth;
            }
            k.Body();
        }

        /// <summary>A crimson banner hanging from the belt in front and a longer one behind, each with a pale ring-and-spike sigil, diamonds and a torn hem.</summary>
        static void BeastBanners(Figure f, Color pale)
        {
            var s = f.Spec;
            var k = f.Kit;
            Color red = f.P.Cloth;
            k.Body();
            float rz = s.TorsoDepth * 1.18f + 0.012f;
            for (int face = 1; face >= -1; face -= 2)
            {
                float len = face > 0 ? 0.6f : 0.7f, width = face > 0 ? 0.25f : 0.3f;
                var rot = Quaternion.Euler(face * -5f, 0f, 0f);
                var top = f.Torso(0f, s.TorsoLength * 0.08f, face * rz);
                var c = top + rot * new Vector3(0f, -len * 0.5f, face * 0.01f);
                Vector3 On(float x, float y) => c + rot * new Vector3(x, y, face * 0.011f);
                k.Box(c, new Vector3(width, len, 0.016f), red, 1f, rot);
                // A darker band along the top where it folds over the belt.
                k.Box(c + rot * new Vector3(0f, len * 0.5f - 0.025f, face * 0.004f), new Vector3(width + 0.01f, 0.05f, 0.016f), red * 0.75f, 1f, rot);
                // Torn hem: points of uneven length hanging below the bottom edge.
                float[] drop = { 0.07f, 0.11f, 0.06f, 0.1f, 0.08f };
                for (int i = 0; i < drop.Length; i++)
                {
                    float x = Mathf.Lerp(-width * 0.4f, width * 0.4f, i / (float)(drop.Length - 1));
                    k.Box(c + rot * new Vector3(x, -len * 0.5f - drop[i] * 0.5f + 0.005f, 0f), new Vector3(width / drop.Length, drop[i], 0.014f), red * (i % 2 == 0 ? 1f : 0.9f), 0f, rot * Quaternion.Euler(0f, 0f, 180f));
                }
                // The sigil: a ring with four spikes and a diamond at its heart, then three diamonds below.
                var sig = On(0f, len * 0.22f);
                const int n = 10;
                float ring = width * 0.24f;
                for (int i = 0; i < n; i++)
                {
                    float a0 = i * Mathf.PI * 2f / n, a1 = (i + 1) * Mathf.PI * 2f / n;
                    k.Limb(sig + rot * new Vector3(Mathf.Cos(a0), Mathf.Sin(a0), 0f) * ring, sig + rot * new Vector3(Mathf.Cos(a1), Mathf.Sin(a1), 0f) * ring, 0.007f, 0.007f, pale, 4);
                }
                for (int i = 0; i < 4; i++)
                {
                    float a = i * Mathf.PI * 0.5f;
                    var d = rot * new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
                    k.Limb(sig + d * (ring + 0.005f), sig + d * (ring + (i == 1 ? 0.05f : 0.035f)), 0.011f, 0f, pale, 4);
                }
                k.Gem(sig, new Vector3(0.018f, 0.028f, 0.008f), pale);
                foreach (float y in new[] { -0.02f, -0.1f, -0.17f })
                    k.Gem(On(0f, y), new Vector3(0.016f, 0.024f, 0.007f), pale * (y < -0.15f ? 0.9f : 1f));
            }
        }

        /// <summary>A second, wider belt band under the first and a large gold sun-star buckle.</summary>
        static void BeastBelt(Figure f)
        {
            var s = f.Spec;
            var p = f.P;
            var k = f.Kit;
            k.Body();
            var waist = f.Torso(0f, s.TorsoLength * 0.12f, 0f);
            float rx = s.TorsoWidth * 1.12f, rz = s.TorsoDepth * 1.24f;
            k.Lathe(waist - f.TorsoRotation * Vector3.up * 0.075f, new[] { new Vector2(0f, rx), new Vector2(0.05f, rx * 0.99f) }, 16, p.Leather * 1.3f, rz / rx, f.TorsoRotation);
            var front = waist + f.TorsoRotation * new Vector3(0f, -0.02f, rz + 0.02f);
            var faceOut = f.TorsoRotation * Quaternion.Euler(90f, 0f, 0f);
            k.Lathe(front, new[] { new Vector2(0f, 0.065f), new Vector2(0.018f, 0.06f), new Vector2(0.026f, 0.035f) }, 8, p.Trim, 1f, faceOut);
            k.Gem(front + f.TorsoRotation * Vector3.forward * 0.03f, new Vector3(0.022f, 0.022f, 0.014f), p.Trim * 0.8f);
            // Sun rays: long and short in turn.
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI / 4f;
                var d = f.TorsoRotation * new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
                var root = front + d * 0.06f + f.TorsoRotation * Vector3.forward * 0.008f;
                k.Limb(root, root + d * (i % 2 == 0 ? 0.065f : 0.038f), 0.017f, 0f, p.Trim, 4);
            }
        }

        /// <summary>Ragged fur tassels and bone shards on cords hanging round the hips, between the banners.</summary>
        static void BeastHipTassels(Figure f, Color fur, Color bone)
        {
            var s = f.Spec;
            var k = f.Kit;
            bool smooth = k.Smooth;
            k.Smooth = false;
            k.Body();
            float rx = s.TorsoWidth * 1.14f, rz = s.TorsoDepth * 1.26f;
            for (int side = -1; side <= 1; side += 2)
                for (int i = 0; i < 7; i++)
                {
                    // Round each side from just off the front banner to just off the back one.
                    float a = Mathf.Lerp(35f, 150f, i / 6f) * Mathf.Deg2Rad;
                    var outward = new Vector3(side * Mathf.Sin(a), 0f, Mathf.Cos(a));
                    var root = f.Torso(side * Mathf.Sin(a) * rx, s.TorsoLength * 0.04f, Mathf.Cos(a) * rz);
                    float drop = 0.17f + (i % 3) * 0.04f;
                    k.Limb(root, root + outward * 0.05f + Vector3.down * drop, 0.04f, 0f, fur * (i % 2 == 0 ? 1f : 0.9f), 5);
                    if (i % 2 == 1)
                    {
                        // A bone shard on a cord, with a gold bead.
                        var cord = root + outward * 0.035f;
                        k.Limb(cord, cord + Vector3.down * 0.06f, 0.005f, 0.005f, f.P.Leather, 4);
                        k.Gem(cord + Vector3.down * 0.065f, new Vector3(0.012f, 0.012f, 0.012f), f.P.Trim);
                        k.Limb(cord + Vector3.down * 0.07f, cord + outward * 0.02f + Vector3.down * 0.19f, 0.014f, 0f, bone, 4);
                    }
                }
            k.Smooth = smooth;
        }

        /// <summary>The human right arm: a heavy bicep with pale stripe bands and a gold armband, a wrapped forearm and a dark leather bracer.</summary>
        static void BeastRightArm(Figure f, Color stripe)
        {
            var s = f.Spec;
            var p = f.P;
            var k = f.Kit;
            f.ArmPart(false);
            Vector3 sh = f.ShoulderR, el = f.ElbowR, wr = f.WristR;
            var up = (el - sh).normalized;
            var fore = (wr - el).normalized;
            k.Ball(Vector3.Lerp(sh, el, 0.45f) + new Vector3(0.008f, 0f, 0.012f), new Vector3(s.ArmThickness * 1.3f, s.ArmLength * 0.2f, s.ArmThickness * 1.32f), p.Skin, 8, 5);
            foreach (float t in new[] { 0.25f, 0.33f })
            {
                var c = Vector3.Lerp(sh, el, t);
                k.Limb(c - up * 0.007f, c + up * 0.007f, s.ArmThickness * 1.37f, s.ArmThickness * 1.37f, stripe, 8);
            }
            var band = Vector3.Lerp(sh, el, 0.72f);
            k.Limb(band - up * 0.014f, band + up * 0.014f, s.ArmThickness * 1.22f, s.ArmThickness * 1.2f, p.Trim, 8);
            // Cloth wraps from the elbow, then the bracer to the wrist, laced with lighter straps.
            k.Limb(el + fore * 0.02f, Vector3.Lerp(el, wr, 0.4f), s.ArmThickness * 1.0f, s.ArmThickness * 0.98f, p.Metal * 0.62f, 8);
            for (int i = 0; i < 3; i++)
            {
                var c = Vector3.Lerp(el, wr, 0.08f + i * 0.11f);
                k.Limb(c - fore * 0.008f, c + fore * 0.008f, s.ArmThickness * 1.04f, s.ArmThickness * 1.04f, p.Metal * 0.5f, 8);
            }
            k.Limb(Vector3.Lerp(el, wr, 0.4f), wr + fore * 0.012f, s.ArmThickness * 1.12f, s.ArmThickness * 1.2f, p.Leather, 8);
            foreach (float t in new[] { 0.52f, 0.7f, 0.88f })
            {
                var c = Vector3.Lerp(el, wr, t);
                k.Limb(c - fore * 0.006f, c + fore * 0.006f, s.ArmThickness * 1.2f, s.ArmThickness * 1.24f, p.Leather * 1.6f, 8);
            }
            k.Body();
        }

        /// <summary>
        /// The left arm, a beast's: a crimson core covered in faceted crystal plates, spikes along
        /// the forearm, and an oversized hand with long bone claws that reach the knee.
        /// </summary>
        static void BeastArm(Figure f, Color bone)
        {
            var p = f.P;
            var k = f.Kit;
            bool smooth = k.Smooth;
            k.Smooth = false;
            f.ArmPart(true);
            Vector3 sh = f.ShoulderL, el = f.ElbowL, wr = f.WristL;
            Color dark = p.Cloth * 0.62f, mid = p.Cloth * 0.95f, light = p.Cloth * 1.3f;
            var up = (el - sh).normalized;
            var fore = (wr - el).normalized;
            k.Limb(sh + Vector3.up * 0.02f, el, 0.105f, 0.085f, dark, 7);
            k.Ball(el, new Vector3(0.09f, 0.09f, 0.09f), dark, 7, 4);
            k.Limb(el, wr, 0.095f, 0.115f, dark, 7);

            // Crystal plates: chamfered slabs set at uneven angles over the outside and front of the arm.
            var plates = new (float t, float around, float size, float tilt)[]
            {
                (0.1f, -0.4f, 1.2f, 18f), (0.3f, 0.5f, 1f, -14f), (0.45f, -0.2f, 1.1f, 24f), (0.62f, 0.9f, 0.9f, -20f), (0.8f, 0.1f, 1f, 12f),
            };
            for (int i = 0; i < plates.Length; i++)
            {
                var pl = plates[i];
                var dirOut = new Vector3(-Mathf.Cos(pl.around), 0f, Mathf.Sin(pl.around));
                var at = Vector3.Lerp(sh, el, pl.t) + dirOut * 0.07f;
                var rot = Quaternion.LookRotation(dirOut, up) * Quaternion.Euler(pl.tilt, pl.tilt * 0.7f, i * 23f);
                k.Block(at, new Vector3(0.12f, 0.14f, 0.07f) * pl.size, i % 2 == 0 ? mid : light, 0.25f, rot);
            }
            var forePlates = new (float t, float around, float size, float tilt)[]
            {
                (0.08f, 0.3f, 1.05f, -16f), (0.25f, -0.5f, 1.15f, 20f), (0.4f, 0.8f, 1f, -10f), (0.55f, -0.1f, 1.25f, 15f), (0.72f, 0.6f, 1.1f, -22f), (0.88f, -0.6f, 1.2f, 10f),
                (0.35f, 2.2f, 0.9f, 14f), (0.7f, 2.6f, 0.95f, -12f),
            };
            for (int i = 0; i < forePlates.Length; i++)
            {
                var pl = forePlates[i];
                var dirOut = new Vector3(-Mathf.Cos(pl.around), 0f, Mathf.Sin(pl.around));
                var at = Vector3.Lerp(el, wr, pl.t) + dirOut * Mathf.Lerp(0.08f, 0.1f, pl.t);
                var rot = Quaternion.LookRotation(dirOut, fore) * Quaternion.Euler(pl.tilt, -pl.tilt * 0.6f, i * 31f);
                k.Block(at, new Vector3(0.13f, 0.15f, 0.075f) * pl.size, i % 3 == 1 ? light : mid, 0.25f, rot);
            }
            // Spikes along the outside of the forearm and one off the elbow.
            for (int i = 0; i < 3; i++)
            {
                var root = Vector3.Lerp(el, wr, 0.25f + i * 0.25f) + new Vector3(-0.09f, 0f, -0.03f);
                k.Limb(root, root + new Vector3(-0.09f + i * 0.015f, 0.05f, -0.05f), 0.032f, 0f, light, 4);
            }
            k.Limb(el + new Vector3(-0.03f, 0f, -0.06f), el + new Vector3(-0.07f, 0.05f, -0.17f), 0.04f, 0f, bone, 5);

            // The hand: a broad palm, knuckle plates, four fingers ending in long bone claws, and a thumb claw.
            var palm = wr + fore * 0.07f + new Vector3(-0.005f, 0f, 0.015f);
            var handRot = Quaternion.LookRotation(Vector3.forward, fore);
            k.Block(palm, new Vector3(0.14f, 0.17f, 0.19f), dark, 0.35f, handRot);
            for (int i = 0; i < 4; i++)
            {
                float z = Mathf.Lerp(-0.07f, 0.075f, i / 3f);
                var knuckle = palm + fore * 0.075f + new Vector3(-0.01f, 0f, z);
                k.Block(knuckle + new Vector3(-0.035f, 0f, 0f), new Vector3(0.05f, 0.04f, 0.04f), light, 0.5f, handRot);
                var mid1 = knuckle + fore * 0.06f + new Vector3(0.012f, 0f, z * 0.15f);
                k.Limb(knuckle, mid1, 0.026f, 0.022f, mid, 5);
                // Claws curve inward toward the palm.
                float len = i == 1 || i == 2 ? 0.27f : 0.22f;
                var tip = mid1 + fore * len * 0.85f + new Vector3(0.05f, 0f, z * 0.3f + 0.02f);
                var bend = Vector3.Lerp(mid1, tip, 0.45f) + new Vector3(-0.012f, 0f, 0f);
                k.Limb(mid1, bend, 0.027f, 0.018f, bone, 5);
                k.Limb(bend, tip, 0.018f, 0f, bone, 5);
            }
            var thumb = palm + new Vector3(0.045f, 0.01f, 0.07f);
            k.Limb(thumb, thumb + fore * 0.06f + new Vector3(0.03f, 0f, 0.05f), 0.024f, 0.018f, mid, 5);
            k.Limb(thumb + fore * 0.06f + new Vector3(0.03f, 0f, 0.05f), thumb + fore * 0.15f + new Vector3(0.06f, 0f, 0.07f), 0.018f, 0f, bone, 5);
            k.Smooth = smooth;
            k.Body();
        }

        /// <summary>A cream fur mantle: a heavy mass across the back and over both shoulders, ragged tufts at its edges, and a low collar at the front so the chest stays bare.</summary>
        static void BeastMantle(Figure f, Color fur)
        {
            var s = f.Spec;
            var k = f.Kit;
            bool smooth = k.Smooth;
            k.Smooth = false;
            k.Body();
            var back = f.Torso(0f, s.TorsoLength * 0.8f, -s.TorsoDepth * 1.05f);
            k.Ball(back, new Vector3(0.38f, 0.19f, 0.13f), fur, 10, 5, f.TorsoRotation, 0.12f, 11);
            k.Ball(f.Torso(0f, s.TorsoLength * 0.98f, -0.02f), new Vector3(0.24f, 0.07f, 0.17f), fur, 10, 4, f.TorsoRotation, 0.15f, 12);
            for (int side = -1; side <= 1; side += 2)
            {
                var sh = side < 0 ? f.ShoulderL : f.ShoulderR;
                k.Ball(sh + new Vector3(side * 0.02f, 0.05f, -0.02f), new Vector3(0.17f, 0.11f, 0.17f), fur * 0.97f, 9, 4, null, 0.2f, 13 + side);
            }
            // Ragged tufts hanging from the bottom edge of the mantle down the back, and off the shoulders.
            for (int i = 0; i < 11; i++)
            {
                float x = Mathf.Lerp(-0.4f, 0.4f, i / 10f);
                float edge = 1f - Mathf.Pow(x / 0.4f, 2f) * 0.4f;
                var root = back + f.TorsoRotation * new Vector3(x, -0.13f * edge, -0.03f);
                float drop = 0.1f + (i % 3) * 0.05f + (i == 5 ? 0.05f : 0f);
                k.Limb(root, root + new Vector3(x * 0.15f, -drop, -0.03f), 0.05f, 0f, fur * (i % 2 == 0 ? 1f : 0.9f), 5);
            }
            for (int side = -1; side <= 1; side += 2)
            {
                var sh = side < 0 ? f.ShoulderL : f.ShoulderR;
                for (int i = 0; i < 3; i++)
                {
                    var root = sh + new Vector3(side * 0.1f, -0.03f, (i - 1) * 0.08f);
                    k.Limb(root, root + new Vector3(side * 0.06f, -0.1f - (i % 2) * 0.04f, (i - 1) * 0.02f), 0.04f, 0f, fur * (i % 2 == 0 ? 0.92f : 1f), 5);
                }
            }
            k.Smooth = smooth;
        }

        /// <summary>A big beast skull worn on the left shoulder, snout out and forward, with a glowing gold eye, fangs, an open jaw and swept-back horns.</summary>
        static void BeastSkull(Figure f, Color bone)
        {
            var p = f.P;
            var k = f.Kit;
            bool smooth = k.Smooth;
            k.Smooth = false;
            k.Body(); // rests on the mantle, steady while the arm swings
            var rot = Quaternion.LookRotation(new Vector3(-0.8f, -0.1f, 0.6f).normalized, Vector3.up);
            var s = f.ShoulderL + new Vector3(-0.07f, 0.12f, 0.01f);
            Vector3 At(float x, float y, float z) => s + rot * new Vector3(x, y, z);
            Color socket = p.Leather * 0.35f;
            k.Block(At(0f, 0.02f, -0.04f), new Vector3(0.24f, 0.18f, 0.26f), bone, 0.5f, rot);
            k.Block(At(0f, 0.09f, 0.07f), new Vector3(0.26f, 0.05f, 0.09f), bone * 0.95f, 0.5f, rot * Quaternion.Euler(-10f, 0f, 0f));
            k.Block(At(0f, -0.005f, 0.16f), new Vector3(0.14f, 0.11f, 0.2f), bone, 0.4f, rot * Quaternion.Euler(8f, 0f, 0f));
            k.Block(At(0f, -0.02f, 0.27f), new Vector3(0.11f, 0.08f, 0.06f), bone * 0.95f, 0.5f, rot);
            k.Box(At(0f, 0.005f, 0.3f), new Vector3(0.05f, 0.03f, 0.02f), socket, 1f, rot);
            for (int side = -1; side <= 1; side += 2)
            {
                k.Box(At(side * 0.08f, 0.045f, 0.105f), new Vector3(0.065f, 0.05f, 0.04f), socket, 1f, rot * Quaternion.Euler(0f, side * 20f, side * 12f));
                k.Gem(At(side * 0.08f, 0.045f, 0.125f), new Vector3(0.022f, 0.016f, 0.022f), p.Glow);
                // Upper fangs, lower fangs and a horn sweeping back.
                k.Limb(At(side * 0.05f, -0.05f, 0.22f), At(side * 0.046f, -0.15f, 0.235f), 0.02f, 0f, bone, 4);
                k.Limb(At(side * 0.03f, -0.05f, 0.27f), At(side * 0.028f, -0.1f, 0.275f), 0.011f, 0f, bone, 4);
                k.Limb(At(side * 0.045f, -0.105f, 0.17f), At(side * 0.045f, -0.04f, 0.19f), 0.013f, 0f, bone, 4);
                k.Limb(At(side * 0.1f, 0.08f, -0.08f), At(side * 0.15f, 0.17f, -0.2f), 0.035f, 0.02f, bone * 0.95f, 5);
                k.Limb(At(side * 0.15f, 0.17f, -0.2f), At(side * 0.15f, 0.15f, -0.31f), 0.02f, 0f, bone * 0.95f, 5);
            }
            // The open lower jaw.
            k.Block(At(0f, -0.12f, 0.1f), new Vector3(0.13f, 0.04f, 0.2f), bone * 0.88f, 0.5f, rot * Quaternion.Euler(-14f, 0f, 0f));
            k.Smooth = smooth;
            k.Body();
        }

        /// <summary>A leather cord with bone fangs either side and a gold totem: a pointed ring with a long gold fang hanging from it.</summary>
        static void BeastTotem(Figure f, Color bone)
        {
            var s = f.Spec;
            var p = f.P;
            var k = f.Kit;
            k.Body();
            var q = f.TorsoRotation;
            var cl = f.Torso(-0.1f, s.TorsoLength * 0.98f, s.TorsoDepth * 0.7f);
            var cr = f.Torso(0.1f, s.TorsoLength * 0.98f, s.TorsoDepth * 0.7f);
            var ring = f.Torso(0f, s.TorsoLength * 0.68f, s.TorsoDepth * 0.85f + 0.06f);
            // The cord, sagging from the neck to the ring, with fangs hung along it.
            Vector3 Cord(float t, bool left)
            {
                var a = left ? cl : cr;
                var b = ring + q * new Vector3(left ? -0.03f : 0.03f, 0.055f, 0f);
                return Vector3.Lerp(a, b, t) + q * new Vector3(0f, 0f, Mathf.Sin(t * Mathf.PI) * 0.04f);
            }
            for (int side = 0; side < 2; side++)
            {
                bool left = side == 0;
                Vector3 prev = Cord(0f, left);
                for (int i = 1; i <= 5; i++)
                {
                    var pt = Cord(i / 5f, left);
                    k.Limb(prev, pt, 0.006f, 0.006f, p.Leather * 0.8f, 4);
                    prev = pt;
                    if (i >= 2 && i <= 4)
                        k.Limb(pt, pt + q * new Vector3(0f, -0.045f - (i == 3 ? 0.015f : 0f), 0.006f), 0.01f, 0f, i == 3 ? p.Trim : bone, 4);
                }
            }
            const int n = 10;
            for (int i = 0; i < n; i++)
            {
                float a0 = i * Mathf.PI * 2f / n, a1 = (i + 1) * Mathf.PI * 2f / n;
                k.Limb(ring + q * new Vector3(Mathf.Cos(a0), Mathf.Sin(a0), 0f) * 0.05f, ring + q * new Vector3(Mathf.Cos(a1), Mathf.Sin(a1), 0f) * 0.05f, 0.011f, 0.011f, p.Trim, 4);
            }
            for (int i = 0; i < 6; i++)
            {
                float a = i * Mathf.PI / 3f + Mathf.PI / 6f;
                var d = q * new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
                k.Limb(ring + d * 0.058f, ring + d * 0.085f, 0.012f, 0f, p.Trim, 4);
            }
            k.Gem(ring, new Vector3(0.016f, 0.022f, 0.012f), p.Glow);
            var drop = ring + q * new Vector3(0f, -0.05f, 0.004f);
            k.Limb(drop, drop + q * new Vector3(0f, -0.17f, 0.025f), 0.022f, 0f, p.Trim, 5);
            k.Limb(drop + q * new Vector3(-0.035f, 0.01f, 0f), drop + q * new Vector3(-0.05f, -0.08f, 0.01f), 0.012f, 0f, bone, 4);
            k.Limb(drop + q * new Vector3(0.035f, 0.01f, 0f), drop + q * new Vector3(0.05f, -0.08f, 0.01f), 0.012f, 0f, bone, 4);
        }

        /// <summary>Long crimson hair in chunky locks down the back and over the shoulders, and two front braids with gold cuffs and beads.</summary>
        static void BeastHair(Figure f)
        {
            float h = f.Spec.HeadSize;
            var p = f.P;
            var k = f.Kit;
            bool smooth = k.Smooth;
            k.Smooth = false;
            k.Body();
            // Locks down the back, longest in the middle, over the fur mantle.
            for (int i = -3; i <= 3; i++)
            {
                var root = f.Head + new Vector3(i * h * 0.3f, -h * 0.1f, -h * 0.88f);
                // Out over the collar of fur first, then straight down the back.
                var mid = root + new Vector3(i * 0.03f, -0.18f, -0.28f + Mathf.Abs(i) * 0.02f);
                var tip = mid + new Vector3(i * 0.015f, -0.55f + Mathf.Abs(i) * 0.06f, -0.03f);
                var c = p.Hair * (i % 2 == 0 ? 1f : 0.86f);
                k.Limb(root, mid, 0.05f, 0.05f, c, 6);
                k.Limb(mid, tip, 0.05f, 0f, c, 6);
            }
            // Locks falling over the front of each shoulder.
            for (int side = -1; side <= 1; side += 2)
                for (int i = 0; i < 2; i++)
                {
                    var root = f.Head + new Vector3(side * h * (0.85f + i * 0.1f), -h * 0.1f, -h * (0.1f + i * 0.35f));
                    var mid = root + new Vector3(side * 0.05f, -0.18f, 0.03f);
                    var tip = mid + new Vector3(side * 0.02f, -0.24f, 0.03f - i * 0.04f);
                    var c = p.Hair * (i == 0 ? 0.92f : 1f);
                    k.Limb(root, mid, 0.04f, 0.035f, c, 6);
                    k.Limb(mid, tip, 0.035f, 0f, c, 6);
                }
            // Two braids at the front, falling to the chest, each with two gold cuffs and a bead.
            for (int side = -1; side <= 1; side += 2)
            {
                var top = f.Head + new Vector3(side * h * 0.72f, -h * 0.35f, h * 0.45f);
                var end = top + new Vector3(side * 0.025f, -0.3f, 0.05f);
                for (int i = 0; i < 6; i++)
                {
                    var c = Vector3.Lerp(top, end, i / 5f);
                    k.Gem(c, new Vector3(0.022f, 0.03f, 0.022f) * (1f - i * 0.05f), p.Hair * (i % 2 == 0 ? 1f : 0.85f));
                }
                foreach (float t in new[] { 0.35f, 0.75f })
                {
                    var c = Vector3.Lerp(top, end, t);
                    var dir = (end - top).normalized;
                    k.Limb(c - dir * 0.012f, c + dir * 0.012f, 0.025f, 0.025f, p.Trim, 6);
                }
                k.Gem(end + Vector3.down * 0.035f, new Vector3(0.018f, 0.022f, 0.018f), p.Trim);
            }
            k.Smooth = smooth;
        }

        /// <summary>Pale stripes painted on the face, two under each eye.</summary>
        static void BeastFace(Figure f, Color stripe)
        {
            float h = f.Spec.HeadSize;
            var k = f.Kit;
            k.Part(BodyPart.Head, f.NeckBase);
            var face = f.Head + new Vector3(0f, 0f, h * 0.88f);
            for (int side = -1; side <= 1; side += 2)
                for (int i = 0; i < 2; i++)
                    k.Box(face + new Vector3(side * h * 0.42f, -h * (0.14f + i * 0.13f), -h * 0.04f), new Vector3(h * 0.32f, h * 0.06f, h * 0.06f), stripe, 1f, Quaternion.Euler(0f, side * 18f, side * -14f));
            k.Body();
        }

        /// <summary>A gold halo crown behind the head: a heavy ring with a tall spike at the top, points either side and small ones below, and a stone under the top spike.</summary>
        static void BeastHalo(Figure f)
        {
            float h = f.Spec.HeadSize;
            var p = f.P;
            var k = f.Kit;
            bool smooth = k.Smooth;
            k.Smooth = false;
            k.Part(BodyPart.Head, f.NeckBase);
            var c = f.Head + new Vector3(0f, h * 0.6f, -h * 1.2f);
            var tilt = Quaternion.Euler(-12f, 0f, 0f);
            const float r = 0.2f;
            const int n = 16;
            for (int i = 0; i < n; i++)
            {
                float a0 = i * Mathf.PI * 2f / n, a1 = (i + 1) * Mathf.PI * 2f / n;
                k.Limb(c + tilt * new Vector3(Mathf.Cos(a0), Mathf.Sin(a0), 0f) * r, c + tilt * new Vector3(Mathf.Cos(a1), Mathf.Sin(a1), 0f) * r, 0.022f, 0.022f, p.Trim, 5);
            }
            // As on the sheet: the tall top spike, points up and out either side, flat side points and small ones below.
            var spikes = new (float deg, float len, float width)[] { (90f, 0.24f, 0.045f), (45f, 0.08f, 0.026f), (135f, 0.08f, 0.026f), (0f, 0.1f, 0.03f), (180f, 0.1f, 0.03f), (-40f, 0.04f, 0.018f), (220f, 0.04f, 0.018f) };
            foreach (var sp in spikes)
            {
                float a = sp.deg * Mathf.Deg2Rad;
                var d = tilt * new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
                k.Limb(c + d * (r - 0.01f), c + d * (r + sp.len), sp.width, 0f, p.Trim, 4);
            }
            k.Gem(c + tilt * new Vector3(0f, r * 0.9f, 0.02f), new Vector3(0.02f, 0.028f, 0.014f), p.Glow);
            k.Smooth = smooth;
            k.Body();
        }
    }
}
