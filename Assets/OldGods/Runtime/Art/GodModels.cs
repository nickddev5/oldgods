using System.Collections.Generic;
using UnityEngine;

namespace OldGods.Runtime
{
    /// <summary>
    /// The seven gods: a robed figure about 1.85 m tall with hands, feet, a masked face and a
    /// domain-specific crown and gear. The robe is the body; feet and arms are tagged so the
    /// shader can walk them. Face +z, feet at y = 0.
    /// </summary>
    public static class GodModels
    {
        static readonly Dictionary<GodLook, Mesh> cache = new Dictionary<GodLook, Mesh>();
        static readonly Color Robe = new Color(0.92f, 0.92f, 0.92f);
        static readonly Color Trim = new Color(0.72f, 0.72f, 0.72f);
        static readonly Color Skin = new Color(1f, 0.93f, 0.85f);
        static readonly Color Gold = new Color(1f, 0.82f, 0.45f);
        static readonly Color Dark = new Color(0.35f, 0.33f, 0.32f);
        static readonly Color Pale = Color.white;

        public static Mesh Get(GodLook look)
        {
            if (cache.TryGetValue(look, out var m) && m != null) return m;
            var k = new MeshKit();
            Build(k, look);
            m = k.Build("God" + look);
            cache[look] = m;
            return m;
        }

        static void Build(MeshKit k, GodLook look)
        {
            float tall = look == GodLook.Elias ? 1.06f : look == GodLook.Earth ? 0.97f : 1f;
            float broad = look == GodLook.Forge || look == GodLook.Earth ? 1.18f : look == GodLook.Hunt ? 0.9f : 1f;

            // Feet under the hem, tagged as legs.
            for (int side = -1; side <= 1; side += 2)
            {
                var hip = new Vector3(side * 0.11f, 0.75f, 0f);
                k.Part(side < 0 ? BodyPart.LeftLeg : BodyPart.RightLeg, hip);
                k.Limb(new Vector3(side * 0.11f, 0.3f, 0f), new Vector3(side * 0.11f, 0.08f, 0f), 0.06f, 0.05f, Dark, 6);
                k.Box(new Vector3(side * 0.11f, 0.04f, 0.07f), new Vector3(0.1f, 0.08f, 0.22f), Dark, 0.75f);
            }

            k.Body();
            // Robe: flared hem, cinched waist, broad chest.
            k.Lathe(Vector3.zero, new[]
            {
                new Vector2(0.06f, 0.4f * broad), new Vector2(0.3f, 0.34f * broad), new Vector2(0.7f, 0.25f * broad),
                new Vector2(0.9f * tall, 0.22f * broad), new Vector2(1.2f * tall, 0.27f * broad), new Vector2(1.42f * tall, 0.2f * broad),
                new Vector2(1.5f * tall, 0.1f),
            }, 12, Robe, 0.8f);
            // Belt and a sash down the front.
            k.Lathe(new Vector3(0f, 0.86f * tall, 0f), new[] { new Vector2(0f, 0.235f * broad), new Vector2(0.08f, 0.235f * broad) }, 12, Gold, 0.8f);
            k.Box(new Vector3(0f, 0.45f, 0.26f * broad), new Vector3(0.12f, 0.8f, 0.03f), Trim, 1f, Quaternion.Euler(-8f, 0f, 0f));
            // Mantle over the shoulders.
            k.Ball(new Vector3(0f, 1.38f * tall, -0.02f), new Vector3(0.36f * broad, 0.12f, 0.24f), Trim, 10, 4);

            // Arms in loose sleeves with hands.
            for (int side = -1; side <= 1; side += 2)
            {
                var shoulder = new Vector3(side * 0.3f * broad, 1.36f * tall, 0f);
                k.Part(side < 0 ? BodyPart.LeftArm : BodyPart.RightArm, shoulder);
                var elbow = shoulder + new Vector3(side * 0.06f, -0.32f, 0.04f);
                var wrist = elbow + new Vector3(0f, -0.26f, 0.12f);
                k.Limb(shoulder, elbow, 0.08f, 0.09f, Robe, 7);
                k.Limb(elbow, wrist, 0.09f, 0.11f, Robe, 7);
                k.Ball(wrist + new Vector3(0f, -0.06f, 0.03f), new Vector3(0.05f, 0.065f, 0.045f), Skin, 6, 3);
            }

            // Head: face, hair or hood, and a mask.
            var neck = new Vector3(0f, 1.5f * tall, 0f);
            k.Part(BodyPart.Head, neck);
            var head = neck + new Vector3(0f, 0.17f, 0.02f);
            k.Limb(neck - Vector3.up * 0.04f, head - Vector3.up * 0.08f, 0.06f, 0.055f, Skin, 6);
            k.Ball(head, new Vector3(0.13f, 0.155f, 0.14f), Skin, 9, 6);
            k.Ball(head + new Vector3(0f, 0.04f, -0.03f), new Vector3(0.14f, 0.15f, 0.14f), Dark, 9, 5); // hair
            k.Block(head + new Vector3(0f, 0.01f, 0.12f), new Vector3(0.2f, 0.12f, 0.04f), Gold, 0.4f); // mask
            k.Ball(head + new Vector3(-0.045f, 0.015f, 0.145f), new Vector3(0.018f, 0.012f, 0.01f), Dark, 4, 2);
            k.Ball(head + new Vector3(0.045f, 0.015f, 0.145f), new Vector3(0.018f, 0.012f, 0.01f), Dark, 4, 2);

            switch (look)
            {
                case GodLook.Storm:
                    for (int i = -2; i <= 2; i++)
                        k.Limb(head + new Vector3(i * 0.06f, 0.12f, -0.01f), head + new Vector3(i * 0.11f, 0.38f - Mathf.Abs(i) * 0.05f, -0.04f), 0.025f, 0f, Pale, 4);
                    k.Body();
                    // A furled cloak like a stormcloud.
                    k.Ball(new Vector3(0f, 1.05f, -0.28f), new Vector3(0.3f, 0.45f, 0.1f), Trim, 8, 5, null, 0.08f, 7);
                    break;
                case GodLook.Forge:
                    k.Lathe(head + Vector3.up * 0.1f, new[] { new Vector2(0f, 0.15f), new Vector2(0.08f, 0.15f) }, 8, Dark);
                    k.Body();
                    k.Block(new Vector3(-0.38f, 1.4f, 0f), new Vector3(0.24f, 0.14f, 0.3f), Dark, 0.35f, Quaternion.Euler(0f, 0f, 18f));
                    k.Block(new Vector3(0.38f, 1.4f, 0f), new Vector3(0.24f, 0.14f, 0.3f), Dark, 0.35f, Quaternion.Euler(0f, 0f, -18f));
                    // Hammer across the back.
                    k.Limb(new Vector3(-0.25f, 0.65f, -0.3f), new Vector3(0.3f, 1.45f, -0.3f), 0.03f, 0.03f, Dark, 5);
                    k.Block(new Vector3(0.32f, 1.48f, -0.3f), new Vector3(0.22f, 0.13f, 0.13f), Trim, 0.25f, Quaternion.Euler(0f, 0f, 35f));
                    // Leather apron.
                    k.Block(new Vector3(0f, 0.6f, 0.27f), new Vector3(0.36f, 0.55f, 0.04f), Dark, 0.2f, Quaternion.Euler(-6f, 0f, 0f));
                    break;
                case GodLook.Tide:
                    k.Ball(head + new Vector3(0f, 0.02f, -0.04f), new Vector3(0.18f, 0.2f, 0.18f), Robe, 9, 5); // hood
                    k.Limb(head + new Vector3(0f, 0.16f, -0.08f), head + new Vector3(0f, 0.32f, -0.3f), 0.06f, 0f, Pale, 5);
                    k.Body();
                    for (int i = 0; i < 4; i++)
                    {
                        float a = i * 0.6f - 0.9f;
                        var root = new Vector3(Mathf.Sin(a) * 0.32f, 0.3f, -Mathf.Cos(a) * 0.26f);
                        k.Limb(root, root + new Vector3(Mathf.Sin(a) * 0.1f, -0.25f, -0.08f), 0.04f, 0f, Pale, 4);
                    }
                    break;
                case GodLook.Hunt:
                    k.Limb(head + new Vector3(-0.08f, 0.12f, 0f), head + new Vector3(-0.22f, 0.36f, -0.1f), 0.022f, 0f, Pale, 4);
                    k.Limb(head + new Vector3(0.08f, 0.12f, 0f), head + new Vector3(0.22f, 0.36f, -0.1f), 0.022f, 0f, Pale, 4);
                    k.Limb(head + new Vector3(-0.15f, 0.25f, -0.05f), head + new Vector3(-0.26f, 0.3f, -0.02f), 0.015f, 0f, Pale, 4);
                    k.Limb(head + new Vector3(0.15f, 0.25f, -0.05f), head + new Vector3(0.26f, 0.3f, -0.02f), 0.015f, 0f, Pale, 4);
                    k.Body();
                    // Quiver of spears.
                    k.Lathe(new Vector3(0.12f, 0.95f, -0.3f), new[] { new Vector2(0f, 0.06f), new Vector2(0.5f, 0.07f) }, 6, Dark);
                    for (int i = 0; i < 3; i++)
                        k.Limb(new Vector3(0.08f + i * 0.04f, 1.4f, -0.3f), new Vector3(0.1f + i * 0.05f, 1.75f, -0.32f), 0.012f, 0f, Pale, 3);
                    break;
                case GodLook.Ember:
                    for (int i = 0; i < 7; i++)
                    {
                        float a = i * Mathf.PI * 2f / 7f;
                        var root = head + new Vector3(Mathf.Cos(a) * 0.12f, 0.12f, Mathf.Sin(a) * 0.12f);
                        k.Limb(root, root + new Vector3(Mathf.Cos(a) * 0.04f, 0.14f + (i % 2) * 0.08f, Mathf.Sin(a) * 0.04f), 0.03f, 0f, Gold, 4);
                    }
                    k.Body();
                    // A brazier at the belt.
                    k.Lathe(new Vector3(-0.27f, 0.62f, 0.08f), new[] { new Vector2(0f, 0.03f), new Vector2(0.08f, 0.08f), new Vector2(0.1f, 0.07f) }, 7, Gold);
                    break;
                case GodLook.Earth:
                    k.Limb(head + new Vector3(-0.12f, 0.06f, 0f), head + new Vector3(-0.26f, 0.12f, 0.08f), 0.035f, 0.01f, Pale, 5);
                    k.Limb(head + new Vector3(0.12f, 0.06f, 0f), head + new Vector3(0.26f, 0.12f, 0.08f), 0.035f, 0.01f, Pale, 5);
                    k.Body();
                    // A stone mantle.
                    k.Ball(new Vector3(0f, 1.42f, -0.05f), new Vector3(0.46f, 0.12f, 0.3f), Trim, 7, 3, null, 0.12f, 11);
                    for (int i = -1; i <= 1; i++)
                        k.Block(new Vector3(i * 0.22f, 1.5f, -0.1f), new Vector3(0.14f, 0.12f, 0.14f), Dark, 0.3f, Quaternion.Euler(0f, i * 25f, i * -15f));
                    break;
                case GodLook.Elias:
                    for (int i = 0; i < 9; i++)
                    {
                        float a = i * Mathf.PI * 2f / 9f;
                        var root = head + new Vector3(Mathf.Cos(a) * 0.14f, 0.12f, Mathf.Sin(a) * 0.14f);
                        k.Limb(root, root + new Vector3(0f, 0.08f + (i % 3 == 0 ? 0.06f : 0f), 0f), 0.018f, 0f, Gold, 4);
                    }
                    k.Lathe(head + Vector3.up * 0.1f, new[] { new Vector2(0f, 0.15f), new Vector2(0.03f, 0.15f) }, 9, Gold);
                    k.Body();
                    // A long cloak that trails the ground.
                    k.Lathe(new Vector3(0f, 0f, -0.12f), new[] { new Vector2(0.02f, 0.38f), new Vector2(0.9f, 0.3f), new Vector2(1.45f, 0.26f) }, 10, Trim, 0.6f);
                    break;
            }
            k.Body();
        }
    }
}
