using UnityEngine;

namespace OldGods.Runtime
{
    /// <summary>
    /// The pixel-art detail texture every low-poly surface is shaded with: 128 by 128 tones,
    /// point-filtered, generated in code from a fixed seed so it is ours and identical on every
    /// machine. The shaders project it in object space (no UVs needed) and multiply it into
    /// the vertex colour, so models keep their colour blocks and gain coarse texels.
    /// Channels: R fine grain with clumps (characters, cloth, skin), G ground (clumps of
    /// tone with lighter blade tips), B streaked grain (bark, rock faces, planks).
    /// Mipmaps average the texels to mid-grey far away, so distant ground does not shimmer.
    /// </summary>
    public static class PixelTexture
    {
        public const int Size = 128;
        public static readonly int GlobalId = Shader.PropertyToID("_OG_PixelTex");

        static Texture2D texture;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Bind()
        {
            if (texture == null) texture = Build();
            Shader.SetGlobalTexture(GlobalId, texture);
        }

        public static Texture2D Build()
        {
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, true, true)
            {
                name = "Old Gods Pixel Detail",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Repeat,
                anisoLevel = 0,
                hideFlags = HideFlags.DontSave,
            };
            tex.SetPixels32(Pixels());
            tex.Apply(true, true);
            return tex;
        }

        /// <summary>The texture's texels, row by row; pure and seeded, so tests can check them.</summary>
        public static Color32[] Pixels()
        {
            var px = new Color32[Size * Size];
            var blade = new float[Size * Size];
            var rng = new System.Random(20261009);
            // Grass blade tips: a light texel with a darker one under it, scattered.
            for (int i = 0; i < Size * Size / 14; i++)
            {
                int x = rng.Next(Size), y = rng.Next(Size);
                blade[y * Size + x] = 1f;
                blade[Wrap(y - 1) * Size + x] = -0.8f;
            }
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                {
                    float grain = Hash(x, y, 1);
                    float clump = Smooth(x, y, 8, 8, 2);
                    float r = Quantize(0.55f * grain + 0.45f * clump, 4);

                    float patch = Smooth(x, y, 16, 16, 3);
                    float g = Quantize(0.45f * patch + 0.55f * Hash(x, y, 4), 5);
                    g = Mathf.Clamp01(g + blade[y * Size + x] * 0.3f);

                    float streak = Smooth(x, y, 2, 8, 5);
                    float b = Quantize(0.65f * streak + 0.35f * grain, 4);

                    px[y * Size + x] = new Color32(Byte(r), Byte(g), Byte(b), 255);
                }
            return px;
        }

        static int Wrap(int v) => (v % Size + Size) % Size;

        static byte Byte(float v) => (byte)Mathf.RoundToInt(Mathf.Clamp01(v) * 255f);

        /// <summary>Snaps 0..1 to a few even tones, as a pixel artist's ramp would.</summary>
        public static float Quantize(float v, int levels) =>
            Mathf.Clamp01(Mathf.Round(Mathf.Clamp01(v) * (levels - 1)) / (levels - 1));

        static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                uint h = (uint)(Wrap(x) * 374761393 + Wrap(y) * 668265263 + seed * 2147483647);
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return (h & 0xffffff) / (float)0xffffff;
            }
        }

        /// <summary>
        /// Tileable smooth value noise with cells of cellX by cellY texels (each a divisor of
        /// Size), so the lattice wraps exactly at the texture's edge.
        /// </summary>
        static float Smooth(int x, int y, int cellX, int cellY, int seed)
        {
            int px = Size / cellX, py = Size / cellY;
            int x0 = x / cellX, y0 = y / cellY;
            float tx = (x % cellX) / (float)cellX, ty = (y % cellY) / (float)cellY;
            tx = tx * tx * (3f - 2f * tx);
            ty = ty * ty * (3f - 2f * ty);
            float a = Hash(x0 % px, y0 % py, seed), b = Hash((x0 + 1) % px, y0 % py, seed);
            float c = Hash(x0 % px, (y0 + 1) % py, seed), d = Hash((x0 + 1) % px, (y0 + 1) % py, seed);
            return Mathf.Lerp(Mathf.Lerp(a, b, tx), Mathf.Lerp(c, d, tx), ty);
        }
    }
}
