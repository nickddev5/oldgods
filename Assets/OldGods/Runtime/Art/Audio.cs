using System;
using System.Collections.Generic;
using UnityEngine;

namespace OldGods.Runtime
{
    public enum Sfx { Hit, Kill, Pickup, Gold, LevelUp, Hurt, Slam, BossWake, Portal, Chest, Click, Shrine, Death }

    /// <summary>
    /// Every sound in the game, synthesized at startup from simple waves and noise, so the
    /// audio is made for this project and licence-safe. One music drone per biome.
    /// Rate-limits busy sounds (hits, kills, pickups) so a big horde does not roar.
    /// </summary>
    public sealed class Audio : MonoBehaviour
    {
        const int Rate = 44100;
        static Audio instance;

        readonly Dictionary<Sfx, AudioClip> clips = new Dictionary<Sfx, AudioClip>();
        readonly Dictionary<Sfx, float> nextAllowed = new Dictionary<Sfx, float>();
        readonly Dictionary<string, AudioClip> music = new Dictionary<string, AudioClip>();
        readonly List<AudioSource> voices = new List<AudioSource>();
        AudioSource musicA, musicB;
        int voice;
        string currentMusic;
        System.Random rng = new System.Random(5);

        public static float Master = 0.8f, MusicVolume = 0.6f, SfxVolume = 0.8f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            if (instance != null || Application.isBatchMode) return;
            var go = new GameObject("Audio");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<Audio>();
        }

        void Awake()
        {
            for (int i = 0; i < 16; i++)
            {
                var s = gameObject.AddComponent<AudioSource>();
                s.playOnAwake = false;
                voices.Add(s);
            }
            musicA = gameObject.AddComponent<AudioSource>();
            musicB = gameObject.AddComponent<AudioSource>();
            foreach (var m in new[] { musicA, musicB }) { m.loop = true; m.playOnAwake = false; m.volume = 0f; }
            Build();
            ApplySettings(SaveStore.Current.settings);
        }

        public static void ApplySettings(OldGods.Rules.Settings s)
        {
            Master = s.masterVolume;
            MusicVolume = s.musicVolume;
            SfxVolume = s.sfxVolume;
            AudioListener.volume = Master;
        }

        public static void Play(Sfx sfx, float volume = 1f, float pitchJitter = 0.06f)
        {
            if (instance == null) return;
            instance.PlayInternal(sfx, volume, pitchJitter);
        }

        void PlayInternal(Sfx sfx, float volume, float jitter)
        {
            float now = Time.unscaledTime;
            float gap = sfx == Sfx.Hit ? 0.035f : sfx == Sfx.Kill ? 0.05f : sfx == Sfx.Pickup || sfx == Sfx.Gold ? 0.04f : 0f;
            if (nextAllowed.TryGetValue(sfx, out float t) && now < t) return;
            nextAllowed[sfx] = now + gap;
            if (!clips.TryGetValue(sfx, out var clip)) return;
            var src = voices[voice];
            voice = (voice + 1) % voices.Count;
            src.clip = clip;
            src.volume = volume * SfxVolume;
            src.pitch = 1f + (float)(rng.NextDouble() * 2 - 1) * jitter;
            src.Play();
        }

        /// <summary>Crossfades to a biome's drone; key is the biome id or "menu".</summary>
        public static void Music(string key)
        {
            if (instance == null) return;
            instance.MusicInternal(key);
        }

        void MusicInternal(string key)
        {
            if (key == currentMusic) return;
            currentMusic = key;
            if (!music.TryGetValue(key, out var clip)) clip = music.TryGetValue("menu", out var m) ? m : null;
            if (clip == null) return;
            (musicA, musicB) = (musicB, musicA);
            musicA.clip = clip;
            musicA.volume = 0f;
            musicA.Play();
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            musicA.volume = Mathf.MoveTowards(musicA.volume, 0.5f * MusicVolume, dt * 0.4f);
            musicB.volume = Mathf.MoveTowards(musicB.volume, 0f, dt * 0.4f);
            if (musicB.volume <= 0f && musicB.isPlaying) musicB.Stop();
        }

        // ---- Synthesis ----

        delegate float Wave(float t, float progress);

        AudioClip Make(string name, float seconds, Wave wave)
        {
            int n = Mathf.Max(1, (int)(seconds * Rate));
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                data[i] = Mathf.Clamp(wave(t, i / (float)n), -1f, 1f);
            }
            var clip = AudioClip.Create(name, n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        float noiseState;
        float Noise() => (float)(rng.NextDouble() * 2.0 - 1.0);
        float Smooth(float amount)
        {
            noiseState += (Noise() - noiseState) * amount;
            return noiseState;
        }

        static float Sine(float f, float t) => Mathf.Sin(2f * Mathf.PI * f * t);
        static float Env(float p, float attack, float power) => p < attack ? p / attack : Mathf.Pow(1f - (p - attack) / (1f - attack), power);

        void Build()
        {
            clips[Sfx.Hit] = Make("hit", 0.05f, (t, p) => Smooth(0.5f) * Env(p, 0.02f, 3f) * 0.5f);
            clips[Sfx.Kill] = Make("kill", 0.12f, (t, p) => Sine(Mathf.Lerp(140f, 55f, p), t) * Env(p, 0.02f, 2f) * 0.55f + Smooth(0.2f) * Env(p, 0.01f, 4f) * 0.2f);
            clips[Sfx.Pickup] = Make("pickup", 0.07f, (t, p) => Sine(Mathf.Lerp(1100f, 1600f, p), t) * Env(p, 0.05f, 2f) * 0.25f);
            clips[Sfx.Gold] = Make("gold", 0.16f, (t, p) => (Sine(1568f, t) + Sine(2093f, t) * (p > 0.4f ? 1f : 0f)) * Env(p, 0.02f, 2f) * 0.18f);
            clips[Sfx.LevelUp] = Make("levelup", 0.6f, (t, p) =>
            {
                float f = p < 0.33f ? 523f : p < 0.66f ? 659f : 784f;
                return (Sine(f, t) + 0.4f * Sine(f * 2f, t)) * Env(p, 0.02f, 1.2f) * 0.3f;
            });
            clips[Sfx.Hurt] = Make("hurt", 0.16f, (t, p) => Mathf.Sign(Sine(Mathf.Lerp(110f, 70f, p), t)) * Env(p, 0.02f, 2f) * 0.25f);
            clips[Sfx.Slam] = Make("slam", 0.5f, (t, p) => (Sine(Mathf.Lerp(80f, 35f, p), t) * 0.7f + Smooth(0.08f) * 0.6f) * Env(p, 0.01f, 2.5f) * 0.8f);
            clips[Sfx.BossWake] = Make("bosswake", 2f, (t, p) =>
                (Sine(55f, t) + 0.6f * Sine(82.4f, t) + 0.3f * Sine(110f, t) + 0.3f * Smooth(0.03f)) * Env(p, 0.3f, 1.5f) * 0.4f);
            clips[Sfx.Portal] = Make("portal", 1.4f, (t, p) =>
                (Sine(Mathf.Lerp(330f, 660f, p), t) * 0.5f + Sine(Mathf.Lerp(495f, 990f, p), t) * 0.3f) * Env(p, 0.2f, 1.5f) * 0.3f);
            clips[Sfx.Chest] = Make("chest", 0.5f, (t, p) => (Sine(p < 0.4f ? 659f : 988f, t) + 0.3f * Sine(1318f, t)) * Env(p, 0.01f, 1.4f) * 0.28f);
            clips[Sfx.Click] = Make("click", 0.03f, (t, p) => Smooth(0.7f) * Env(p, 0.05f, 4f) * 0.3f);
            clips[Sfx.Shrine] = Make("shrine", 1.2f, (t, p) => (Sine(392f, t) + Sine(587f, t) * 0.7f + Sine(784f, t) * 0.4f) * Env(p, 0.1f, 1.6f) * 0.25f);
            clips[Sfx.Death] = Make("death", 1.6f, (t, p) => (Sine(Mathf.Lerp(220f, 55f, p), t) * 0.6f + Smooth(0.05f) * 0.3f) * Env(p, 0.05f, 1.2f) * 0.5f);

            music["menu"] = Drone("menu", 65.4f, new[] { 1f, 1.5f, 2f, 3f }, 0.02f, 0.05f);
            music["biome.grey_steppe"] = Drone("steppe", 73.4f, new[] { 1f, 1.5f, 2f, 2.5f }, 0.05f, 0.12f);
            music["biome.ash_wood"] = Drone("ashwood", 55f, new[] { 1f, 1.2f, 1.5f, 2f }, 0.03f, 0.2f);
            music["biome.drowned_coast"] = Drone("coast", 82.4f, new[] { 1f, 1.5f, 1.875f, 3f }, 0.08f, 0.25f);
            music["biome.last_test"] = Drone("lasttest", 49f, new[] { 1f, 1.5f, 2f, 2.997f, 4f }, 0.02f, 0.06f);
            music["biome.greybox"] = music["biome.grey_steppe"];
        }

        /// <summary>A 16-second looping drone: harmonics over a root with slow swells, plus soft wind.</summary>
        AudioClip Drone(string name, float root, float[] ratios, float wind, float shimmer)
        {
            const float seconds = 16f;
            return Make("music_" + name, seconds, (t, p) =>
            {
                float s = 0f;
                for (int i = 0; i < ratios.Length; i++)
                {
                    // Swell rates are whole cycles per loop so the loop is seamless.
                    float swell = 0.55f + 0.45f * Mathf.Sin(2f * Mathf.PI * (t / seconds) * (i + 1) + i);
                    s += Sine(root * ratios[i], t) * swell / (i + 1.5f);
                }
                s += shimmer * Sine(root * 8f, t) * Mathf.Max(0f, Mathf.Sin(2f * Mathf.PI * t / seconds * 3f)) * 0.3f;
                s += wind * Smooth(0.02f) * 3f;
                // Fade the ends a little so the loop point does not click.
                float edge = Mathf.Min(1f, Mathf.Min(t, seconds - t) * 20f);
                return s * 0.18f * edge;
            });
        }
    }
}
