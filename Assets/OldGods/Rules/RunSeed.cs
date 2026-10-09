using System;
using System.Collections.Generic;

namespace OldGods.Rules
{
    /// <summary>
    /// Deterministic random generator (xoshiro256**) seeded through SplitMix64.
    /// One instance per stream; never shared between systems.
    /// </summary>
    public sealed class Rng
    {
        ulong s0, s1, s2, s3;

        public Rng(ulong seed)
        {
            ulong x = seed;
            s0 = SplitMix(ref x);
            s1 = SplitMix(ref x);
            s2 = SplitMix(ref x);
            s3 = SplitMix(ref x);
        }

        static ulong SplitMix(ref ulong x)
        {
            x += 0x9E3779B97F4A7C15UL;
            ulong z = x;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }

        static ulong Rotl(ulong x, int k) => (x << k) | (x >> (64 - k));

        public ulong NextULong()
        {
            ulong result = Rotl(s1 * 5, 7) * 9;
            ulong t = s1 << 17;
            s2 ^= s0;
            s3 ^= s1;
            s1 ^= s2;
            s0 ^= s3;
            s2 ^= t;
            s3 = Rotl(s3, 45);
            return result;
        }

        /// <summary>Uniform float in [0, 1).</summary>
        public float NextFloat() => (NextULong() >> 40) * (1f / (1 << 24));

        /// <summary>Uniform int in [minInclusive, maxExclusive).</summary>
        public int Range(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive) return minInclusive;
            ulong span = (ulong)((long)maxExclusive - minInclusive);
            return (int)((long)minInclusive + (long)(NextULong() % span));
        }

        public float Range(float min, float max) => min + (max - min) * NextFloat();

        public bool Chance(float probability) => NextFloat() < probability;

        /// <summary>Index into weights, chosen with probability proportional to weight. -1 if all weights are zero.</summary>
        public int PickWeighted(IReadOnlyList<float> weights)
        {
            float total = 0f;
            for (int i = 0; i < weights.Count; i++) total += Math.Max(0f, weights[i]);
            if (total <= 0f) return -1;
            float roll = NextFloat() * total;
            for (int i = 0; i < weights.Count; i++)
            {
                float w = Math.Max(0f, weights[i]);
                if (roll < w) return i;
                roll -= w;
            }
            for (int i = weights.Count - 1; i >= 0; i--)
                if (weights[i] > 0f) return i;
            return -1;
        }

        public void Shuffle<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }

    /// <summary>
    /// The one seed of a run. Systems never share a generator: each asks for its
    /// own named stream, so adding rolls to one system never shifts another.
    /// </summary>
    public sealed class RunSeed
    {
        public const string Map = "map";
        public const string Spawns = "spawns";
        public const string Draft = "draft";
        public const string Loot = "loot";
        public const string Shrines = "shrines";

        public ulong Value { get; }

        public RunSeed(ulong value) { Value = value; }

        public Rng Stream(string name) => new Rng(Value ^ Hash(name));

        /// <summary>A stream for a sub-part, such as the map of stage 2.</summary>
        public Rng Stream(string name, int index) => new Rng(Value ^ Hash(name) ^ ((ulong)(uint)index * 0xD6E8FEB86659FD93UL));

        /// <summary>FNV-1a 64-bit, stable across platforms and runtimes.</summary>
        public static ulong Hash(string text)
        {
            ulong h = 0xCBF29CE484222325UL;
            foreach (char c in text)
            {
                h ^= c;
                h *= 0x100000001B3UL;
            }
            return h;
        }

        public override string ToString() => Value.ToString("X16");

        public static bool TryParse(string text, out RunSeed seed)
        {
            seed = null;
            if (string.IsNullOrWhiteSpace(text)) return false;
            if (!ulong.TryParse(text.Trim(), System.Globalization.NumberStyles.HexNumber, null, out ulong v)) return false;
            seed = new RunSeed(v);
            return true;
        }

        public static RunSeed FromEntropy(long ticks, int salt)
        {
            ulong x = (ulong)ticks ^ ((ulong)(uint)salt << 32);
            return new RunSeed(new Rng(x).NextULong());
        }
    }
}
