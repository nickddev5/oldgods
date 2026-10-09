using System.Collections.Generic;
using UnityEngine;

namespace OldGods.Runtime
{
    /// <summary>
    /// Boss bodies, about 1.7 m tall before the boss's scale (3 to 3.6x in game), facing +z,
    /// with tagged legs and arms so the shader can walk them.
    /// </summary>
    public static class BossModels
    {
        static readonly Dictionary<BossModel, Mesh> cache = new Dictionary<BossModel, Mesh>();
        static readonly Color Main = new Color(0.9f, 0.9f, 0.9f);
        static readonly Color Dark = new Color(0.58f, 0.58f, 0.58f);
        static readonly Color Deep = new Color(0.36f, 0.36f, 0.36f);
        static readonly Color Moss = new Color(0.55f, 0.7f, 0.45f);
        static readonly Color Glow = new Color(1f, 0.75f, 0.35f);

        public static Mesh Get(BossModel model)
        {
            if (cache.TryGetValue(model, out var m) && m != null) return m;
            var k = new MeshKit();
            switch (model)
            {
                case BossModel.Warden: Warden(k); break;
                case BossModel.Stag: Stag(k); break;
                case BossModel.Mother: Mother(k); break;
                default: Construct(k); break;
            }
            m = k.Build("Boss" + model);
            cache[model] = m;
            return m;
        }

        /// <summary>A stone golem of carved blocks with moss on its shoulders and a burning eye slit.</summary>
        static void Warden(MeshKit k)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                var hip = new Vector3(side * 0.24f, 0.62f, 0f);
                k.Part(side < 0 ? BodyPart.LeftLeg : BodyPart.RightLeg, hip);
                k.Block(new Vector3(side * 0.25f, 0.45f, 0f), new Vector3(0.26f, 0.32f, 0.3f), Dark, 0.3f);
                k.Block(new Vector3(side * 0.26f, 0.17f, 0.04f), new Vector3(0.3f, 0.3f, 0.4f), Deep, 0.3f);
            }
            k.Body();
            k.Block(new Vector3(0f, 0.72f, 0f), new Vector3(0.62f, 0.22f, 0.4f), Deep, 0.3f);
            k.Block(new Vector3(0f, 1.02f, 0.02f), new Vector3(0.9f, 0.48f, 0.55f), Main, 0.25f, Quaternion.Euler(8f, 0f, 0f));
            k.Block(new Vector3(0f, 1.28f, 0.05f), new Vector3(1.05f, 0.24f, 0.6f), Main, 0.3f, Quaternion.Euler(8f, 0f, 0f));
            // Carved runes on the chest.
            for (int i = -1; i <= 1; i++)
                k.Box(new Vector3(i * 0.18f, 1.05f, 0.32f), new Vector3(0.05f, 0.24f, 0.03f), Glow);
            // Moss on the shoulders.
            k.Ball(new Vector3(-0.42f, 1.42f, 0f), new Vector3(0.2f, 0.07f, 0.22f), Moss, 7, 3, null, 0.2f, 1);
            k.Ball(new Vector3(0.42f, 1.42f, 0f), new Vector3(0.2f, 0.07f, 0.22f), Moss, 7, 3, null, 0.2f, 2);
            for (int side = -1; side <= 1; side += 2)
            {
                var shoulder = new Vector3(side * 0.62f, 1.3f, 0.05f);
                k.Part(side < 0 ? BodyPart.LeftArm : BodyPart.RightArm, shoulder);
                k.Block(shoulder + new Vector3(side * 0.05f, 0.02f, 0f), new Vector3(0.36f, 0.3f, 0.36f), Main, 0.35f);
                k.Block(shoulder + new Vector3(side * 0.08f, -0.32f, 0.05f), new Vector3(0.24f, 0.36f, 0.26f), Dark, 0.3f);
                k.Block(shoulder + new Vector3(side * 0.08f, -0.68f, 0.14f), new Vector3(0.3f, 0.36f, 0.3f), Dark, 0.3f, Quaternion.Euler(-15f, 0f, 0f));
                k.Block(shoulder + new Vector3(side * 0.08f, -0.98f, 0.24f), new Vector3(0.38f, 0.26f, 0.36f), Deep, 0.35f);
            }
            var neck = new Vector3(0f, 1.42f, 0.1f);
            k.Part(BodyPart.Head, neck);
            k.Block(new Vector3(0f, 1.56f, 0.12f), new Vector3(0.34f, 0.3f, 0.32f), Main, 0.3f);
            k.Box(new Vector3(0f, 1.58f, 0.29f), new Vector3(0.24f, 0.05f, 0.02f), Glow);
            k.Block(new Vector3(0f, 1.74f, 0.08f), new Vector3(0.4f, 0.08f, 0.36f), Dark, 0.4f);
            k.Body();
        }

        /// <summary>A burnt stag: diagonal leg pairs trot together; a wide crown of antlers.</summary>
        static void Stag(MeshKit k)
        {
            void Leg(BodyPart part, float x, float z, bool front)
            {
                var top = new Vector3(x, 0.9f, z);
                k.Part(part, top);
                var knee = new Vector3(x, 0.5f, z + (front ? 0.04f : -0.08f));
                var hoof = new Vector3(x, 0.05f, z + (front ? 0.06f : 0f));
                k.Limb(top, knee, 0.09f, 0.055f, Dark, 6);
                k.Limb(knee, hoof, 0.05f, 0.04f, Dark, 6);
                k.Box(hoof - Vector3.up * 0.02f, new Vector3(0.08f, 0.06f, 0.1f), Deep);
            }
            Leg(BodyPart.LeftLeg, -0.18f, 0.42f, true);
            Leg(BodyPart.LeftLeg, 0.18f, -0.42f, false);
            Leg(BodyPart.RightLeg, 0.18f, 0.42f, true);
            Leg(BodyPart.RightLeg, -0.18f, -0.42f, false);
            k.Body();
            k.Ball(new Vector3(0f, 0.95f, 0f), new Vector3(0.28f, 0.26f, 0.62f), Main, 10, 5);
            k.Ball(new Vector3(0f, 1.0f, 0.38f), new Vector3(0.27f, 0.28f, 0.28f), Main, 8, 4);
            k.Limb(new Vector3(0f, 1.05f, -0.6f), new Vector3(0f, 1.15f, -0.72f), 0.06f, 0f, Dark, 4); // tail
            // Ember cracks along the flank.
            for (int i = 0; i < 4; i++)
                k.Box(new Vector3(0.28f, 0.95f + (i % 2) * 0.08f, -0.3f + i * 0.18f), new Vector3(0.02f, 0.03f, 0.14f), Glow, 1f, Quaternion.Euler(0f, 0f, 10f));
            var neck = new Vector3(0f, 1.15f, 0.5f);
            k.Part(BodyPart.Head, neck);
            var head = new Vector3(0f, 1.48f, 0.78f);
            k.Limb(neck, head - new Vector3(0f, 0.06f, 0.06f), 0.13f, 0.09f, Main, 7);
            k.Ball(head, new Vector3(0.12f, 0.13f, 0.18f), Main, 7, 4, Quaternion.Euler(20f, 0f, 0f));
            k.Limb(head + new Vector3(0f, -0.04f, 0.1f), head + new Vector3(0f, -0.1f, 0.32f), 0.08f, 0.05f, Dark, 6); // snout
            k.Ball(head + new Vector3(-0.07f, 0.04f, 0.12f), Vector3.one * 0.025f, Glow, 4, 2);
            k.Ball(head + new Vector3(0.07f, 0.04f, 0.12f), Vector3.one * 0.025f, Glow, 4, 2);
            for (int side = -1; side <= 1; side += 2)
            {
                var root = head + new Vector3(side * 0.08f, 0.1f, -0.02f);
                var a1 = root + new Vector3(side * 0.18f, 0.28f, -0.08f);
                var a2 = a1 + new Vector3(side * 0.12f, 0.3f, -0.12f);
                k.Limb(root, a1, 0.035f, 0.028f, Deep, 5);
                k.Limb(a1, a2, 0.028f, 0.012f, Deep, 5);
                k.Limb(a1, a1 + new Vector3(side * 0.22f, 0.12f, 0.08f), 0.022f, 0f, Deep, 4);
                k.Limb(a2 - new Vector3(side * 0.04f, 0.1f, -0.03f), a2 + new Vector3(side * 0.18f, 0.06f, 0.06f), 0.018f, 0f, Deep, 4);
                k.Limb(a2, a2 + new Vector3(side * 0.02f, 0.2f, -0.06f), 0.014f, 0f, Deep, 4);
            }
            k.Body();
        }

        /// <summary>A drowned matriarch: a towering shroud, long arms, a coral crown, tentacles at the hem.</summary>
        static void Mother(MeshKit k)
        {
            k.Body();
            k.Lathe(Vector3.zero, new[] { new Vector2(0.15f, 0.62f), new Vector2(0.5f, 0.45f), new Vector2(1.0f, 0.32f), new Vector2(1.3f, 0.36f), new Vector2(1.5f, 0.2f) }, 12, Dark, 0.85f);
            k.Ball(new Vector3(0f, 1.35f, 0f), new Vector3(0.38f, 0.16f, 0.3f), Main, 10, 4);
            // Shells along the robe.
            for (int i = 0; i < 6; i++)
            {
                float a = i * 1.1f;
                k.Ball(new Vector3(Mathf.Cos(a) * 0.42f, 0.5f + (i % 3) * 0.2f, Mathf.Sin(a) * 0.36f), new Vector3(0.07f, 0.05f, 0.07f), Main, 5, 2);
            }
            // Tentacles: alternate between the two leg groups so they ripple.
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI * 2f / 8f;
                var root = new Vector3(Mathf.Cos(a) * 0.55f, 0.18f, Mathf.Sin(a) * 0.47f);
                k.Part(i % 2 == 0 ? BodyPart.LeftLeg : BodyPart.RightLeg, root + Vector3.up * 0.3f);
                var mid = root + new Vector3(Mathf.Cos(a) * 0.2f, -0.12f, Mathf.Sin(a) * 0.2f);
                k.Limb(root, mid, 0.09f, 0.06f, Dark, 6);
                k.Limb(mid, mid + new Vector3(Mathf.Cos(a) * 0.22f, -0.05f, Mathf.Sin(a) * 0.22f), 0.06f, 0f, Main, 5);
            }
            for (int side = -1; side <= 1; side += 2)
            {
                var shoulder = new Vector3(side * 0.36f, 1.38f, 0f);
                k.Part(side < 0 ? BodyPart.LeftArm : BodyPart.RightArm, shoulder);
                var elbow = shoulder + new Vector3(side * 0.12f, -0.45f, 0.18f);
                var hand = elbow + new Vector3(0f, -0.35f, 0.3f);
                k.Limb(shoulder, elbow, 0.08f, 0.065f, Dark, 6);
                k.Limb(elbow, hand, 0.065f, 0.05f, Main, 6);
                for (int f = -1; f <= 1; f++)
                    k.Limb(hand, hand + new Vector3(f * 0.06f, -0.12f, 0.12f), 0.025f, 0f, Main, 4);
            }
            var neck = new Vector3(0f, 1.48f, 0f);
            k.Part(BodyPart.Head, neck);
            var head = new Vector3(0f, 1.68f, 0.04f);
            k.Ball(head, new Vector3(0.17f, 0.21f, 0.18f), Main, 9, 5);
            k.Ball(head + new Vector3(0f, -0.03f, 0.12f), new Vector3(0.11f, 0.12f, 0.07f), Deep, 7, 4);
            k.Ball(head + new Vector3(-0.05f, 0.0f, 0.17f), Vector3.one * 0.022f, Glow, 4, 2);
            k.Ball(head + new Vector3(0.05f, 0.0f, 0.17f), Vector3.one * 0.022f, Glow, 4, 2);
            for (int i = 0; i < 7; i++)
            {
                float a = Mathf.Lerp(-1.2f, 1.2f, i / 6f);
                var root = head + new Vector3(Mathf.Sin(a) * 0.15f, 0.15f, -Mathf.Cos(a) * 0.04f);
                var tip = root + new Vector3(Mathf.Sin(a) * 0.12f, 0.28f - Mathf.Abs(a) * 0.08f, -0.05f);
                k.Limb(root, tip, 0.03f, 0.008f, Main, 4);
                k.Limb(Vector3.Lerp(root, tip, 0.5f), Vector3.Lerp(root, tip, 0.5f) + new Vector3(Mathf.Sin(a) * 0.08f, 0.06f, 0f), 0.015f, 0f, Main, 4);
            }
            k.Body();
        }

        /// <summary>The Last Test: a construct of plates around a burning core, on jointed legs.</summary>
        static void Construct(MeshKit k)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                var hip = new Vector3(side * 0.3f, 0.72f, 0f);
                k.Part(side < 0 ? BodyPart.LeftLeg : BodyPart.RightLeg, hip);
                var knee = new Vector3(side * 0.38f, 0.38f, 0.12f);
                k.Limb(hip, knee, 0.08f, 0.07f, Dark, 6);
                k.Ball(knee, Vector3.one * 0.09f, Main, 6, 3);
                k.Limb(knee, new Vector3(side * 0.36f, 0.06f, 0f), 0.07f, 0.05f, Dark, 6);
                k.Block(new Vector3(side * 0.36f, 0.04f, 0.04f), new Vector3(0.22f, 0.08f, 0.3f), Deep, 0.3f);
            }
            k.Body();
            k.Block(new Vector3(0f, 0.78f, 0f), new Vector3(0.5f, 0.16f, 0.34f), Deep, 0.35f);
            var core = new Vector3(0f, 1.15f, 0f);
            k.Gem(core, new Vector3(0.24f, 0.32f, 0.24f), Glow);
            // A cage of plates around the core.
            for (int i = 0; i < 6; i++)
            {
                float a = i * Mathf.PI * 2f / 6f;
                var at = core + new Vector3(Mathf.Cos(a) * 0.38f, 0f, Mathf.Sin(a) * 0.34f);
                k.Block(at, new Vector3(0.16f, 0.62f, 0.06f), Main, 0.4f, Quaternion.Euler(0f, -a * Mathf.Rad2Deg + 90f, 0f));
            }
            k.Lathe(core + Vector3.up * 0.32f, new[] { new Vector2(0f, 0.46f), new Vector2(0.08f, 0.4f), new Vector2(0.12f, 0.2f) }, 8, Main);
            for (int side = -1; side <= 1; side += 2)
            {
                var shoulder = new Vector3(side * 0.52f, 1.42f, 0f);
                k.Part(side < 0 ? BodyPart.LeftArm : BodyPart.RightArm, shoulder);
                k.Ball(shoulder, new Vector3(0.16f, 0.14f, 0.16f), Main, 7, 4);
                var elbow = shoulder + new Vector3(side * 0.16f, -0.38f, 0.1f);
                k.Limb(shoulder, elbow, 0.07f, 0.06f, Dark, 6);
                var fist = elbow + new Vector3(0f, -0.38f, 0.2f);
                k.Limb(elbow, fist, 0.06f, 0.08f, Dark, 6);
                k.Block(fist, new Vector3(0.2f, 0.22f, 0.22f), Main, 0.35f);
            }
            var neck = new Vector3(0f, 1.5f, 0f);
            k.Part(BodyPart.Head, neck);
            k.Gem(new Vector3(0f, 1.72f, 0.02f), new Vector3(0.16f, 0.2f, 0.16f), Main);
            k.Box(new Vector3(0f, 1.72f, 0.16f), new Vector3(0.18f, 0.04f, 0.04f), Glow);
            // A broken halo.
            for (int i = 0; i < 7; i++)
            {
                float a0 = i * Mathf.PI * 2f / 9f, a1 = (i + 1) * Mathf.PI * 2f / 9f;
                var p0 = new Vector3(Mathf.Cos(a0) * 0.3f, 1.98f + Mathf.Sin(a0) * 0.04f, Mathf.Sin(a0) * 0.3f - 0.05f);
                var p1 = new Vector3(Mathf.Cos(a1) * 0.3f, 1.98f + Mathf.Sin(a1) * 0.04f, Mathf.Sin(a1) * 0.3f - 0.05f);
                k.Limb(p0, p1, 0.02f, 0.02f, Glow, 4);
            }
            k.Body();
        }
    }
}
