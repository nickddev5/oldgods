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
        public void ShadersHaveTheOutlineAndNoiseProperties()
        {
            foreach (var name in new[] { "OldGods/LowPoly", "OldGods/HordeInstanced" })
            {
                var shader = Shader.Find(name);
                Assert.IsNotNull(shader, name);
                Assert.GreaterOrEqual(shader.FindPropertyIndex("_OutlineWidth"), 0, name);
                Assert.GreaterOrEqual(shader.FindPropertyIndex("_SurfaceNoise"), 0, name);
            }
            Assert.IsNotNull(Shader.Find("OldGods/Sky"));
        }
    }
}
