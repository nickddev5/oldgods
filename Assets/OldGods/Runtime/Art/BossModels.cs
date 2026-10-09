using System.Collections.Generic;
using UnityEngine;

namespace OldGods.Runtime
{
    /// <summary>
    /// Boss bodies, about 1.7 to 2 m tall before the boss's scale (3 to 3.6x in game), facing +z,
    /// dressed to the same detail as the gods and coloured in the mesh. Legs and arms are tagged so
    /// the shader can walk them.
    /// </summary>
    public static class BossModels
    {
        static readonly Dictionary<BossModel, Mesh> cache = new Dictionary<BossModel, Mesh>();

        public static Mesh Get(BossModel model)
        {
            if (cache.TryGetValue(model, out var m) && m != null) return m;
            var k = new MeshKit();
            switch (model)
            {
                case BossModel.Warden: Warden(k); break;
                case BossModel.Stag: Stag(k); break;
                case BossModel.Mother: Mother(k); break;
                default: Construct(k); break;
            }
            m = k.Build("Boss" + model);
            cache[model] = m;
            return m;
        }

        static Color Grey(float v) => new Color(v, v, v);

        // ---- The Stone Warden ----

        static Palette WardenPalette => new Palette
        {
            Skin = new Color(0.62f, 0.6f, 0.55f), SkinShade = new Color(0.46f, 0.45f, 0.42f),
            Cloth = new Color(0.36f, 0.5f, 0.26f), Cloth2 = new Color(0.52f, 0.6f, 0.34f),
            Leather = new Color(0.5f, 0.4f, 0.27f), Metal = new Color(0.38f, 0.37f, 0.36f), Trim = new Color(0.74f, 0.7f, 0.62f),
            Hair = new Color(0.32f, 0.46f, 0.24f), Eye = new Color(1f, 0.78f, 0.36f), Pupil = new Color(0.1f, 0.09f, 0.08f), Glow = new Color(1f, 0.72f, 0.3f),
        };

        /// <summary>
        /// A golem of faceted stone: a moss mantle and a rope belt hung with charms, glowing runes cut
        /// into its chest, standing stones jutting from its back, and a stone maul.
        /// </summary>
        static void Warden(MeshKit k)
        {
            var p = WardenPalette;
            var s = new FigureSpec
            {
                LegLength = 0.6f, LegThickness = 0.13f, HipWidth = 0.2f,
                TorsoLength = 0.72f, TorsoWidth = 0.4f, TorsoDepth = 0.28f, Hunch = 14f,
                ShoulderWidth = 0.46f, ArmLength = 0.95f, ArmThickness = 0.13f, HandSize = 0.15f,
                HeadSize = 0.14f, NeckLength = 0.01f, Sides = 7,
            };
            // Stone is faceted; moss and rope are smooth.
            k.Smooth = false;
            var f = new Figure(k, s, p).Layout().Body();
            f.Boots(p.SkinShade, 0.22f, p.Metal).Pauldrons(p.Skin, 0.24f, p.SkinShade);
            // Cracked plates over the chest and belly.
            k.Body();
            k.Block(f.Torso(0f, s.TorsoLength * 0.66f, s.TorsoDepth * 0.72f), new Vector3(s.TorsoWidth * 1.6f, s.TorsoLength * 0.42f, 0.16f), p.Trim, 0.25f, f.TorsoRotation);
            k.Block(f.Torso(0f, s.TorsoLength * 0.3f, s.TorsoDepth * 0.7f), new Vector3(s.TorsoWidth * 1.2f, s.TorsoLength * 0.24f, 0.14f), p.Skin, 0.3f, f.TorsoRotation);
            // Runes cut into the plate.
            var face = f.Torso(0f, s.TorsoLength * 0.66f, s.TorsoDepth * 0.72f + 0.085f);
            for (int i = -1; i <= 1; i++)
            {
                k.Box(face + f.TorsoRotation * new Vector3(i * 0.16f, 0f, 0f), new Vector3(0.035f, 0.2f, 0.02f), p.Glow, 1f, f.TorsoRotation);
                k.Box(face + f.TorsoRotation * new Vector3(i * 0.16f + 0.03f, 0.06f * i, 0f), new Vector3(0.08f, 0.03f, 0.02f), p.Glow, 1f, f.TorsoRotation * Quaternion.Euler(0f, 0f, 35f * i));
            }
            // Standing stones jutting from the back.
            for (int i = 0; i < 3; i++)
            {
                float x = (i - 1) * 0.22f;
                var root = f.Torso(x, s.TorsoLength * (0.75f - Mathf.Abs(i - 1) * 0.15f), -s.TorsoDepth * 0.8f);
                k.Block(root + new Vector3(0f, 0.2f, -0.06f), new Vector3(0.13f, 0.5f - Mathf.Abs(i - 1) * 0.12f, 0.1f), i == 1 ? p.Trim : p.Skin, 0.35f, Quaternion.Euler(-28f, 0f, (1 - i) * 18f));
            }
            // Head: a heavy brow over a burning slit, set low between the shoulders.
            k.Part(BodyPart.Head, f.NeckBase);
            var h = f.Head;
            k.Block(h + new Vector3(0f, 0.07f, 0.03f), new Vector3(0.3f, 0.1f, 0.27f), p.SkinShade, 0.3f);
            k.Box(h + new Vector3(0f, 0.0f, 0.13f), new Vector3(0.2f, 0.035f, 0.02f), p.Glow);
            k.Block(h + new Vector3(0f, -0.08f, 0.06f), new Vector3(0.22f, 0.08f, 0.2f), p.Skin, 0.3f);
            k.Smooth = true;
            k.Ball(h + new Vector3(0.04f, 0.13f, -0.02f), new Vector3(0.14f, 0.05f, 0.13f), p.Hair, 9, 4, null, 0.25f, 4);
            k.Body();

            // Moss: a mantle over the shoulders, tufts on the pauldrons, a tattered loincloth.
            f.Mantle(p.Cloth, 0.1f, 1.35f, p.Cloth2);
            for (int side = -1; side <= 1; side += 2)
            {
                f.ArmPart(side < 0);
                var sh = side < 0 ? f.ShoulderL : f.ShoulderR;
                k.Ball(sh + new Vector3(side * 0.1f, 0.14f, 0f), new Vector3(0.17f, 0.06f, 0.16f), p.Hair, 9, 4, null, 0.3f, side + 7);
                // Rope bound round the forearm.
                var el = side < 0 ? f.ElbowL : f.ElbowR;
                var wr = side < 0 ? f.WristL : f.WristR;
                for (int r = 0; r < 3; r++)
                {
                    var c = Vector3.Lerp(el, wr, 0.35f + r * 0.15f);
                    k.Limb(c - Vector3.up * 0.02f, c + Vector3.up * 0.02f, s.ArmThickness * 1.0f, s.ArmThickness * 1.0f, p.Leather, 8);
                }
            }
            k.Body();
            f.Belt(p.Leather, p.Trim, 0, 0.05f);
            for (int i = 0; i < 5; i++)
            {
                float a = (i - 2) * 0.42f;
                var at = f.Torso(Mathf.Sin(a) * s.TorsoWidth * 1.1f, s.TorsoLength * 0.04f, Mathf.Cos(a) * s.TorsoDepth * 1.2f);
                k.Box(at + new Vector3(0f, -0.2f, 0.02f), new Vector3(0.13f, 0.36f - Mathf.Abs(i - 2) * 0.05f, 0.02f), i % 2 == 0 ? p.Cloth : p.Cloth2, 0.7f, Quaternion.Euler(-8f, a * Mathf.Rad2Deg, 0f));
            }
            // Charms on cords: carved stones hanging from the belt.
            for (int side = -1; side <= 1; side += 2)
            {
                var at = f.Torso(side * s.TorsoWidth * 0.95f, s.TorsoLength * 0.04f, s.TorsoDepth * 0.7f);
                k.Limb(at, at + new Vector3(side * 0.02f, -0.16f, 0.02f), 0.008f, 0.008f, p.Leather, 4);
                k.Smooth = false;
                k.Block(at + new Vector3(side * 0.02f, -0.21f, 0.02f), new Vector3(0.07f, 0.09f, 0.04f), p.Trim, 0.3f);
                k.Smooth = true;
                k.Box(at + new Vector3(side * 0.02f, -0.21f, 0.043f), new Vector3(0.02f, 0.05f, 0.01f), p.Glow);
            }

            // A stone maul hanging from the right hand, its head near the ground.
            f.HeldPart(false);
            var hand = f.HandR;
            // The hands hang low, so the head rests on the ground beside the fist.
            var bottom = hand + new Vector3(0f, 0.16f, -0.02f);
            var top = new Vector3(hand.x + 0.14f, 0.2f, hand.z + 0.3f);
            k.Limb(bottom, top, 0.04f, 0.045f, p.Leather * 0.75f, 7);
            for (int r = 0; r < 3; r++) k.Limb(Vector3.Lerp(hand, bottom, 0.4f * r) - Vector3.up * 0.03f, Vector3.Lerp(hand, bottom, 0.4f * r) + Vector3.up * 0.03f, 0.05f, 0.05f, p.Leather, 7);
            k.Smooth = false;
            var dir = (top - bottom).normalized;
            k.Block(top + dir * 0.06f, new Vector3(0.48f, 0.28f, 0.3f), p.Metal, 0.25f, Quaternion.LookRotation(Vector3.right, dir));
            k.Block(top + dir * 0.06f + new Vector3(0f, 0f, 0.16f), new Vector3(0.18f, 0.16f, 0.04f), p.Trim, 0.25f, Quaternion.LookRotation(Vector3.forward, dir));
            k.Smooth = true;
            k.Box(top + dir * 0.06f + new Vector3(0f, 0f, 0.185f), new Vector3(0.04f, 0.1f, 0.01f), p.Glow, 1f, Quaternion.LookRotation(Vector3.forward, dir));
            k.Body();
        }

        // ---- The Ash Stag ----

        /// <summary>
        /// A burnt stag: charred hide split by embers, an ash-grey ruff, a bone skull-mask, and a
        /// wide crown of antlers hung with scorched prayer strips. Diagonal leg pairs trot together.
        /// </summary>
        static void Stag(MeshKit k)
        {
            var hide = new Color(0.26f, 0.22f, 0.2f);
            var hideLight = new Color(0.36f, 0.31f, 0.27f);
            var ash = new Color(0.62f, 0.6f, 0.57f);
            var bone = new Color(0.84f, 0.78f, 0.66f);
            var hoofC = new Color(0.12f, 0.1f, 0.09f);
            var ember = new Color(1f, 0.52f, 0.16f);
            var cloth = new Color(0.62f, 0.2f, 0.14f);
            var cloth2 = new Color(0.82f, 0.72f, 0.5f);

            void Leg(BodyPart part, float x, float z, bool front)
            {
                var top = new Vector3(x, 0.98f, z);
                k.Part(part, top, 0.55f);
                // Shoulder or haunch muscle, then a thin lower leg with a fetlock and a split hoof.
                k.Ball(top + new Vector3(0f, -0.1f, front ? 0.02f : -0.04f), new Vector3(0.12f, 0.22f, front ? 0.15f : 0.2f), hide, 10, 6);
                var knee = new Vector3(x, 0.55f, z + (front ? 0.05f : -0.1f));
                var fetlock = new Vector3(x, 0.14f, z + (front ? 0.03f : 0.02f));
                k.Limb(top + new Vector3(0f, -0.15f, 0f), knee, 0.1f, 0.055f, hide, 8);
                k.Ball(knee, Vector3.one * 0.055f, hideLight, 7, 4);
                k.Limb(knee, fetlock, 0.045f, 0.038f, hideLight, 7);
                k.Ball(fetlock, new Vector3(0.05f, 0.05f, 0.055f), ash, 7, 4);
                for (int side = -1; side <= 1; side += 2)
                    k.Limb(fetlock + new Vector3(side * 0.018f, -0.03f, 0.01f), new Vector3(x + side * 0.022f, 0.0f, fetlock.z + 0.05f), 0.026f, 0.018f, hoofC, 6);
            }
            Leg(BodyPart.LeftLeg, -0.17f, 0.42f, true);
            Leg(BodyPart.LeftLeg, 0.17f, -0.42f, false);
            Leg(BodyPart.RightLeg, 0.17f, 0.42f, true);
            Leg(BodyPart.RightLeg, -0.17f, -0.42f, false);

            k.Body();
            // Barrel, chest and rump.
            k.Ball(new Vector3(0f, 1.0f, -0.02f), new Vector3(0.27f, 0.27f, 0.55f), hide, 14, 8);
            k.Ball(new Vector3(0f, 1.02f, 0.36f), new Vector3(0.28f, 0.3f, 0.26f), hide, 12, 7);
            k.Ball(new Vector3(0f, 1.05f, -0.42f), new Vector3(0.26f, 0.25f, 0.22f), hide, 12, 7);
            k.Ball(new Vector3(0f, 0.86f, 0.04f), new Vector3(0.2f, 0.12f, 0.42f), hideLight, 10, 5);
            // A spine ridge of charred bone.
            for (int i = 0; i < 6; i++)
            {
                var root = new Vector3(0f, 1.25f, -0.42f + i * 0.15f);
                k.Limb(root, root + new Vector3(0f, 0.07f, -0.03f), 0.03f, 0f, bone * 0.8f, 5);
            }
            // Ember cracks along both flanks.
            for (int side = -1; side <= 1; side += 2)
                for (int i = 0; i < 6; i++)
                {
                    var at = new Vector3(side * 0.265f, 0.96f + Mathf.Sin(i * 1.7f) * 0.09f, -0.38f + i * 0.14f);
                    k.Box(at, new Vector3(0.015f, 0.025f, 0.12f), ember, 1f, Quaternion.Euler(Mathf.Sin(i * 2.3f) * 30f, side * 4f, side * 10f));
                }
            // Tail: a short ashen tuft.
            k.Limb(new Vector3(0f, 1.15f, -0.6f), new Vector3(0f, 1.08f, -0.72f), 0.06f, 0.05f, hide, 7);
            k.Ball(new Vector3(0f, 1.02f, -0.75f), new Vector3(0.06f, 0.09f, 0.05f), ash, 8, 5);

            var neckBase = new Vector3(0f, 1.18f, 0.5f);
            k.Part(BodyPart.Head, neckBase);
            var head = new Vector3(0f, 1.58f, 0.8f);
            k.Limb(neckBase, head - new Vector3(0f, 0.08f, 0.06f), 0.16f, 0.1f, hide, 10);
            // The ruff: shaggy ash hanging under the neck and over the shoulders.
            k.Ball(new Vector3(0f, 1.24f, 0.56f), new Vector3(0.24f, 0.2f, 0.18f), ash, 12, 6, null, 0.2f, 11);
            for (int i = 0; i < 9; i++)
            {
                float a = Mathf.Lerp(-1.3f, 1.3f, i / 8f);
                var root = new Vector3(Mathf.Sin(a) * 0.17f, 1.3f - Mathf.Abs(a) * 0.06f, 0.62f + Mathf.Cos(a) * 0.08f);
                k.Limb(root, root + new Vector3(Mathf.Sin(a) * 0.05f, -0.2f - (i % 3) * 0.04f, 0.04f), 0.045f, 0f, i % 2 == 0 ? ash : ash * 0.85f, 5);
            }
            // Skull: a long bone mask over a dark head.
            var tilt = Quaternion.Euler(28f, 0f, 0f);
            k.Ball(head, new Vector3(0.12f, 0.12f, 0.16f), hide, 10, 6, tilt);
            k.Ball(head + new Vector3(0f, 0.02f, 0.08f), new Vector3(0.1f, 0.09f, 0.12f), bone, 10, 6, tilt);
            k.Limb(head + new Vector3(0f, -0.02f, 0.12f), head + new Vector3(0f, -0.12f, 0.33f), 0.075f, 0.05f, bone, 8);
            k.Ball(head + new Vector3(0f, -0.13f, 0.34f), new Vector3(0.05f, 0.04f, 0.04f), hoofC, 6, 3);
            for (int side = -1; side <= 1; side += 2)
            {
                var eye = head + new Vector3(side * 0.07f, 0.02f, 0.13f);
                k.Ball(eye, new Vector3(0.032f, 0.026f, 0.02f), hoofC, 6, 3);
                k.Ball(eye + new Vector3(side * 0.004f, 0f, 0.012f), Vector3.one * 0.016f, ember, 5, 3);
                // Ears.
                var ear = head + new Vector3(side * 0.1f, 0.06f, -0.04f);
                k.Limb(ear, ear + new Vector3(side * 0.13f, 0.03f, -0.05f), 0.035f, 0f, hide, 6);
                // Antlers: a main beam with five tines, the tips glowing.
                var root = head + new Vector3(side * 0.06f, 0.1f, -0.02f);
                var b1 = root + new Vector3(side * 0.16f, 0.26f, -0.1f);
                var b2 = b1 + new Vector3(side * 0.14f, 0.28f, -0.12f);
                var b3 = b2 + new Vector3(side * 0.02f, 0.22f, -0.04f);
                k.Ball(root, Vector3.one * 0.04f, bone * 0.85f, 6, 4);
                k.Limb(root, b1, 0.035f, 0.03f, bone, 6);
                k.Limb(b1, b2, 0.03f, 0.022f, bone, 6);
                k.Limb(b2, b3, 0.022f, 0.01f, bone, 6);
                k.Ball(b3, Vector3.one * 0.016f, ember, 5, 3);
                var tines = new[]
                {
                    (Vector3.Lerp(root, b1, 0.35f), new Vector3(side * 0.04f, 0.1f, 0.16f)),
                    (b1, new Vector3(side * 0.24f, 0.12f, 0.08f)),
                    (Vector3.Lerp(b1, b2, 0.5f), new Vector3(side * 0.06f, 0.22f, 0.08f)),
                    (b2, new Vector3(side * 0.2f, 0.08f, 0.05f)),
                    (Vector3.Lerp(b2, b3, 0.5f), new Vector3(-side * 0.08f, 0.14f, 0.02f)),
                };
                foreach (var (from, d) in tines)
                {
                    k.Limb(from, from + d, 0.018f, 0.006f, bone, 5);
                    k.Ball(from + d, Vector3.one * 0.012f, ember, 4, 2);
                }
                // Scorched prayer strips tied to the antlers.
                for (int st = 0; st < 2; st++)
                {
                    var tie = Vector3.Lerp(b1, b2, 0.3f + st * 0.4f);
                    k.Limb(tie - Vector3.up * 0.01f, tie + Vector3.up * 0.01f, 0.03f, 0.03f, cloth2, 5);
                    k.Box(tie + new Vector3(side * 0.01f, -0.11f, 0f), new Vector3(0.05f, 0.2f, 0.008f), st == 0 ? cloth : cloth2, 0.8f, Quaternion.Euler(0f, side * 70f, side * 6f));
                }
            }
            k.Body();
        }

        // ---- The Tide Mother ----

        static Palette MotherPalette => new Palette
        {
            Skin = new Color(0.62f, 0.72f, 0.7f), SkinShade = new Color(0.45f, 0.56f, 0.56f),
            Cloth = new Color(0.16f, 0.3f, 0.34f), Cloth2 = new Color(0.26f, 0.44f, 0.42f),
            Leather = new Color(0.3f, 0.36f, 0.22f), Metal = new Color(0.8f, 0.76f, 0.66f), Trim = new Color(0.92f, 0.5f, 0.42f),
            Hair = new Color(0.2f, 0.34f, 0.2f), Eye = new Color(0.55f, 1f, 1f), Pupil = new Color(0.06f, 0.12f, 0.14f), Glow = new Color(0.55f, 1f, 1f),
        };

        /// <summary>
        /// A drowned matriarch, tall and long-armed: a sea-dark robe with a tattered hem of weed,
        /// a net mantle hung with shells, kelp hair, a coral crown, pearls and a trident. Tentacles
        /// spill from under the robe and ripple as she moves.
        /// </summary>
        static void Mother(MeshKit k)
        {
            var p = MotherPalette;
            var s = new FigureSpec
            {
                LegLength = 0.98f, LegThickness = 0.08f, HipWidth = 0.11f,
                TorsoLength = 0.56f, TorsoWidth = 0.21f, TorsoDepth = 0.14f, Hunch = 10f,
                ShoulderWidth = 0.24f, ArmLength = 0.8f, ArmThickness = 0.05f, HandSize = 0.075f,
                HeadSize = 0.14f, NeckLength = 0.07f, Claws = true, Sides = 8,
            };
            var f = new Figure(k, s, p).Layout().Body().Face(false);
            f.Robe(p.Cloth, 0.12f, 2.3f).Sleeves(p.Cloth2, false, p.Leather);
            // The hem: strips of weed hanging to the ground all round.
            k.Body();
            for (int i = 0; i < 18; i++)
            {
                float a = i * Mathf.PI * 2f / 18f;
                float r = s.TorsoWidth * 2.25f;
                var at = new Vector3(Mathf.Sin(a) * r, 0.1f, Mathf.Cos(a) * r * 0.75f);
                k.Box(at, new Vector3(0.07f, 0.2f + (i % 3) * 0.05f, 0.012f), i % 2 == 0 ? p.Leather : p.Cloth2, 0.6f, Quaternion.Euler(0f, a * Mathf.Rad2Deg, 0f));
            }
            // Shells and barnacles on the robe.
            for (int i = 0; i < 9; i++)
            {
                float a = -1.6f + i * 0.4f;
                float y = 0.25f + (i % 3) * 0.24f;
                float r = Mathf.Lerp(s.TorsoWidth * 2.0f, s.TorsoWidth * 1.3f, y / s.LegLength);
                var at = new Vector3(Mathf.Sin(a) * r, y, Mathf.Cos(a) * r * 0.78f);
                k.Ball(at, new Vector3(0.05f, 0.045f, 0.03f), i % 2 == 0 ? p.Metal : p.Trim, 7, 3, Quaternion.Euler(0f, a * Mathf.Rad2Deg, 0f));
            }
            f.Belt(p.Leather, p.Metal, 0, 0.04f);
            // A net mantle hung with shells, and a string of pearls.
            f.Mantle(p.Cloth2, 0.24f, 1.6f, p.Leather);
            for (int i = 0; i < 7; i++)
            {
                float a = Mathf.Lerp(-1.3f, 1.3f, i / 6f);
                var at = f.Torso(Mathf.Sin(a) * s.ShoulderWidth * 1.1f, s.TorsoLength * 0.74f, Mathf.Cos(a) * s.TorsoDepth * 1.45f);
                k.Ball(at, new Vector3(0.035f, 0.04f, 0.02f), i % 2 == 0 ? p.Trim : p.Metal, 6, 3, Quaternion.Euler(0f, a * Mathf.Rad2Deg, 0f));
            }
            for (int i = 0; i < 11; i++)
            {
                float a = Mathf.Lerp(-1.2f, 1.2f, i / 10f);
                var at = f.Torso(Mathf.Sin(a) * s.TorsoWidth * 0.62f, s.TorsoLength * (1.0f - Mathf.Cos(a) * 0.12f), Mathf.Cos(a) * s.TorsoDepth * 0.95f);
                k.Ball(at, Vector3.one * 0.016f, new Color(0.95f, 0.94f, 0.9f), 6, 4);
            }
            // Kelp hair falling past the shoulders, and a coral crown.
            f.Hair(p.Hair, 1.08f);
            k.Part(BodyPart.Head, f.NeckBase);
            float hs = s.HeadSize;
            for (int i = 0; i < 9; i++)
            {
                float a = Mathf.Lerp(-2.2f, 2.2f, i / 8f);
                var root = f.Head + new Vector3(Mathf.Sin(a) * hs * 0.85f, hs * 0.1f, -Mathf.Cos(a) * hs * 0.6f);
                var tip = root + new Vector3(Mathf.Sin(a) * 0.04f, -0.32f - (i % 3) * 0.06f, -0.06f);
                k.Limb(root, tip, 0.03f, 0.008f, i % 2 == 0 ? p.Hair : p.Leather, 6);
            }
            var crown = f.Head + new Vector3(0f, hs * 0.7f, -hs * 0.05f);
            k.Lathe(crown, new[] { new Vector2(0f, hs * 0.78f), new Vector2(0.03f, hs * 0.8f) }, 14, p.Trim);
            for (int i = 0; i < 7; i++)
            {
                float a = Mathf.Lerp(-1.4f, 1.4f, i / 6f);
                var root = crown + new Vector3(Mathf.Sin(a) * hs * 0.78f, 0.02f, Mathf.Cos(a) * hs * 0.5f);
                float tall = 0.18f - Mathf.Abs(a) * 0.05f;
                var tip = root + new Vector3(Mathf.Sin(a) * 0.05f, tall, -0.02f);
                k.Limb(root, tip, 0.02f, 0.007f, p.Trim, 5);
                var mid = Vector3.Lerp(root, tip, 0.55f);
                k.Limb(mid, mid + new Vector3(Mathf.Sin(a) * 0.06f + 0.02f, 0.06f, 0f), 0.012f, 0.004f, p.Trim, 4);
                k.Ball(tip, Vector3.one * 0.012f, p.Glow, 4, 2);
            }
            // A pale pearl set in the crown's front.
            k.Ball(crown + new Vector3(0f, 0.04f, hs * 0.8f), Vector3.one * 0.028f, p.Glow, 7, 4);
            k.Body();
            f.Weapon("trident", 1.5f, p.Metal, p.Leather);

            // Tentacles from under the hem, alternating between the two leg groups so they ripple.
            for (int i = 0; i < 10; i++)
            {
                float a = (i + 0.5f) * Mathf.PI * 2f / 10f;
                var dirOut = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a) * 0.85f);
                var root = dirOut * s.TorsoWidth * 1.9f + Vector3.up * 0.16f;
                k.Part(i % 2 == 0 ? BodyPart.LeftLeg : BodyPart.RightLeg, root + Vector3.up * 0.4f);
                var mid = root + dirOut * 0.2f + Vector3.down * 0.1f;
                var tip = mid + dirOut * 0.22f + new Vector3(Mathf.Cos(a) * 0.08f, 0.04f, 0f);
                k.Limb(root, mid, 0.075f, 0.055f, p.SkinShade, 8);
                k.Limb(mid, tip, 0.055f, 0f, p.Skin, 7);
                for (int sct = 0; sct < 3; sct++)
                    k.Ball(Vector3.Lerp(root, tip, 0.25f + sct * 0.22f) + Vector3.down * 0.04f, Vector3.one * 0.016f, p.Trim, 5, 3);
            }
            k.Body();
        }

        // ---- The Last Test ----

        static Palette ConstructPalette => new Palette
        {
            Skin = new Color(0.86f, 0.83f, 0.77f), SkinShade = new Color(0.66f, 0.63f, 0.58f),
            Cloth = new Color(0.17f, 0.2f, 0.33f), Cloth2 = new Color(0.28f, 0.3f, 0.44f),
            Leather = new Color(0.35f, 0.26f, 0.2f), Metal = new Color(0.82f, 0.64f, 0.32f), Trim = new Color(0.96f, 0.84f, 0.5f),
            Hair = Grey(0.3f), Eye = new Color(1f, 0.93f, 0.7f), Pupil = new Color(0.12f, 0.11f, 0.12f), Glow = new Color(1f, 0.9f, 0.62f),
        };

        /// <summary>
        /// The Last Test: a tall construct of pale stone in gilded armour, a burning core caged in
        /// its open chest, a faceless mask with one slit of light, a broken halo and a long sword.
        /// </summary>
        static void Construct(MeshKit k)
        {
            var p = ConstructPalette;
            var s = new FigureSpec
            {
                LegLength = 0.88f, LegThickness = 0.1f, HipWidth = 0.14f,
                TorsoLength = 0.64f, TorsoWidth = 0.29f, TorsoDepth = 0.19f, Hunch = 3f,
                ShoulderWidth = 0.34f, ArmLength = 0.72f, ArmThickness = 0.075f, HandSize = 0.09f,
                HeadSize = 0.13f, NeckLength = 0.06f, Sides = 8,
            };
            k.Smooth = false;
            var f = new Figure(k, s, p).Layout().Body();
            k.Smooth = true;
            f.Boots(p.Metal, 0.36f, p.Trim).Gloves(p.Metal).Pauldrons(p.Metal, 0.18f, p.Trim);
            f.Belt(p.Leather, p.Trim, 0, 0.05f).Tabard(p.Cloth, 0.6f, 0.24f).Cloak(p.Cloth, 0.15f, p.Cloth2);
            // Greaves and vambraces.
            for (int side = 0; side < 2; side++)
            {
                bool left = side == 0;
                f.LegPart(left);
                var knee = left ? f.KneeL : f.KneeR;
                k.Ball(knee + new Vector3(0f, 0.02f, 0.05f), new Vector3(0.09f, 0.08f, 0.07f), p.Trim, 10, 5);
                k.Limb(left ? f.HipL : f.HipR, knee + Vector3.up * 0.06f, s.LegThickness * 1.5f, s.LegThickness * 1.25f, p.Metal * 0.85f, 10);
                f.ArmPart(left);
                var el = left ? f.ElbowL : f.ElbowR;
                k.Ball(el, Vector3.one * s.ArmThickness * 1.25f, p.Trim, 9, 5);
            }
            k.Body();
            // An open chest: a gilded frame round a caged core.
            var core = f.Torso(0f, s.TorsoLength * 0.64f, s.TorsoDepth * 0.55f);
            k.Ball(f.Chest + f.TorsoRotation * new Vector3(0f, 0f, -s.TorsoDepth * 0.1f), new Vector3(s.TorsoWidth * 1.15f, s.TorsoLength * 0.38f, s.TorsoDepth * 1.05f), p.Metal, 14, 7, f.TorsoRotation);
            k.Lathe(core, new[] { new Vector2(-0.02f, 0.17f), new Vector2(0.02f, 0.17f), new Vector2(0.02f, 0.13f) }, 16, p.Trim, 1f, f.TorsoRotation * Quaternion.Euler(90f, 0f, 0f));
            k.Gem(core + f.TorsoRotation * new Vector3(0f, 0f, 0.05f), new Vector3(0.1f, 0.14f, 0.1f), p.Glow);
            for (int i = 0; i < 4; i++)
            {
                float x = (i - 1.5f) * 0.065f;
                k.Limb(core + f.TorsoRotation * new Vector3(x, 0.15f, 0.07f), core + f.TorsoRotation * new Vector3(x, -0.15f, 0.07f), 0.012f, 0.012f, p.Metal, 6);
            }
            // A gorget and a fauld of plates over the hips.
            k.Lathe(f.Torso(0f, s.TorsoLength * 0.95f, 0f), new[] { new Vector2(-0.05f, s.TorsoWidth * 0.75f), new Vector2(0.04f, s.TorsoWidth * 0.55f) }, 14, p.Trim, 0.8f, f.TorsoRotation);
            for (int i = 0; i < 7; i++)
            {
                float a = (i - 3) * 0.45f;
                var at = f.Torso(Mathf.Sin(a) * s.TorsoWidth * 1.1f, -0.06f, Mathf.Cos(a) * s.TorsoDepth * 1.2f);
                k.Block(at, new Vector3(0.12f, 0.16f, 0.025f), i % 2 == 0 ? p.Metal : p.Trim, 0.25f, Quaternion.Euler(-10f, a * Mathf.Rad2Deg, 0f));
            }

            // Head: a smooth faceless mask with a slit of light, a crest, and a broken halo behind.
            k.Part(BodyPart.Head, f.NeckBase);
            var h = f.Head;
            float hs = s.HeadSize;
            k.Ball(h, new Vector3(hs * 0.92f, hs * 1.08f, hs * 0.98f), p.Skin, 14, 9);
            k.Ball(h + new Vector3(0f, -hs * 0.05f, hs * 0.2f), new Vector3(hs * 0.82f, hs * 0.95f, hs * 0.82f), p.Trim, 14, 8);
            k.Box(h + new Vector3(0f, hs * 0.08f, hs * 0.99f), new Vector3(hs * 1.0f, hs * 0.1f, 0.02f), p.Glow);
            k.Lathe(h + new Vector3(0f, hs * 0.85f, -0.02f), new[] { new Vector2(0f, 0.02f), new Vector2(0.14f, 0.012f) }, 6, p.Metal, 7f);
            // The broken halo: a ring behind the head with a gap at the upper left.
            var haloAt = h + new Vector3(0f, hs * 0.3f, -hs * 1.1f);
            for (int i = 0; i < 24; i++)
            {
                if (i >= 6 && i <= 8) continue;
                float a0 = i * Mathf.PI * 2f / 24f, a1 = (i + 1) * Mathf.PI * 2f / 24f;
                var p0 = haloAt + new Vector3(Mathf.Cos(a0) * 0.34f, Mathf.Sin(a0) * 0.34f, 0f);
                var p1 = haloAt + new Vector3(Mathf.Cos(a1) * 0.34f, Mathf.Sin(a1) * 0.34f, 0f);
                k.Limb(p0, p1, 0.03f, 0.03f, p.Glow, 6);
            }
            // A greatsword held point down at the right side.
            f.HeldPart(false);
            var grip = f.HandR;
            var blade = Grey(0.85f);
            k.Limb(grip + Vector3.down * 0.08f, grip + Vector3.up * 0.14f, 0.022f, 0.022f, p.Leather, 7);
            k.Ball(grip + Vector3.up * 0.16f, Vector3.one * 0.04f, p.Trim, 8, 5);
            k.Limb(grip + new Vector3(-0.15f, -0.09f, 0f), grip + new Vector3(0.15f, -0.09f, 0f), 0.03f, 0.03f, p.Metal, 7);
            k.Gem(grip + new Vector3(0f, -0.09f, 0.03f), new Vector3(0.03f, 0.04f, 0.02f), p.Glow);
            float length = grip.y - 0.12f;
            k.Block(grip + new Vector3(0f, -0.12f - length * 0.5f, 0f), new Vector3(0.1f, length, 0.025f), blade, 0.15f);
            k.Limb(new Vector3(grip.x, 0.08f, grip.z), new Vector3(grip.x, -0.02f, grip.z), 0.05f, 0f, blade, 4);
            k.Body();
        }
    }
}
