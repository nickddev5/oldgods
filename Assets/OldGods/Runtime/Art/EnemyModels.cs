using System.Collections.Generic;
using UnityEngine;

namespace OldGods.Runtime
{
    public enum EnemyModel { Husk, Runner, Brute, Ghost, Ashling, Cinder, Hulk, Drowned, BrineRunner, ShellBrute, Champion, AshChampion, TideChampion }

    /// <summary>
    /// Low-poly enemy bodies built in code (made for this project, so licence-safe). All face +z
    /// with feet at y = 0. Legs sit at |x| > 0.05 and below y = 0.7 so the horde shader can swing
    /// them; anything that should not swing stays above the hip or on the centre line.
    /// </summary>
    public static class EnemyModels
    {
        static readonly Dictionary<EnemyModel, Mesh> cache = new Dictionary<EnemyModel, Mesh>();

        static readonly Color Skin = new Color(0.85f, 0.85f, 0.85f);
        static readonly Color Dark = new Color(0.55f, 0.55f, 0.55f);
        static readonly Color Pale = new Color(1f, 1f, 1f);
        static readonly Color Black = new Color(0.3f, 0.3f, 0.3f);

        public static Mesh Get(EnemyModel model)
        {
            if (cache.TryGetValue(model, out var m) && m != null) return m;
            var k = new MeshKit();
            switch (model)
            {
                case EnemyModel.Husk: Husk(k, 1f); break;
                case EnemyModel.Runner: Runner(k); break;
                case EnemyModel.Brute: Brute(k); break;
                case EnemyModel.Ghost: Ghost(k); break;
                case EnemyModel.Ashling: Ashling(k); break;
                case EnemyModel.Cinder: Husk(k, 1f); Horns(k, 1.55f); break;
                case EnemyModel.Hulk: Hulk(k); break;
                case EnemyModel.Drowned: Drowned(k); break;
                case EnemyModel.BrineRunner: BrineRunner(k); break;
                case EnemyModel.ShellBrute: Brute(k); Shell(k); break;
                case EnemyModel.Champion: Husk(k, 1f); Helm(k); Blade(k); break;
                case EnemyModel.AshChampion: Hulk(k); Horns(k, 1.6f); break;
                case EnemyModel.TideChampion: Drowned(k); Shell(k); Helm(k); break;
            }
            m = k.Build("Enemy" + model);
            cache[model] = m;
            return m;
        }

        static void Legs(MeshKit k, float width, float height, float thick)
        {
            k.Box(new Vector3(-width, height * 0.5f, 0f), new Vector3(thick, height, thick * 1.1f), Dark, 0.9f);
            k.Box(new Vector3(width, height * 0.5f, 0f), new Vector3(thick, height, thick * 1.1f), Dark, 0.9f);
        }

        static void Husk(MeshKit k, float s)
        {
            Legs(k, 0.16f, 0.7f, 0.18f);
            k.Box(new Vector3(0f, 1.02f, 0.04f), new Vector3(0.56f, 0.66f, 0.34f) * s, Skin, 1.25f, Quaternion.Euler(12f, 0f, 0f));
            k.Box(new Vector3(-0.38f, 0.95f, 0.12f), new Vector3(0.14f, 0.6f, 0.14f), Dark, 0.8f, Quaternion.Euler(-35f, 0f, 8f));
            k.Box(new Vector3(0.38f, 0.95f, 0.12f), new Vector3(0.14f, 0.6f, 0.14f), Dark, 0.8f, Quaternion.Euler(-35f, 0f, -8f));
            k.Gem(new Vector3(0f, 1.48f, 0.14f), new Vector3(0.17f, 0.17f, 0.17f), Skin);
        }

        static void Runner(MeshKit k)
        {
            Legs(k, 0.12f, 0.7f, 0.12f);
            k.Box(new Vector3(0f, 0.95f, 0.18f), new Vector3(0.36f, 0.5f, 0.24f), Skin, 1.1f, Quaternion.Euler(40f, 0f, 0f));
            k.Box(new Vector3(-0.25f, 0.9f, 0.3f), new Vector3(0.08f, 0.5f, 0.08f), Dark, 0.8f, Quaternion.Euler(-70f, 0f, 0f));
            k.Box(new Vector3(0.25f, 0.9f, 0.3f), new Vector3(0.08f, 0.5f, 0.08f), Dark, 0.8f, Quaternion.Euler(-70f, 0f, 0f));
            k.Gem(new Vector3(0f, 1.18f, 0.42f), new Vector3(0.13f, 0.13f, 0.18f), Skin);
        }

        static void Brute(MeshKit k)
        {
            Legs(k, 0.2f, 0.6f, 0.24f);
            k.Box(new Vector3(0f, 1f, 0f), new Vector3(0.85f, 0.75f, 0.5f), Skin, 1.3f);
            k.Box(new Vector3(-0.58f, 0.85f, 0.08f), new Vector3(0.26f, 0.75f, 0.26f), Dark, 1.3f, Quaternion.Euler(-15f, 0f, 10f));
            k.Box(new Vector3(0.58f, 0.85f, 0.08f), new Vector3(0.26f, 0.75f, 0.26f), Dark, 1.3f, Quaternion.Euler(-15f, 0f, -10f));
            k.Gem(new Vector3(0f, 1.48f, 0.12f), new Vector3(0.16f, 0.14f, 0.16f), Skin);
        }

        static void Ghost(MeshKit k)
        {
            // A drifting robe: no legs on the swing line, a hood and trailing sleeves.
            k.Prism(new Vector3(0f, 0.15f, 0f), 0.36f, 1.1f, 6, Pale, 0.55f);
            k.Box(new Vector3(0f, 1.38f, 0f), new Vector3(0.36f, 0.4f, 0.36f), Pale, 0.6f);
            k.Box(new Vector3(-0.3f, 1.05f, 0.18f), new Vector3(0.1f, 0.45f, 0.1f), Skin, 0.5f, Quaternion.Euler(-60f, 0f, 0f));
            k.Box(new Vector3(0.3f, 1.05f, 0.18f), new Vector3(0.1f, 0.45f, 0.1f), Skin, 0.5f, Quaternion.Euler(-60f, 0f, 0f));
            k.Box(new Vector3(0f, 1.35f, 0.17f), new Vector3(0.2f, 0.06f, 0.04f), Black);
        }

        static void Ashling(MeshKit k)
        {
            Legs(k, 0.1f, 0.45f, 0.1f);
            k.Gem(new Vector3(0f, 0.7f, 0.05f), new Vector3(0.28f, 0.26f, 0.32f), Skin);
            for (int i = -1; i <= 1; i++)
                k.Box(new Vector3(i * 0.12f, 0.98f, -0.05f), new Vector3(0.05f, 0.25f, 0.05f), Black, 0.2f, Quaternion.Euler(-20f, 0f, i * -25f));
            k.Gem(new Vector3(0f, 0.78f, 0.3f), new Vector3(0.08f, 0.08f, 0.06f), Pale);
        }

        static void Horns(MeshKit k, float y)
        {
            k.Box(new Vector3(-0.14f, y + 0.08f, 0.1f), new Vector3(0.05f, 0.24f, 0.05f), Black, 0.2f, Quaternion.Euler(-10f, 0f, 35f));
            k.Box(new Vector3(0.14f, y + 0.08f, 0.1f), new Vector3(0.05f, 0.24f, 0.05f), Black, 0.2f, Quaternion.Euler(-10f, 0f, -35f));
        }

        static void Hulk(MeshKit k)
        {
            Legs(k, 0.22f, 0.55f, 0.26f);
            k.Box(new Vector3(0f, 1.05f, 0.08f), new Vector3(0.95f, 0.8f, 0.6f), Skin, 1.2f, Quaternion.Euler(15f, 0f, 0f));
            k.Box(new Vector3(-0.66f, 0.75f, 0.2f), new Vector3(0.32f, 0.95f, 0.32f), Dark, 1.4f, Quaternion.Euler(-10f, 0f, 6f));
            k.Box(new Vector3(0.66f, 0.75f, 0.2f), new Vector3(0.32f, 0.95f, 0.32f), Dark, 1.4f, Quaternion.Euler(-10f, 0f, -6f));
            k.Gem(new Vector3(0f, 1.5f, 0.3f), new Vector3(0.15f, 0.13f, 0.15f), Skin);
        }

        static void Drowned(MeshKit k)
        {
            Legs(k, 0.15f, 0.68f, 0.17f);
            k.Box(new Vector3(0f, 1f, 0.1f), new Vector3(0.52f, 0.62f, 0.32f), Skin, 1.1f, Quaternion.Euler(25f, 0f, 0f));
            k.Box(new Vector3(-0.34f, 0.8f, 0.2f), new Vector3(0.12f, 0.7f, 0.12f), Dark, 0.7f, Quaternion.Euler(-10f, 0f, 5f));
            k.Box(new Vector3(0.34f, 0.8f, 0.2f), new Vector3(0.12f, 0.7f, 0.12f), Dark, 0.7f, Quaternion.Euler(-10f, 0f, -5f));
            k.Gem(new Vector3(0f, 1.42f, 0.24f), new Vector3(0.16f, 0.18f, 0.16f), Skin);
            for (int i = 0; i < 4; i++)
                k.Box(new Vector3(-0.15f + i * 0.1f, 1.18f, -0.12f), new Vector3(0.04f, 0.5f, 0.04f), Black, 0.5f, Quaternion.Euler(10f, 0f, 0f));
        }

        static void BrineRunner(MeshKit k)
        {
            // Low and wide, legs splayed.
            k.Box(new Vector3(-0.24f, 0.3f, 0f), new Vector3(0.1f, 0.6f, 0.1f), Dark, 0.8f, Quaternion.Euler(0f, 0f, 20f));
            k.Box(new Vector3(0.24f, 0.3f, 0f), new Vector3(0.1f, 0.6f, 0.1f), Dark, 0.8f, Quaternion.Euler(0f, 0f, -20f));
            k.Box(new Vector3(0f, 0.75f, 0.1f), new Vector3(0.7f, 0.3f, 0.6f), Skin, 0.8f);
            k.Box(new Vector3(-0.42f, 0.8f, 0.42f), new Vector3(0.12f, 0.12f, 0.45f), Dark, 1f, Quaternion.Euler(0f, 25f, 0f));
            k.Box(new Vector3(0.42f, 0.8f, 0.42f), new Vector3(0.12f, 0.12f, 0.45f), Dark, 1f, Quaternion.Euler(0f, -25f, 0f));
            k.Gem(new Vector3(0f, 0.95f, 0.38f), new Vector3(0.1f, 0.08f, 0.1f), Pale);
        }

        static void Shell(MeshKit k)
        {
            k.Gem(new Vector3(0f, 1.25f, -0.3f), new Vector3(0.55f, 0.45f, 0.35f), Pale);
            k.Box(new Vector3(0f, 1.25f, -0.32f), new Vector3(0.06f, 0.7f, 0.5f), Black);
        }

        static void Helm(MeshKit k)
        {
            k.Box(new Vector3(0f, 1.6f, 0.12f), new Vector3(0.34f, 0.26f, 0.34f), Pale, 0.8f);
            k.Box(new Vector3(0f, 1.82f, 0.08f), new Vector3(0.06f, 0.24f, 0.4f), Black, 0.5f);
        }

        static void Blade(MeshKit k)
        {
            k.Box(new Vector3(0.48f, 1.1f, 0.5f), new Vector3(0.08f, 0.08f, 1.1f), Pale, 1f, Quaternion.Euler(-25f, 0f, 0f));
        }
    }
}
