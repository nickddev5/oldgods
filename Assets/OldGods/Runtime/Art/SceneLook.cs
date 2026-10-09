using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace OldGods.Runtime
{
    /// <summary>
    /// Screen-wide grading, built in code so a fresh clone needs no profile asset: a soft
    /// bloom so glowing effects and pickups bloom, punchier colour, and a light vignette that
    /// keeps the eye on the player. Neutral tonemapping keeps bright colours from clipping
    /// without the dark, desaturated cast of ACES.
    /// </summary>
    public static class SceneLook
    {
        static Volume volume;

        public static void Ensure()
        {
            if (volume != null) return;
            var go = new GameObject("Scene Look");
            volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 10f;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = "Scene Look (runtime)";

            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(0.95f);
            bloom.intensity.Override(0.55f);
            bloom.scatter.Override(0.6f);
            bloom.highQualityFiltering.Override(false);

            var tone = profile.Add<Tonemapping>(true);
            tone.mode.Override(TonemappingMode.Neutral);

            var grade = profile.Add<ColorAdjustments>(true);
            grade.postExposure.Override(0.15f);
            grade.contrast.Override(10f);
            grade.saturation.Override(10f);

            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(0.22f);
            vignette.smoothness.Override(0.45f);

            volume.sharedProfile = profile;
        }
    }
}
