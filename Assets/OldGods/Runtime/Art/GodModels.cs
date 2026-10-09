using System.Collections.Generic;
using UnityEngine;

namespace OldGods.Runtime
{
    /// <summary>
    /// The seven gods as dressed figures (about 1.75 m): layered clothes, belts and pouches,
    /// faces, hands and their domain's gear. Colours are in the mesh, so the material is white.
    /// Statues use the same shapes in a single stone colour.
    /// </summary>
    public static class GodModels
    {
        static readonly Dictionary<string, Mesh> cache = new Dictionary<string, Mesh>();

        public static Mesh Get(GodLook look) => Build(look, false);
        public static Mesh Statue(GodLook look) => Build(look, true);

        static Mesh Build(GodLook look, bool stone)
        {
            string key = look + (stone ? "_stone" : "");
            if (cache.TryGetValue(key, out var m) && m != null) return m;
            var k = new MeshKit();
            var p = PaletteFor(look);
            if (stone)
            {
                var g = new Color(0.78f, 0.77f, 0.74f);
                var d = new Color(0.62f, 0.61f, 0.59f);
                p = new Palette { Skin = g, SkinShade = d, Cloth = g, Cloth2 = d, Leather = d, Metal = g, Trim = g, Hair = d, Eye = g, Pupil = d, Glow = g };
            }
            Dress(new Figure(k, FigureSpec.Human, p).Layout(), look);
            m = k.Build("God" + key);
            cache[key] = m;
            return m;
        }

        static Palette PaletteFor(GodLook look)
        {
            var p = Palette.Neutral;
            switch (look)
            {
                case GodLook.Storm:
                    p.Cloth = new Color(0.22f, 0.42f, 0.72f); p.Cloth2 = new Color(0.25f, 0.27f, 0.32f); p.Trim = new Color(0.85f, 0.88f, 0.95f);
                    p.Hair = new Color(0.82f, 0.84f, 0.88f); p.Glow = new Color(0.6f, 0.85f, 1f); p.Skin = new Color(0.86f, 0.74f, 0.64f);
                    break;
                case GodLook.Forge:
                    p.Cloth = new Color(0.55f, 0.32f, 0.18f); p.Cloth2 = new Color(0.3f, 0.27f, 0.25f); p.Trim = new Color(0.95f, 0.6f, 0.25f);
                    p.Hair = new Color(0.45f, 0.2f, 0.1f); p.Skin = new Color(0.76f, 0.56f, 0.42f); p.SkinShade = new Color(0.62f, 0.44f, 0.33f);
                    break;
                case GodLook.Tide:
                    p.Cloth = new Color(0.2f, 0.55f, 0.55f); p.Cloth2 = new Color(0.82f, 0.86f, 0.84f); p.Trim = new Color(0.95f, 0.92f, 0.82f);
                    p.Hair = new Color(0.12f, 0.25f, 0.28f); p.Skin = new Color(0.8f, 0.72f, 0.66f);
                    break;
                case GodLook.Hunt:
                    p.Cloth = new Color(0.32f, 0.48f, 0.27f); p.Cloth2 = new Color(0.4f, 0.32f, 0.24f); p.Trim = new Color(0.88f, 0.84f, 0.72f);
                    p.Hair = new Color(0.62f, 0.38f, 0.18f);
                    break;
                case GodLook.Ember:
                    p.Cloth = new Color(0.78f, 0.25f, 0.15f); p.Cloth2 = new Color(0.22f, 0.18f, 0.17f); p.Trim = new Color(1f, 0.7f, 0.25f);
                    p.Hair = new Color(0.9f, 0.45f, 0.15f); p.Glow = new Color(1f, 0.6f, 0.2f); p.Skin = new Color(0.82f, 0.62f, 0.5f);
                    break;
                case GodLook.Earth:
                    p.Cloth = new Color(0.62f, 0.5f, 0.3f); p.Cloth2 = new Color(0.36f, 0.34f, 0.3f); p.Trim = new Color(0.55f, 0.72f, 0.42f);
                    p.Hair = new Color(0.3f, 0.26f, 0.22f); p.Metal = new Color(0.58f, 0.56f, 0.53f); p.Skin = new Color(0.66f, 0.5f, 0.38f);
                    break;
                case GodLook.Elias:
                    p.Cloth = new Color(0.94f, 0.92f, 0.86f); p.Cloth2 = new Color(0.62f, 0.55f, 0.45f); p.Trim = new Color(1f, 0.82f, 0.4f);
                    p.Hair = new Color(0.32f, 0.24f, 0.18f);
                    break;
            }
            return p;
        }

        static void Dress(Figure f, GodLook look)
        {
            var p = f.P;
            switch (look)
            {
                case GodLook.Storm:
                    // The sky's wanderer: long coat over trousers and boots, wide pointed hat, staff with a stormglass.
                    f.Body(p.Cloth2, p.Cloth2, p.Cloth).Face();
                    f.Trousers(p.Cloth2, 1.15f).Boots(p.Leather * 0.7f, 0.36f, p.Leather);
                    f.Tunic(p.Cloth, 0.42f, 1.55f).Sleeves(p.Cloth, false, p.Trim).Gloves(p.Leather);
                    f.Belt(p.Leather, p.Trim, 3).Tabard(p.Trim, 0.4f, 0.09f);
                    f.Mantle(p.Cloth, 0.22f, 1.75f, p.Cloth2);
                    f.Hair(p.Hair, 0.98f).WizardHat(p.Cloth, p.Cloth2, 0.46f);
                    f.Weapon("staff", 1.6f, p.Glow, p.Leather).Book(p.Cloth2, p.Trim);
                    break;
                case GodLook.Forge:
                    // The smith: bare forearms, leather apron, heavy gloves, beard, hammer.
                    f.Body(p.Cloth2, p.Cloth, p.Skin).Face();
                    f.Trousers(p.Cloth2, 1.2f).Boots(p.Leather * 0.6f, 0.3f, p.Metal);
                    f.Tunic(p.Cloth, 0.14f, 1.25f).Sleeves(p.Cloth, true, p.Cloth2).Gloves(p.Leather * 0.7f);
                    f.Tabard(p.Leather, 0.62f, 0.3f).Belt(p.Leather * 0.8f, p.Metal, 4);
                    f.Pauldrons(p.Metal, 0.1f, p.Trim);
                    Beard(f, p.Hair);
                    f.Hair(p.Hair, 0.9f);
                    f.Weapon("hammer", 0.7f, p.Metal, p.Leather);
                    break;
                case GodLook.Tide:
                    // The tide-keeper: a hooded robe with a pale sash and shell clasps, trident.
                    f.Body(p.Cloth, p.Cloth, p.Skin).Face();
                    f.Boots(p.Cloth2 * 0.8f, 0.2f);
                    f.Robe(p.Cloth, 0.04f, 1.95f).Sleeves(p.Cloth, false, p.Cloth2);
                    f.Belt(p.Cloth2, p.Trim, 2).Tabard(p.Cloth2, 0.72f, 0.12f);
                    f.Mantle(p.Cloth2, 0.16f, 1.6f, p.Cloth);
                    f.Hood(p.Cloth);
                    f.Weapon("trident", 1.7f, p.Trim, p.Leather);
                    break;
                case GodLook.Hunt:
                    // The huntress: tunic, trousers, tall boots, short cloak, antler circlet, quiver strap, spear.
                    f.Body(p.Cloth2, p.Cloth, p.Skin).Face();
                    f.Trousers(p.Cloth2, 1.1f).Boots(p.Leather, 0.42f, p.Leather * 0.8f);
                    f.Tunic(p.Cloth, 0.2f, 1.35f).Sleeves(p.Cloth, true, p.Leather).Gloves(p.Leather);
                    f.Belt(p.Leather, p.Trim, 3).Strap(p.Leather);
                    f.Cloak(p.Cloth * 0.85f, 0.55f, p.Cloth2);
                    f.Hair(p.Hair, 1.02f).Ears(p.Skin, 0.08f);
                    Antlers(f, p.Trim);
                    f.Weapon("spear", 1.75f, p.Metal, p.Leather);
                    break;
                case GodLook.Ember:
                    // The flame: red tunic with a dark sash, gauntlets, a crown of fire, a brazier at the belt.
                    f.Body(p.Cloth2, p.Cloth, p.Skin).Face();
                    f.Trousers(p.Cloth2, 1.15f).Boots(p.Cloth2 * 0.7f, 0.34f, p.Trim);
                    f.Tunic(p.Cloth, 0.3f, 1.45f).Sleeves(p.Cloth, false, p.Trim).Gloves(p.Cloth2);
                    f.Belt(p.Cloth2, p.Trim, 2).Tabard(p.Trim, 0.36f, 0.1f);
                    f.Mantle(p.Cloth2, 0.14f, 1.5f, p.Cloth);
                    f.Hair(p.Hair, 1f);
                    FlameCrown(f, p.Trim, p.Glow);
                    break;
                case GodLook.Earth:
                    // The mountain: a heavy robe, stone pauldrons and mantle, down-curving horns, a stone staff.
                    f.Body(p.Cloth2, p.Cloth, p.Skin).Face();
                    f.Boots(p.Cloth2, 0.25f);
                    f.Robe(p.Cloth, 0.04f, 2.1f).Sleeves(p.Cloth, false, p.Cloth2).Gloves(p.Leather);
                    f.Belt(p.Leather, p.Trim, 3);
                    f.Pauldrons(p.Metal, 0.14f, p.Trim);
                    f.Hair(p.Hair, 0.95f);
                    f.Horns(0.18f, 0.035f, p.Cloth2 * 1.4f, 0.75f, -0.6f);
                    Beard(f, p.Hair);
                    f.Weapon("staff", 1.5f, p.Metal, p.Leather);
                    break;
                case GodLook.Elias:
                    // The last god: white robe with gold trim, a long cloak, a plain circlet, a book.
                    f.Body(p.Cloth, p.Cloth, p.Skin).Face();
                    f.Boots(p.Cloth2, 0.22f);
                    f.Robe(p.Cloth, 0.03f, 1.85f).Sleeves(p.Cloth, false, p.Trim);
                    f.Belt(p.Trim, p.Trim, 0).Tabard(p.Trim, 0.75f, 0.1f);
                    f.Cloak(p.Cloth2, 0.08f, p.Trim);
                    f.Mantle(p.Cloth, 0.12f, 1.55f, p.Trim);
                    f.Hair(p.Hair, 0.95f).Crown(p.Trim, 9, 0.045f);
                    f.Book(p.Trim, p.Cloth);
                    break;
            }
        }

        static void Beard(Figure f, Color hair)
        {
            float h = f.Spec.HeadSize;
            f.Kit.Part(BodyPart.Head, f.NeckBase);
            // A squared-off beard: a wide block along the jaw and a narrower one at the chin.
            f.Kit.Block(f.Head + new Vector3(0f, -h * 0.6f, h * 0.45f), new Vector3(h * 1.3f, h * 0.85f, h * 0.8f), hair, 0.45f);
            f.Kit.Block(f.Head + new Vector3(0f, -h * 1.02f, h * 0.52f), new Vector3(h * 0.8f, h * 0.55f, h * 0.55f), hair, 0.45f);
            f.Kit.Body();
        }

        static void Antlers(Figure f, Color color)
        {
            float h = f.Spec.HeadSize;
            var k = f.Kit;
            k.Part(BodyPart.Head, f.NeckBase);
            k.Lathe(f.Head + new Vector3(0f, h * 0.55f, 0f), new[] { new Vector2(0f, h * 0.88f), new Vector2(0.025f, h * 0.9f) }, 14, color);
            for (int side = -1; side <= 1; side += 2)
            {
                var root = f.Head + new Vector3(side * h * 0.6f, h * 0.7f, -h * 0.1f);
                var a = root + new Vector3(side * 0.08f, 0.14f, -0.03f);
                var b = a + new Vector3(side * 0.05f, 0.12f, -0.06f);
                k.Limb(root, a, 0.016f, 0.013f, color, 6);
                k.Limb(a, b, 0.013f, 0f, color, 6);
                k.Limb(a, a + new Vector3(side * 0.09f, 0.04f, 0.04f), 0.01f, 0f, color, 5);
            }
            k.Body();
        }

        static void FlameCrown(Figure f, Color gold, Color flame)
        {
            float h = f.Spec.HeadSize;
            var k = f.Kit;
            k.Part(BodyPart.Head, f.NeckBase);
            var b = f.Head + new Vector3(0f, h * 0.68f, -h * 0.02f);
            k.Lathe(b, new[] { new Vector2(0f, h * 0.8f), new Vector2(0.03f, h * 0.82f) }, 14, gold);
            for (int i = 0; i < 9; i++)
            {
                float a = i * Mathf.PI * 2f / 9f;
                var root = b + new Vector3(Mathf.Cos(a) * h * 0.78f, 0.03f, Mathf.Sin(a) * h * 0.78f);
                k.Limb(root, root + new Vector3(Mathf.Cos(a) * 0.015f, 0.07f + (i % 3 == 0 ? 0.06f : 0f), Mathf.Sin(a) * 0.015f), h * 0.12f, 0f, flame, 6);
            }
            k.Body();
        }
    }
}
