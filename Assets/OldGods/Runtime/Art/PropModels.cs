using System.Collections.Generic;
using UnityEngine;

namespace OldGods.Runtime
{
    public enum PropModel { Boulder, StandingStone, SeaStack, Pine, DeadTree, Driftwood }

    /// <summary>Low-poly biome props built in code. Rocks are about 1.4 units tall, trees about 4.</summary>
    public static class PropModels
    {
        static readonly Dictionary<PropModel, Mesh> cache = new Dictionary<PropModel, Mesh>();

        public static Mesh Get(PropModel model)
        {
            if (cache.TryGetValue(model, out var m) && m != null) return m;
            var k = new MeshKit();
            var c = Color.white;
            var shade = new Color(0.8f, 0.8f, 0.8f);
            switch (model)
            {
                case PropModel.Boulder:
                    k.Prism(Vector3.zero, 1f, 1.4f, 6, c, 0.55f);
                    break;
                case PropModel.StandingStone:
                    k.Box(new Vector3(0f, 1.4f, 0f), new Vector3(0.8f, 2.8f, 0.45f), c, 0.75f, Quaternion.Euler(0f, 0f, 4f));
                    k.Box(new Vector3(0f, 0.08f, 0f), new Vector3(1.2f, 0.16f, 0.9f), shade);
                    break;
                case PropModel.SeaStack:
                    k.Prism(Vector3.zero, 1.1f, 1.6f, 5, c, 0.7f);
                    k.Prism(new Vector3(0.1f, 1.6f, 0f), 0.75f, 1.2f, 5, shade, 0.5f);
                    break;
                case PropModel.Pine:
                    k.Prism(Vector3.zero, 0.18f, 1.2f, 5, new Color(0.55f, 0.42f, 0.32f));
                    k.Prism(new Vector3(0f, 0.9f, 0f), 1.2f, 1.7f, 6, c, 0f);
                    k.Prism(new Vector3(0f, 2.1f, 0f), 0.85f, 1.6f, 6, new Color(0.92f, 0.92f, 0.92f), 0f);
                    break;
                case PropModel.DeadTree:
                    k.Prism(Vector3.zero, 0.22f, 3.6f, 5, c, 0.35f);
                    k.Box(new Vector3(0.45f, 2.4f, 0f), new Vector3(0.1f, 1.2f, 0.1f), shade, 0.4f, Quaternion.Euler(0f, 0f, -40f));
                    k.Box(new Vector3(-0.35f, 1.9f, 0.1f), new Vector3(0.09f, 1f, 0.09f), shade, 0.4f, Quaternion.Euler(10f, 0f, 45f));
                    k.Box(new Vector3(0.1f, 3f, -0.3f), new Vector3(0.07f, 0.8f, 0.07f), shade, 0.4f, Quaternion.Euler(-35f, 0f, 0f));
                    break;
                case PropModel.Driftwood:
                    k.Box(new Vector3(0f, 0.2f, 0f), new Vector3(0.35f, 0.35f, 3.2f), c, 0.85f, Quaternion.Euler(0f, 0f, 8f));
                    k.Box(new Vector3(0.6f, 0.45f, 0.8f), new Vector3(0.18f, 0.9f, 0.18f), shade, 0.5f, Quaternion.Euler(20f, 0f, -30f));
                    break;
            }
            m = k.Build("Prop" + model);
            cache[model] = m;
            return m;
        }
    }
}
