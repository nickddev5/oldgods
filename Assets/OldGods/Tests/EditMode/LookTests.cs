using NUnit.Framework;
using OldGods.Runtime;
using UnityEngine;

namespace OldGods.Tests.EditMode
{
    public class LookTests
    {
        [Test]
        public void SkyHorizonIsTheFogSoFarGroundMeltsIntoIt()
        {
            var fog = new Color(0.66f, 0.8f, 0.9f);
            var c = SkyColors.From(new Color(0.6f, 0.74f, 0.9f), fog, new Color(0.3f, 0.28f, 0.22f));
            Assert.AreEqual(fog, c.Horizon);
        }

        [Test]
        public void SkyZenithIsDeeperAndMoreSaturatedThanTheHorizon()
        {
            var c = SkyColors.From(new Color(0.6f, 0.74f, 0.9f), new Color(0.66f, 0.8f, 0.9f), new Color(0.3f, 0.28f, 0.22f));
            Color.RGBToHSV(c.Horizon, out _, out float hs, out float hv);
            Color.RGBToHSV(c.Zenith, out _, out float zs, out float zv);
            Assert.Greater(zs, hs);
            Assert.Less(zv, hv);
        }

        [Test]
        public void CloudsAreLighterThanTheHorizonAndHillsDarker()
        {
            var fog = new Color(0.56f, 0.42f, 0.5f);
            var c = SkyColors.From(new Color(0.62f, 0.46f, 0.56f), fog, new Color(0.25f, 0.17f, 0.15f));
            Assert.Greater(c.Cloud.grayscale, fog.grayscale);
            Assert.Less(c.Hills.grayscale, fog.grayscale);
        }

        [Test]
        public void PixelTextureIsSeededAndUsesFewTones()
        {
            var a = PixelTexture.Pixels();
            var b = PixelTexture.Pixels();
            Assert.AreEqual(PixelTexture.Size * PixelTexture.Size, a.Length);
            CollectionAssert.AreEqual(a, b);
            // The grain channel is a four-tone ramp, like a hand-made pixel texture.
            var tones = new System.Collections.Generic.HashSet<byte>();
            foreach (var p in a) tones.Add(p.r);
            Assert.LessOrEqual(tones.Count, 4);
            Assert.GreaterOrEqual(tones.Count, 3);
        }

        [Test]
        public void PixelTextureTilesWithoutASeam()
        {
            // Opposite edges differ no more than neighbouring columns inside the texture do.
            var px = PixelTexture.Pixels();
            int n = PixelTexture.Size;
            float Edge(int x0, int x1)
            {
                float d = 0;
                for (int y = 0; y < n; y++) d += System.Math.Abs(px[y * n + x0].g - px[y * n + x1].g);
                return d / n;
            }
            float inside = 0;
            for (int x = 0; x < n - 1; x++) inside += Edge(x, x + 1);
            inside /= n - 1;
            Assert.Less(Edge(n - 1, 0), inside * 2f + 1f);
        }

        [Test]
        public void ShadersHaveTheOutlineAndNoiseProperties()
        {
            foreach (var name in new[] { "OldGods/LowPoly", "OldGods/HordeInstanced" })
            {
                var shader = Shader.Find(name);
                Assert.IsNotNull(shader, name);
                Assert.GreaterOrEqual(shader.FindPropertyIndex("_OutlineWidth"), 0, name);
                Assert.GreaterOrEqual(shader.FindPropertyIndex("_PixelAmount"), 0, name);
                Assert.GreaterOrEqual(shader.FindPropertyIndex("_TexelsPerMeter"), 0, name);
            }
            Assert.IsNotNull(Shader.Find("OldGods/Sky"));
        }
    }
}
