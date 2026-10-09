using System.Collections.Generic;
using UnityEngine;

namespace OldGods.Runtime
{
    public enum PropModel { Boulder, StandingStone, SeaStack, Pine, DeadTree, Driftwood }

    /// <summary>Biome props built in code. Rocks are about 1.4 m tall, trees about 4 m.</summary>
    public static class PropModels
    {
        static readonly Dictionary<PropModel, Mesh> cache = new Dictionary<PropModel, Mesh>();

        public static Mesh Get(PropModel model)
        {
            if (cache.TryGetValue(model, out var m) && m != null) return m;
            var k = new MeshKit();
            var c = Color.white;
            var shade = new Color(0.82f, 0.82f, 0.82f);
            var deep = new Color(0.62f, 0.62f, 0.62f);
            var moss = new Color(0.62f, 0.78f, 0.5f);
            switch (model)
            {
                case PropModel.Boulder:
                    k.Ball(new Vector3(0f, 0.55f, 0f), new Vector3(1f, 0.75f, 0.85f), c, 9, 5, null, 0.16f, 11);
                    k.Ball(new Vector3(0.7f, 0.25f, 0.4f), new Vector3(0.42f, 0.32f, 0.38f), shade, 7, 4, null, 0.2f, 12);
                    k.Ball(new Vector3(-0.2f, 1.15f, 0.1f), new Vector3(0.45f, 0.1f, 0.4f), moss, 7, 3, null, 0.25f, 13);
                    break;
                case PropModel.StandingStone:
                    k.Block(new Vector3(0f, 1.4f, 0f), new Vector3(0.8f, 2.8f, 0.42f), c, 0.25f, Quaternion.Euler(0f, 0f, 4f));
                    k.Block(new Vector3(0.02f, 2.88f, 0f), new Vector3(0.62f, 0.22f, 0.36f), shade, 0.4f, Quaternion.Euler(0f, 0f, 9f));
                    // Carved bands and a ring of fallen stones.
                    for (int i = 0; i < 3; i++)
                        k.Box(new Vector3(0f, 0.9f + i * 0.55f, 0.215f), new Vector3(0.5f, 0.05f, 0.02f), deep, 1f, Quaternion.Euler(0f, 0f, 4f));
                    for (int i = 0; i < 4; i++)
                    {
                        float a = i * 1.7f;
                        k.Ball(new Vector3(Mathf.Cos(a) * 0.7f, 0.1f, Mathf.Sin(a) * 0.6f), new Vector3(0.2f, 0.12f, 0.16f), shade, 6, 3, null, 0.2f, 20 + i);
                    }
                    break;
                case PropModel.SeaStack:
                    k.Lathe(Vector3.zero, new[] { new Vector2(0f, 1.15f), new Vector2(0.5f, 1f), new Vector2(1.4f, 0.85f), new Vector2(1.8f, 0.95f), new Vector2(2.6f, 0.6f), new Vector2(2.9f, 0f) }, 7, c, 0.85f);
                    k.Ball(new Vector3(0.1f, 2.5f, 0f), new Vector3(0.7f, 0.18f, 0.6f), moss, 7, 3, null, 0.25f, 31);
                    k.Ball(new Vector3(0.9f, 0.2f, 0.5f), new Vector3(0.4f, 0.3f, 0.35f), shade, 6, 3, null, 0.25f, 32);
                    break;
                case PropModel.Pine:
                    k.Lathe(Vector3.zero, new[] { new Vector2(0f, 0.24f), new Vector2(0.4f, 0.18f), new Vector2(1.6f, 0.12f) }, 7, new Color(0.55f, 0.42f, 0.32f));
                    k.Lathe(new Vector3(0f, 0.8f, 0f), new[] { new Vector2(0f, 1.35f), new Vector2(0.25f, 1.2f), new Vector2(1.3f, 0f) }, 9, c);
                    k.Lathe(new Vector3(0f, 1.7f, 0f), new[] { new Vector2(0f, 1.05f), new Vector2(0.2f, 0.95f), new Vector2(1.15f, 0f) }, 9, shade + new Color(0.08f, 0.08f, 0.08f));
                    k.Lathe(new Vector3(0f, 2.55f, 0f), new[] { new Vector2(0f, 0.72f), new Vector2(0.15f, 0.65f), new Vector2(1.05f, 0f) }, 9, c);
                    break;
                case PropModel.DeadTree:
                    k.Lathe(Vector3.zero, new[] { new Vector2(0f, 0.38f), new Vector2(0.25f, 0.24f), new Vector2(1.6f, 0.18f), new Vector2(3.4f, 0.08f), new Vector2(3.8f, 0f) }, 7, c);
                    void Branch(Vector3 root, Vector3 tip, float r)
                    {
                        k.Limb(root, tip, r, r * 0.3f, shade, 5);
                        k.Limb(Vector3.Lerp(root, tip, 0.6f), Vector3.Lerp(root, tip, 0.6f) + (tip - root).normalized * 0.4f + Vector3.up * 0.35f, r * 0.45f, 0f, shade, 4);
                    }
                    Branch(new Vector3(0f, 2.1f, 0f), new Vector3(1.1f, 3f, 0.2f), 0.09f);
                    Branch(new Vector3(0f, 1.7f, 0f), new Vector3(-0.9f, 2.6f, 0.35f), 0.08f);
                    Branch(new Vector3(0f, 2.7f, 0f), new Vector3(-0.3f, 3.5f, -0.8f), 0.07f);
                    Branch(new Vector3(0f, 1.2f, 0f), new Vector3(0.5f, 1.8f, -0.9f), 0.07f);
                    // Roots.
                    for (int i = 0; i < 4; i++)
                    {
                        float a = i * Mathf.PI / 2f + 0.4f;
                        k.Limb(new Vector3(0f, 0.25f, 0f), new Vector3(Mathf.Cos(a) * 0.75f, -0.05f, Mathf.Sin(a) * 0.75f), 0.12f, 0.03f, deep, 5);
                    }
                    break;
                case PropModel.Driftwood:
                    k.Limb(new Vector3(-1.5f, 0.18f, -0.2f), new Vector3(1.6f, 0.26f, 0.3f), 0.26f, 0.18f, c, 7);
                    k.Limb(new Vector3(0.6f, 0.25f, 0.15f), new Vector3(1.1f, 0.85f, 0.9f), 0.1f, 0.03f, shade, 5);
                    k.Limb(new Vector3(-0.8f, 0.2f, -0.1f), new Vector3(-1.2f, 0.7f, -0.8f), 0.09f, 0.03f, shade, 5);
                    k.Ball(new Vector3(-1.55f, 0.2f, -0.2f), new Vector3(0.32f, 0.3f, 0.3f), deep, 6, 3, null, 0.3f, 41);
                    break;
            }
            m = k.Build("Prop" + model);
            cache[model] = m;
            return m;
        }
    }
}
