using System.Collections.Generic;
using UnityEngine;

namespace OldGods.Runtime
{
    /// <summary>Shared effect materials and meshes, cached by colour.</summary>
    public static class Fx
    {
        static readonly Dictionary<(Color, bool), Material> cache = new Dictionary<(Color, bool), Material>();
        static Material glowBase, fadeBase;
        static readonly Dictionary<string, Mesh> meshes = new Dictionary<string, Mesh>();

        public static void Init(GameAssets assets)
        {
            glowBase = assets.UnlitGlow;
            fadeBase = assets.UnlitFade;
        }

        /// <summary>Opaque unlit material in this colour (HDR colours glow under bloom).</summary>
        public static Material Glow(Color c) => Get(c, false);

        /// <summary>Alpha-blended unlit material; set alpha with the colour.</summary>
        public static Material Fade(Color c) => Get(c, true);

        static Material Get(Color c, bool fade)
        {
            if (cache.TryGetValue((c, fade), out var m) && m != null) return m;
            var src = fade ? fadeBase : glowBase;
            if (src == null)
            {
                var sh = Shader.Find("OldGods/UnlitGlow");
                src = new Material(sh);
            }
            m = new Material(src) { name = (fade ? "Fade_" : "Glow_") + ColorUtility.ToHtmlStringRGBA(c) };
            m.SetColor("_BaseColor", c);
            m.enableInstancing = true;
            cache[(c, fade)] = m;
            return m;
        }

        public static void ClearCache() => cache.Clear();

        /// <summary>A flat ring on the ground plane, inner to outer radius 1.</summary>
        public static Mesh Ring(float inner = 0.85f, int segments = 40) => Cached($"ring{inner}{segments}", () =>
        {
            var k = new MeshKit();
            for (int i = 0; i < segments; i++)
            {
                float a0 = i * Mathf.PI * 2f / segments, a1 = (i + 1) * Mathf.PI * 2f / segments;
                var o0 = new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0));
                var o1 = new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1));
                k.Quad(o0 * inner, o1 * inner, o1, o0, Color.white);
            }
            return k.Build("Ring");
        });

        /// <summary>A flat disc of radius 1 on the ground plane.</summary>
        public static Mesh Disc(int segments = 32) => Cached($"disc{segments}", () =>
        {
            var k = new MeshKit();
            for (int i = 0; i < segments; i++)
            {
                float a0 = i * Mathf.PI * 2f / segments, a1 = (i + 1) * Mathf.PI * 2f / segments;
                k.Triangle(Vector3.zero, new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1)), new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0)), Color.white);
            }
            return k.Build("Disc");
        });

        /// <summary>An open six-sided column of radius 1 and height 1, for strikes and beams.</summary>
        public static Mesh Column() => Cached("column", () =>
        {
            var k = new MeshKit();
            k.Prism(Vector3.zero, 1f, 1f, 6, Color.white, 0.6f);
            return k.Build("Column");
        });

        public static Mesh Cube() => Cached("cube", () =>
        {
            var k = new MeshKit();
            k.Box(Vector3.zero, Vector3.one, Color.white);
            return k.Build("Cube");
        });

        static Mesh Cached(string key, System.Func<Mesh> make)
        {
            if (meshes.TryGetValue(key, out var m) && m != null) return m;
            m = make();
            meshes[key] = m;
            return m;
        }
    }
}
