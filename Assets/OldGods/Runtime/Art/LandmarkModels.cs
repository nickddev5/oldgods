using OldGods.Rules;
using UnityEngine;

namespace OldGods.Runtime
{
    /// <summary>The colours one biome's ruins are built in.</summary>
    public struct DressingColors
    {
        public Color Stone;
        /// <summary>Moss on the steppe, char in the wood, weed on the coast.</summary>
        public Color Growth;
        public Color Wood;
        public Color Dark;
    }

    /// <summary>
    /// Code-built ruin pieces: columns, walls, the fallen colossus, the great stump, the wreck.
    /// Each piece is drawn in its own local space (base on the ground at y = 0, length along x)
    /// into a shared kit whose Placement puts it in the world. Embers and fire go into a
    /// second kit drawn with a glowing material.
    /// </summary>
    public static class LandmarkModels
    {
        /// <summary>A stone colour, varied in brightness between lo and hi.</summary>
        delegate Color Shade(float lo = 0.88f, float hi = 1.04f);

        public static void Draw(MeshKit k, MeshKit glow, Piece p, DressingColors c)
        {
            var rnd = new System.Random(p.Variant);
            float R(float a, float b) => a + (float)rnd.NextDouble() * (b - a);
            Shade S = (lo, hi) => { float v = R(lo, hi); return new Color(c.Stone.r * v, c.Stone.g * v, c.Stone.b * v); };
            float w = p.Width, h = p.Height, d = p.Depth;
            k.Smooth = false;
            glow.Smooth = false;

            switch (p.Kind)
            {
                case PieceKind.Column:
                    ColumnFoot(k, w, S);
                    k.Prism(new Vector3(0f, 0.6f, 0f), w * 0.42f, h - 1.2f, 10, S(), 0.92f);
                    k.Block(new Vector3(0f, h - 0.45f, 0f), new Vector3(w * 1.15f, 0.3f, w * 1.15f), S(), 0.3f);
                    k.Block(new Vector3(0f, h - 0.15f, 0f), new Vector3(w * 1.45f, 0.3f, w * 1.45f), S(0.8f, 0.95f), 0.3f);
                    if (R(0f, 1f) < 0.5f) k.Ball(new Vector3(R(-0.2f, 0.2f), h + 0.02f, R(-0.2f, 0.2f)), new Vector3(w * 0.55f, 0.12f, w * 0.5f), c.Growth, 7, 3, null, 0.25f, p.Variant);
                    break;

                case PieceKind.BrokenColumn:
                    ColumnFoot(k, w, S);
                    if (h > 0.9f) k.Prism(new Vector3(0f, 0.6f, 0f), w * 0.42f, h - 0.9f, 10, S(), 0.95f);
                    k.Block(new Vector3(R(-0.1f, 0.1f), h - 0.2f, 0f), new Vector3(w * 0.75f, 0.5f, w * 0.65f), S(), 0.35f,
                        Quaternion.Euler(R(-25f, 25f), R(0f, 90f), R(-25f, 25f)));
                    // A drum that came off, lying at the foot.
                    k.Limb(new Vector3(w * 0.9f, w * 0.4f, R(-0.6f, 0.6f)), new Vector3(w * 0.9f + R(-0.3f, 0.3f), w * 0.4f, R(0.9f, 1.4f)), w * 0.4f, w * 0.38f, S(0.8f, 0.95f), 9);
                    if (R(0f, 1f) < 0.4f) k.Ball(new Vector3(0f, h * 0.35f, w * 0.42f), new Vector3(w * 0.3f, h * 0.25f, 0.08f), c.Growth, 6, 3, null, 0.3f, p.Variant + 1);
                    break;

                case PieceKind.FallenColumn:
                {
                    float r = h * 0.5f;
                    int drums = Mathf.Max(2, Mathf.RoundToInt(w / 1.7f));
                    float len = w / drums;
                    for (int i = 0; i < drums; i++)
                    {
                        float x0 = -w * 0.5f + i * len + 0.04f, x1 = x0 + len - 0.08f;
                        Vector3 off = new Vector3(0f, R(-0.04f, 0.02f), R(-0.12f, 0.12f));
                        k.Limb(new Vector3(x0, r * 0.92f, 0f) + off, new Vector3(x1, r * 0.92f, 0f) + off, r, r * 0.97f, S(), 10);
                    }
                    if (R(0f, 1f) < 0.5f) k.Ball(new Vector3(R(-w * 0.3f, w * 0.3f), r * 1.85f, 0f), new Vector3(w * 0.15f, 0.1f, r * 0.7f), c.Growth, 6, 3, null, 0.3f, p.Variant);
                    break;
                }

                case PieceKind.Wall:
                    Courses(k, -w * 0.5f, w * 0.5f, h, d, S, R, jagged: true);
                    if (R(0f, 1f) < 0.6f) k.Ball(new Vector3(R(-w * 0.3f, w * 0.3f), h * 0.92f, 0f), new Vector3(w * 0.25f, 0.18f, d * 0.55f), c.Growth, 7, 3, null, 0.3f, p.Variant);
                    break;

                case PieceKind.Rubble:
                {
                    int n = 5 + rnd.Next(4);
                    for (int i = 0; i < n; i++)
                    {
                        float s = R(0.5f, 1.1f);
                        k.Block(new Vector3(R(-w * 0.45f, w * 0.45f), R(0.1f, h * 0.6f), R(-d * 0.5f, d * 0.5f)), new Vector3(s * 1.2f, s * 0.6f, s * 0.8f), S(0.75f, 1f), 0.3f,
                            Quaternion.Euler(R(-20f, 20f), R(0f, 180f), R(-20f, 20f)));
                    }
                    break;
                }

                case PieceKind.Arch:
                {
                    float pillar = 1.6f, top = h - 1.1f;
                    Courses(k, -w * 0.5f, -w * 0.5f + pillar, top, d, S, R, jagged: false);
                    Courses(k, w * 0.5f - pillar, w * 0.5f, top, d, S, R, jagged: false);
                    // Lintel stones and a keystone, one end cracked down.
                    float x = -w * 0.5f;
                    while (x < w * 0.5f - 0.1f)
                    {
                        float len = Mathf.Min(R(1.4f, 2.2f), w * 0.5f - x);
                        bool cracked = x > w * 0.25f;
                        k.Block(new Vector3(x + len * 0.5f, top + 0.5f - (cracked ? 0.12f : 0f), 0f), new Vector3(len - 0.06f, 1f, d), S(), 0.2f,
                            cracked ? Quaternion.Euler(0f, 0f, -4f) : (Quaternion?)null);
                        x += len;
                    }
                    k.Block(new Vector3(0f, top + 0.65f, 0f), new Vector3(1.1f, 1.4f, d + 0.2f), S(0.95f, 1.08f), 0.25f);
                    k.Box(new Vector3(0f, top + 0.7f, d * 0.5f + 0.11f), new Vector3(0.5f, 0.6f, 0.02f), c.Dark);
                    k.Ball(new Vector3(-w * 0.3f, top + 1.02f, 0f), new Vector3(w * 0.2f, 0.12f, d * 0.5f), c.Growth, 7, 3, null, 0.3f, p.Variant);
                    break;
                }

                case PieceKind.Altar:
                    k.Block(new Vector3(0f, h * 0.4f, 0f), new Vector3(w, h * 0.8f, d), S(), 0.25f);
                    k.Block(new Vector3(0f, h * 0.9f, 0f), new Vector3(w * 1.12f, h * 0.2f, d * 1.15f), S(0.92f, 1.06f), 0.3f);
                    k.Box(new Vector3(0f, h * 0.45f, d * 0.5f + 0.01f), new Vector3(w * 0.6f, 0.08f, 0.02f), c.Dark);
                    // A fire still burns on it.
                    k.Prism(new Vector3(0f, h, 0f), 0.45f, 0.25f, 8, c.Dark, 1.2f);
                    Flame(glow, new Vector3(0f, h + 0.2f, 0f), 0.5f, rnd);
                    break;

                case PieceKind.Monolith:
                    k.Box(new Vector3(0f, h * 0.5f, 0f), new Vector3(w, h, d), S(), 0.82f);
                    k.Block(new Vector3(0f, h + 0.08f, 0f), new Vector3(w * 0.8f, 0.3f, d * 0.85f), S(0.85f, 0.95f), 0.4f, Quaternion.Euler(0f, 0f, R(-8f, 8f)));
                    for (int i = 0; i < 3; i++)
                        k.Box(new Vector3(0f, h * (0.3f + i * 0.17f), d * 0.5f - 0.02f), new Vector3(w * 0.55f, 0.06f, 0.05f), c.Dark, 1f);
                    if (R(0f, 1f) < 0.6f) k.Ball(new Vector3(R(-0.2f, 0.2f), h * 0.15f, -d * 0.45f), new Vector3(w * 0.4f, h * 0.15f, 0.12f), c.Growth, 6, 3, null, 0.3f, p.Variant);
                    break;

                case PieceKind.Lintel:
                    k.Block(new Vector3(0f, h * 0.5f, 0f), new Vector3(w, h, d), S(), 0.3f);
                    break;

                case PieceKind.Doorway:
                    k.Block(new Vector3(-w * 0.5f + 0.3f, h * 0.5f, 0f), new Vector3(0.6f, h, d), S(), 0.25f);
                    k.Block(new Vector3(w * 0.5f - 0.3f, h * 0.5f, 0f), new Vector3(0.6f, h, d), S(), 0.25f);
                    k.Block(new Vector3(0f, h + 0.25f, 0f), new Vector3(w + 0.5f, 0.5f, d + 0.1f), S(0.9f, 1f), 0.25f);
                    // The dark passage into the mound.
                    k.Quad(new Vector3(w * 0.5f - 0.6f, 0f, -0.1f), new Vector3(w * 0.5f - 0.6f, h, -0.1f), new Vector3(-w * 0.5f + 0.6f, h, -0.1f), new Vector3(-w * 0.5f + 0.6f, 0f, -0.1f), c.Dark);
                    break;

                case PieceKind.Milestone:
                    k.Box(new Vector3(0f, h * 0.5f, 0f), new Vector3(w, h, d), S(), 0.85f);
                    k.Block(new Vector3(0f, h, 0f), new Vector3(w * 0.9f, 0.18f, d * 0.95f), S(0.85f, 0.95f), 0.4f);
                    k.Box(new Vector3(0f, h * 0.7f, d * 0.5f), new Vector3(w * 0.5f, 0.05f, 0.03f), c.Dark);
                    break;

                case PieceKind.ColossusHead:
                    Head(k, c, S, R, rnd, p.Variant);
                    break;

                case PieceKind.ColossusHand:
                    Hand(k, S);
                    break;

                case PieceKind.ColossusTorso:
                    Torso(k, c, w, h, d, S, R);
                    break;

                case PieceKind.Stump:
                    Stump(k, glow, c, w * 0.5f, h, R, rnd);
                    break;

                case PieceKind.Hull:
                    Hull(k, c, w, h, d, R, rnd);
                    break;

                case PieceKind.Tower:
                    Tower(k, c, w * 0.5f, h, S, R);
                    break;

                case PieceKind.Boulder:
                    k.Ball(new Vector3(0f, h * 0.4f, 0f), new Vector3(w * 0.5f, h * 0.55f, d * 0.5f), S(0.85f, 1f), 8, 5, Quaternion.Euler(R(-10f, 10f), 0f, R(-10f, 10f)), 0.18f, p.Variant);
                    k.Ball(new Vector3(w * 0.3f, h * 0.15f, d * 0.25f), new Vector3(w * 0.22f, h * 0.2f, d * 0.2f), S(0.75f, 0.9f), 6, 4, null, 0.2f, p.Variant + 1);
                    if (R(0f, 1f) < 0.6f) k.Ball(new Vector3(0f, h * 0.9f, 0f), new Vector3(w * 0.28f, 0.12f, d * 0.25f), c.Growth, 7, 3, null, 0.3f, p.Variant + 2);
                    break;

                case PieceKind.Embers:
                {
                    int n = 6 + rnd.Next(6);
                    for (int i = 0; i < n; i++)
                    {
                        float s = R(0.12f, 0.3f);
                        var at = new Vector3(R(-w * 0.5f, w * 0.5f), s * 0.3f, R(-w * 0.5f, w * 0.5f));
                        glow.Gem(at, new Vector3(s, s * 0.5f, s), Color.white * R(0.6f, 1f));
                    }
                    k.Ball(new Vector3(0f, -0.05f, 0f), new Vector3(w * 0.45f, 0.12f, w * 0.45f), c.Dark, 7, 3, null, 0.3f, p.Variant);
                    break;
                }
            }
            k.Smooth = true;
            glow.Smooth = true;
        }

        static void ColumnFoot(MeshKit k, float w, Shade S)
        {
            k.Block(new Vector3(0f, 0.2f, 0f), new Vector3(w * 1.4f, 0.4f, w * 1.4f), S(0.85f, 0.95f), 0.3f);
            k.Block(new Vector3(0f, 0.5f, 0f), new Vector3(w * 1.2f, 0.2f, w * 1.2f), S(0.9f, 1f), 0.3f);
        }

        /// <summary>Courses of cut stone from x0 to x1, height h, depth d; a jagged wall loses stones near the top.</summary>
        static void Courses(MeshKit k, float x0, float x1, float h, float d, Shade S, System.Func<float, float, float> R, bool jagged)
        {
            float y = 0f;
            int row = 0;
            while (y < h - 0.1f)
            {
                float ch = Mathf.Min(R(0.55f, 0.75f), h - y);
                float x = x0 + (row % 2 == 1 ? -R(0.3f, 0.6f) : 0f);
                while (x < x1 - 0.05f)
                {
                    float len = R(0.9f, 1.6f);
                    float a = Mathf.Max(x, x0), b = Mathf.Min(x + len, x1);
                    bool missing = jagged && y > h * 0.55f && R(0f, 1f) < (y / h - 0.45f) * 0.9f;
                    if (b - a > 0.15f && !missing)
                        k.Block(new Vector3((a + b) * 0.5f, y + ch * 0.5f, R(-0.05f, 0.05f)), new Vector3(b - a - 0.05f, ch - 0.04f, d - R(0f, 0.15f)), S(0.82f, 1.04f), 0.22f);
                    x += len;
                }
                y += ch;
                row++;
            }
        }

        static void Flame(MeshKit glow, Vector3 at, float size, System.Random rnd)
        {
            for (int i = 0; i < 4; i++)
            {
                float a = i * 1.6f + (float)rnd.NextDouble();
                var off = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * size * 0.3f;
                glow.Limb(at + off, at + off * 0.3f + Vector3.up * size * (1.1f + (float)rnd.NextDouble() * 0.6f), size * 0.28f, 0f, Color.white * (0.7f + 0.1f * i), 5);
            }
        }

        /// <summary>A colossal god's head lying on its back, face up, crown to local +z.</summary>
        static void Head(MeshKit k, DressingColors c, Shade S, System.Func<float, float, float> R, System.Random rnd, int seed)
        {
            // Built upright facing +z, then laid on its back looking at the sky, crown to +z.
            var outer = k.Placement;
            k.Placement = outer * Matrix4x4.TRS(new Vector3(0f, 2.6f, 0f), Quaternion.Euler(0f, 180f, 0f) * Quaternion.Euler(-82f, 0f, 0f), Vector3.one * 1.3f);
            k.Ball(Vector3.zero, new Vector3(2.1f, 2.5f, 2.2f), S(0.92f, 1f), 10, 7, null, 0.04f, seed);
            k.Block(new Vector3(0f, 0.55f, 1.8f), new Vector3(3f, 0.5f, 0.9f), S(), 0.3f);
            k.Block(new Vector3(0f, -0.1f, 2.2f), new Vector3(0.6f, 1.2f, 0.8f), S(), 0.3f, Quaternion.Euler(-12f, 0f, 0f));
            k.Box(new Vector3(-0.72f, 0.15f, 2f), new Vector3(0.75f, 0.35f, 0.3f), c.Dark);
            k.Box(new Vector3(0.72f, 0.15f, 2f), new Vector3(0.75f, 0.35f, 0.3f), c.Dark);
            k.Block(new Vector3(0f, -0.95f, 1.95f), new Vector3(1.3f, 0.3f, 0.5f), S(0.85f, 0.95f), 0.3f);
            // A squared beard and a crown of rays.
            k.Box(new Vector3(0f, -1.9f, 1.1f), new Vector3(2.4f, 1.8f, 1.6f), S(0.85f, 0.95f), 1.25f);
            k.Lathe(new Vector3(0f, 1.3f, 0f), new[] { new Vector2(0f, 2.2f), new Vector2(0.5f, 2.25f) }, 12, S(0.8f, 0.9f));
            for (int i = 0; i < 7; i++)
            {
                float a = i * Mathf.PI * 2f / 7f + 0.2f;
                var root = new Vector3(Mathf.Cos(a) * 1.7f, 1.7f, Mathf.Sin(a) * 1.7f);
                if (R(0f, 1f) < 0.3f) continue; // broken off
                k.Limb(root, root + new Vector3(Mathf.Cos(a) * 0.5f, R(1f, 1.5f), Mathf.Sin(a) * 0.5f), 0.32f, 0f, S(0.85f, 0.95f), 5);
            }
            k.Prism(new Vector3(0f, -3.2f, -0.2f), 1.2f, 0.9f, 9, S(0.8f, 0.9f));
            // Moss on the upturned side.
            k.Ball(new Vector3(2f, 0.4f, -0.2f), new Vector3(0.3f, 1.6f, 1.5f), c.Growth, 8, 4, null, 0.25f, seed + 3);
            k.Placement = outer;
        }

        /// <summary>A colossal hand lying palm up, fingers curling, wrist to -z.</summary>
        static void Hand(MeshKit k, Shade S)
        {
            k.Block(new Vector3(0f, 0.55f, 0f), new Vector3(2.6f, 1.1f, 2.8f), S(), 0.35f);
            k.Limb(new Vector3(0f, 0.6f, -1.3f), new Vector3(0f, 0.7f, -2.9f), 1f, 1.1f, S(0.85f, 0.95f), 8);
            for (int i = 0; i < 4; i++)
            {
                float x = -0.95f + i * 0.63f, curl = 0.15f * i;
                var a = new Vector3(x, 0.9f, 1.3f);
                var b = new Vector3(x, 1.25f + curl, 2.35f - curl * 0.5f);
                var tip = new Vector3(x, 2f + curl, 2.7f - curl);
                k.Limb(a, b, 0.33f, 0.3f, S(), 7);
                k.Limb(b, tip, 0.3f, 0.24f, S(), 7);
            }
            k.Limb(new Vector3(1.25f, 0.8f, -0.1f), new Vector3(2.1f, 1.25f, 0.8f), 0.38f, 0.33f, S(), 7);
            k.Limb(new Vector3(2.1f, 1.25f, 0.8f), new Vector3(2.3f, 1.9f, 1.3f), 0.33f, 0.26f, S(), 7);
        }

        /// <summary>A colossal armoured torso on its back, shoulders to +x, broken waist to -x.</summary>
        static void Torso(MeshKit k, DressingColors c, float w, float h, float d, Shade S, System.Func<float, float, float> R)
        {
            k.Block(new Vector3(0f, h * 0.45f, 0f), new Vector3(w * 0.82f, h * 0.9f, d * 0.92f), S(0.9f, 1f), 0.35f);
            foreach (float side in new[] { -1f, 1f })
            {
                k.Block(new Vector3(w * 0.12f, h * 0.93f, side * d * 0.22f), new Vector3(w * 0.3f, 0.5f, d * 0.4f), S(0.95f, 1.06f), 0.4f);
                k.Ball(new Vector3(w * 0.43f, h * 0.48f, side * d * 0.42f), new Vector3(1.5f, 1.5f, 1.4f), S(0.9f, 1f), 9, 5, null, 0.05f, side > 0 ? 7 : 8);
            }
            // A belt, and the folds of a robe carved below it.
            k.Box(new Vector3(-w * 0.18f, h * 0.48f, 0f), new Vector3(0.7f, h * 0.98f, d * 0.96f), S(0.75f, 0.85f));
            for (int i = 0; i < 4; i++)
                k.Box(new Vector3(-w * 0.32f, h * 0.9f, -d * 0.3f + i * d * 0.2f), new Vector3(w * 0.2f, 0.12f, 0.12f), c.Dark);
            for (int i = 0; i < 5; i++)
                k.Block(new Vector3(-w * 0.45f + R(-0.6f, 0.4f), R(0.2f, 0.7f), R(-d * 0.45f, d * 0.45f)), new Vector3(R(0.8f, 1.5f), R(0.5f, 1f), R(0.7f, 1.3f)), S(0.8f, 0.95f), 0.3f,
                    Quaternion.Euler(R(-20f, 20f), R(0f, 180f), R(-20f, 20f)));
            k.Ball(new Vector3(0f, h * 0.92f, d * 0.2f), new Vector3(w * 0.2f, 0.15f, d * 0.25f), c.Growth, 7, 3, null, 0.3f, 9);
        }

        /// <summary>The burnt stump of a giant tree: flared roots, splintered top, embers in the cracks.</summary>
        static void Stump(MeshKit k, MeshKit glow, DressingColors c, float r, float h, System.Func<float, float, float> R, System.Random rnd)
        {
            Color bark = c.Wood * 0.55f, charred = c.Dark;
            k.Lathe(Vector3.zero, new[] { new Vector2(-0.3f, r * 1.4f), new Vector2(0.8f, r * 1.05f), new Vector2(h * 0.5f, r * 0.95f), new Vector2(h * 0.8f, r * 0.88f) }, 11, bark);
            k.Prism(new Vector3(0f, h * 0.78f, 0f), r * 0.7f, 0.05f, 9, charred);
            int splinters = 10;
            for (int i = 0; i < splinters; i++)
            {
                float a = i * Mathf.PI * 2f / splinters + R(-0.1f, 0.1f);
                var root = new Vector3(Mathf.Cos(a) * r * 0.78f, h * 0.78f, Mathf.Sin(a) * r * 0.78f);
                float tall = i % 3 == 0 ? R(1.8f, 3.2f) : R(0.4f, 1.3f);
                k.Limb(root, root + new Vector3(Mathf.Cos(a) * 0.2f, tall, Mathf.Sin(a) * 0.2f), 0.55f, 0.02f, i % 2 == 0 ? bark : charred, 4);
            }
            for (int i = 0; i < 7; i++)
            {
                float a = i * Mathf.PI * 2f / 7f + R(-0.2f, 0.2f);
                var dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                var mid = dir * (r + R(0.8f, 1.4f)) + Vector3.up * 0.5f;
                k.Limb(dir * r * 0.8f + Vector3.up * 1.4f, mid, 0.85f, 0.55f, bark, 6);
                k.Limb(mid, dir * (r + R(2.4f, 3.4f)) + Vector3.down * 0.25f, 0.55f, 0.12f, bark, 6);
            }
            // Glowing cracks up the trunk.
            for (int i = 0; i < 6; i++)
            {
                float a = i * Mathf.PI * 2f / 6f + R(0f, 0.5f);
                float y = R(1f, h * 0.6f), rr = Mathf.Lerp(r * 1.06f, r * 0.94f, y / h) + 0.04f;
                glow.Box(new Vector3(Mathf.Cos(a) * rr, y, Mathf.Sin(a) * rr), new Vector3(0.14f, R(1f, 2.2f), 0.14f), Color.white * R(0.6f, 1f), 1f,
                    Quaternion.Euler(0f, -a * Mathf.Rad2Deg, R(-12f, 12f)));
            }
            Flame(glow, new Vector3(0f, h * 0.78f, 0f), r * 0.35f, rnd);
        }

        /// <summary>A wrecked ship's hull, keel down, bow to +x; planks missing amidships to show the ribs.</summary>
        static void Hull(MeshKit k, DressingColors c, float len, float h, float beam, System.Func<float, float, float> R, System.Random rnd)
        {
            const int stations = 10, around = 6;
            var pts = new Vector3[stations + 1, around + 1];
            for (int i = 0; i <= stations; i++)
            {
                float t = i / (float)stations * 2f - 1f;
                float half = beam * 0.5f * Mathf.Pow(Mathf.Max(0f, 1f - Mathf.Pow(Mathf.Abs(t), 2.2f)), 0.6f) + 0.05f;
                float deck = h + 0.6f * t * t;
                float bottom = h * (1f - Mathf.Sqrt(Mathf.Max(0f, 1f - Mathf.Pow(Mathf.Abs(t), 3f))));
                for (int j = 0; j <= around; j++)
                {
                    float th = -Mathf.PI * 0.5f + Mathf.PI * j / around;
                    pts[i, j] = new Vector3(t * len * 0.5f, deck - (deck - bottom) * Mathf.Cos(th), half * Mathf.Sin(th));
                }
            }
            Color plank = c.Wood;
            for (int i = 0; i < stations; i++)
                for (int j = 0; j < around; j++)
                {
                    bool hole = j >= around / 2 && i >= 3 && i <= 6 && rnd.NextDouble() < 0.65;
                    if (hole) continue;
                    Color col = plank * (0.85f + 0.15f * ((i + j) % 3) / 2f);
                    Vector3 a = pts[i, j], b = pts[i + 1, j], cc = pts[i + 1, j + 1], d = pts[i, j + 1];
                    k.Quad(a, b, cc, d, col * 0.8f); // inside face
                    k.Quad(d, cc, b, a, col);
                }
            // Ribs, the keel, a snapped mast.
            for (int i = 1; i < stations; i++)
                for (int j = 0; j < around; j++)
                    k.Limb(pts[i, j], pts[i, j + 1], 0.12f, 0.12f, plank * 0.6f, 4);
            k.Limb(pts[0, around / 2] + Vector3.down * 0.1f, pts[stations, around / 2] + Vector3.down * 0.1f, 0.25f, 0.2f, plank * 0.5f, 5);
            k.Limb(new Vector3(len * 0.08f, 0.6f, 0f), new Vector3(len * 0.08f, h + 1.6f, 0f), 0.3f, 0.26f, plank * 0.7f, 6);
            k.Limb(new Vector3(len * 0.12f, h + 0.3f, beam * 0.3f), new Vector3(len * 0.25f, 0.4f, beam * 1.6f), 0.24f, 0.2f, plank * 0.7f, 6);
            k.Ball(new Vector3(-len * 0.2f, 0.3f, beam * 0.45f), new Vector3(1.4f, 0.4f, 0.6f), c.Growth, 7, 3, null, 0.3f, 5);
        }

        /// <summary>A round tower broken off at an angle, with a door, slit windows and fallen stones.</summary>
        static void Tower(MeshKit k, DressingColors c, float r, float h, Shade S, System.Func<float, float, float> R)
        {
            const int sides = 12;
            float inner = r - 0.7f;
            var tops = new float[sides];
            for (int i = 0; i < sides; i++)
            {
                float a = i * Mathf.PI * 2f / sides;
                tops[i] = h * (0.62f + 0.38f * (0.5f + 0.5f * Mathf.Cos(a - 0.6f))) + R(-0.6f, 0.3f);
            }
            for (int i = 0; i < sides; i++)
            {
                int n = (i + 1) % sides;
                float a0 = i * Mathf.PI * 2f / sides, a1 = n * Mathf.PI * 2f / sides;
                var d0 = new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0));
                var d1 = new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1));
                Color col = S(0.84f, 1f);
                // Outer and inner faces, then the broken rim between them.
                k.Quad(d0 * r, d0 * r + Vector3.up * tops[i], d1 * r + Vector3.up * tops[n], d1 * r, col);
                k.Quad(d1 * inner, d1 * inner + Vector3.up * tops[n], d0 * inner + Vector3.up * tops[i], d0 * inner, col * 0.7f);
                k.Quad(d0 * inner + Vector3.up * tops[i], d1 * inner + Vector3.up * tops[n], d1 * r + Vector3.up * tops[n], d0 * r + Vector3.up * tops[i], col * 1.05f);
                for (float y = 1.4f; y < tops[i] - 0.3f; y += 1.6f)
                    k.Box(Vector3.Lerp(d0, d1, 0.5f) * (r + 0.02f) + Vector3.up * y, new Vector3(0.08f, 0.08f, 0.08f), col * 0.8f);
            }
            k.Prism(new Vector3(0f, 0.05f, 0f), inner, 0.1f, sides, c.Dark);
            k.Box(new Vector3(0f, 1.1f, r + 0.02f), new Vector3(1.2f, 2.2f, 0.06f), c.Dark);
            for (int i = 0; i < 3; i++)
            {
                float a = 1.2f + i * 1.9f;
                k.Box(new Vector3(Mathf.Cos(a) * (r + 0.02f), h * 0.45f + i * 1.3f, Mathf.Sin(a) * (r + 0.02f)), new Vector3(0.25f, 1f, 0.25f), c.Dark, 1f, Quaternion.Euler(0f, -a * Mathf.Rad2Deg, 0f));
            }
            for (int i = 0; i < 6; i++)
            {
                float a = R(0f, Mathf.PI * 2f), dist = r + R(0.6f, 2.5f), s = R(0.5f, 1f);
                k.Block(new Vector3(Mathf.Cos(a) * dist, s * 0.3f, Mathf.Sin(a) * dist), new Vector3(s * 1.2f, s * 0.6f, s * 0.8f), S(0.8f, 0.95f), 0.3f, Quaternion.Euler(R(-15f, 15f), R(0f, 180f), 0f));
            }
            k.Ball(new Vector3(0f, 0.1f, 0f) + new Vector3(Mathf.Cos(0.6f), 0f, Mathf.Sin(0.6f)) * (r + 0.1f), new Vector3(0.9f, 1.4f, 0.9f), c.Growth, 7, 4, null, 0.3f, 4);
        }
    }
}
