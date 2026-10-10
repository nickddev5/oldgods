using System.Collections.Generic;
using OldGods.Rules;
using UnityEngine;

namespace OldGods.Runtime
{
    /// <summary>
    /// The seven gods as dressed figures (about 1.75 m): layered clothes, belts and pouches,
    /// faces, hands and their domain's gear. Colours are in the mesh, so the material is white.
    /// Statues use the same shapes in a single stone colour.
    /// </summary>
    public static partial class GodModels
    {
        static readonly Dictionary<string, Mesh> cache = new Dictionary<string, Mesh>();

        public static Mesh Get(GodLook look) => Build(look, false);
        public static Mesh Statue(GodLook look) => Build(look, true);

        /// <summary>
        /// Each god's own slide (see DodgeLook): Storm rides a cloud, Forge charges, Tide surfs a wave,
        /// Beast drops to all fours, Ember dives in flame, Earth ploughs through the ground, Elias glides.
        /// </summary>
        public static DodgeStyle Dodge(GodLook look)
        {
            switch (look)
            {
                case GodLook.Storm: return DodgeStyle.StormCloud;
                case GodLook.Forge: return DodgeStyle.ForgeCharge;
                case GodLook.Tide: return DodgeStyle.TideWave;
                case GodLook.Beast: return DodgeStyle.BeastBound;
                case GodLook.Ember: return DodgeStyle.EmberDive;
                case GodLook.Earth: return DodgeStyle.EarthBurrow;
                case GodLook.Elias: return DodgeStyle.EliasGlide;
                default: return DodgeStyle.Slide;
            }
        }

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
            Dress(new Figure(k, look == GodLook.Beast ? BeastSpec : FigureSpec.Human, p).Layout(), look);
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
                    // From Nick's concept sheet (2026-10-09): navy and near-black robes over white, silver and gold trim, white hair, glowing eyes.
                    p.Cloth = new Color(0.2f, 0.24f, 0.48f); p.Cloth2 = new Color(0.12f, 0.13f, 0.18f); p.Trim = new Color(0.8f, 0.7f, 0.45f);
                    p.Metal = new Color(0.5f, 0.53f, 0.6f); p.Leather = new Color(0.16f, 0.16f, 0.2f);
                    p.Hair = new Color(0.9f, 0.91f, 0.94f); p.Glow = new Color(0.62f, 0.78f, 1f); p.Skin = new Color(0.8f, 0.72f, 0.68f); p.SkinShade = new Color(0.66f, 0.58f, 0.56f);
                    p.Eye = new Color(0.82f, 0.9f, 1f); p.Pupil = new Color(0.4f, 0.6f, 1f);
                    break;
                case GodLook.Forge:
                    p.Cloth = new Color(0.55f, 0.32f, 0.18f); p.Cloth2 = new Color(0.3f, 0.27f, 0.25f); p.Trim = new Color(0.95f, 0.6f, 0.25f);
                    p.Hair = new Color(0.45f, 0.2f, 0.1f); p.Skin = new Color(0.76f, 0.56f, 0.42f); p.SkinShade = new Color(0.62f, 0.44f, 0.33f);
                    break;
                case GodLook.Tide:
                    p.Cloth = new Color(0.2f, 0.55f, 0.55f); p.Cloth2 = new Color(0.82f, 0.86f, 0.84f); p.Trim = new Color(0.95f, 0.92f, 0.82f);
                    p.Hair = new Color(0.12f, 0.25f, 0.28f); p.Skin = new Color(0.8f, 0.72f, 0.66f);
                    break;
                case GodLook.Beast:
                    // From Nick's concept sheet (2026-10-10): dark reddish-brown skin, crimson hair and cloth, bone and cream fur, gold, glowing red eyes.
                    p.Skin = new Color(0.44f, 0.25f, 0.19f); p.SkinShade = new Color(0.33f, 0.18f, 0.14f);
                    p.Cloth = new Color(0.62f, 0.11f, 0.1f); p.Cloth2 = new Color(0.2f, 0.15f, 0.13f); p.Leather = new Color(0.25f, 0.16f, 0.11f);
                    p.Metal = new Color(0.92f, 0.87f, 0.74f); p.Trim = new Color(0.95f, 0.72f, 0.28f);
                    p.Hair = new Color(0.66f, 0.12f, 0.1f); p.Glow = new Color(1f, 0.82f, 0.35f);
                    p.Eye = new Color(1f, 0.45f, 0.35f); p.Pupil = new Color(0.95f, 0.08f, 0.05f);
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
                    // Lord of the heavens (Nick's concept sheet): a navy robe over a white underlayer and a dark centre panel, a sash
                    // with a silver disc and hanging medallions, dark plate shoulders and a long cape, white
                    // hair and beard, a spiked crown and the spear Skybreaker.
                    f.Body(p.Cloth2, p.Cloth2, p.Cloth).Face();
                    f.Boots(p.Metal * 0.7f, 0.2f);
                    f.Robe(p.Cloth, 0.03f, 2.0f).Sleeves(p.Cloth, false, p.Trim).Gloves(p.Metal * 0.75f);
                    RobePanels(f, new Color(0.88f, 0.89f, 0.91f), p.Cloth2, p.Trim);
                    f.Belt(p.Cloth, p.Trim, 0);
                    Sash(f, p.Metal * 1.3f, p.Trim, p.Glow);
                    f.Cloak(p.Cloth2, 0.04f, p.Cloth);
                    f.Mantle(p.Cloth2, 0.2f, 1.6f, p.Cloth).Pauldrons(p.Metal * 0.8f, 0.085f, p.Trim);
                    f.Hair(p.Hair, 1.04f);
                    LongHair(f, p.Hair);
                    PointedBeard(f, p.Hair);
                    SpikedCrown(f, p.Metal, p.Glow);
                    Skybreaker(f, p.Metal * 0.6f, new Color(0.75f, 0.86f, 1f), p.Trim);
                    StormDetail(f, new Color(0.88f, 0.89f, 0.91f));
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
                case GodLook.Beast:
                    Beast(f);
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

        /// <summary>A white underlayer down the front of the robe with a narrower dark panel and gold edging.</summary>
        static void RobePanels(Figure f, Color white, Color panel, Color gold)
        {
            var s = f.Spec;
            var k = f.Kit;
            k.Body();
            float z = s.TorsoDepth * 1.25f;
            float top = s.LegLength + s.TorsoLength * 0.1f;
            // The robe flares, so the panels lean out toward the hem.
            var lean = Quaternion.Euler(-9f, 0f, 0f);
            k.Box(new Vector3(0f, top * 0.5f, z + 0.06f), new Vector3(0.32f, top, 0.015f), white, 1f, lean);
            k.Box(new Vector3(0f, top * 0.5f, z + 0.07f), new Vector3(0.13f, top, 0.015f), panel, 1f, lean);
            for (int side = -1; side <= 1; side += 2)
                k.Box(new Vector3(side * 0.072f, top * 0.5f, z + 0.075f), new Vector3(0.015f, top, 0.012f), gold, 1f, lean);
            k.Box(new Vector3(0f, 0.12f, z + 0.15f), new Vector3(0.13f, 0.02f, 0.012f), gold, 1f, lean);
        }

        /// <summary>A silver disc on the sash with a compass cross, and medallions hanging on chains.</summary>
        static void Sash(Figure f, Color silver, Color gold, Color glow)
        {
            var s = f.Spec;
            var k = f.Kit;
            k.Body();
            var front = f.Torso(0f, s.TorsoLength * 0.12f, s.TorsoDepth * 1.25f);
            var faceOut = f.TorsoRotation * Quaternion.Euler(90f, 0f, 0f);
            k.Lathe(front, new[] { new Vector2(0f, 0.065f), new Vector2(0.02f, 0.065f), new Vector2(0.03f, 0.04f) }, 10, silver, 1f, faceOut);
            var c = front + f.TorsoRotation * new Vector3(0f, 0f, 0.035f);
            k.Box(c, new Vector3(0.11f, 0.014f, 0.01f), gold);
            k.Box(c, new Vector3(0.014f, 0.11f, 0.01f), gold);
            k.Gem(c + Vector3.forward * 0.008f, new Vector3(0.016f, 0.016f, 0.01f), glow);
            // Medallions hang off-centre down the right of the robe, as on the sheet.
            float[] drops = { 0.16f, 0.32f, 0.46f };
            var hang = front + new Vector3(0.09f, -0.03f, 0.02f);
            var prev = hang;
            for (int i = 0; i < drops.Length; i++)
            {
                var m = hang + new Vector3(0.01f * i, -drops[i], 0.02f + 0.012f * i);
                k.Limb(prev, m + Vector3.up * 0.03f, 0.004f, 0.004f, gold, 4);
                prev = m - Vector3.up * 0.03f;
                var face = i == 1 ? silver : gold;
                var cross = i == 1 ? gold : silver;
                k.Lathe(m, new[] { new Vector2(0f, 0.032f), new Vector2(0.012f, 0.032f) }, 8, face, 1f, Quaternion.Euler(90f, 0f, 0f));
                k.Box(m + Vector3.forward * 0.014f, new Vector3(0.04f, 0.008f, 0.006f), cross);
                k.Box(m + Vector3.forward * 0.014f, new Vector3(0.008f, 0.04f, 0.006f), cross);
            }
        }

        /// <summary>White hair falling down the back past the shoulder blades, with a lock either side of the face.</summary>
        static void LongHair(Figure f, Color hair)
        {
            var s = f.Spec;
            float h = s.HeadSize;
            var k = f.Kit;
            bool smooth = k.Smooth;
            k.Smooth = false;
            k.Body();
            // Down the back: a slab that widens toward the bottom and ends in three tapering points.
            var nape = f.Head + new Vector3(0f, -h * 0.4f, -h * 0.75f);
            float len = s.TorsoLength * 0.85f;
            k.Box(nape + new Vector3(0f, -len * 0.4f, -0.04f), new Vector3(h * 1.7f, len * 0.8f, 0.05f), hair, 0.75f, Quaternion.Euler(8f, 0f, 0f));
            for (int i = -1; i <= 1; i++)
            {
                var root = nape + new Vector3(i * h * 0.6f, -len * 0.75f, -0.1f);
                k.Limb(root, root + new Vector3(i * 0.03f, -0.16f - (i == 0 ? 0.06f : 0f), -0.04f), 0.05f, 0f, hair, 5);
            }
            // Side locks over the front of the shoulders.
            for (int side = -1; side <= 1; side += 2)
            {
                var root = f.Head + new Vector3(side * h * 0.85f, -h * 0.1f, h * 0.1f);
                var mid = root + new Vector3(side * 0.03f, -0.16f, 0.02f);
                k.Limb(root, mid, 0.035f, 0.03f, hair, 5);
                k.Limb(mid, mid + new Vector3(side * 0.01f, -0.14f, 0.03f), 0.03f, 0f, hair, 5);
            }
            k.Smooth = smooth;
        }

        /// <summary>A full beard that tapers to a point at the chest, and a moustache.</summary>
        static void PointedBeard(Figure f, Color hair)
        {
            float h = f.Spec.HeadSize;
            var k = f.Kit;
            bool smooth = k.Smooth;
            k.Smooth = false;
            k.Part(BodyPart.Head, f.NeckBase);
            k.Block(f.Head + new Vector3(0f, -h * 0.62f, h * 0.48f), new Vector3(h * 1.25f, h * 0.75f, h * 0.7f), hair, 0.45f);
            k.Limb(f.Head + new Vector3(0f, -h * 0.85f, h * 0.55f), f.Head + new Vector3(0f, -h * 2.0f, h * 0.75f), h * 0.42f, 0f, hair, 6);
            k.Box(f.Head + new Vector3(0f, -h * 0.34f, h * 0.9f), new Vector3(h * 0.6f, h * 0.1f, h * 0.08f), hair, 0.6f);
            k.Smooth = smooth;
            k.Body();
        }

        /// <summary>A dark metal band with spikes rising highest at the front centre, and a blue stone.</summary>
        static void SpikedCrown(Figure f, Color metal, Color stone)
        {
            float h = f.Spec.HeadSize;
            var k = f.Kit;
            bool smooth = k.Smooth;
            k.Smooth = false;
            k.Part(BodyPart.Head, f.NeckBase);
            var b = f.Head + new Vector3(0f, h * 0.55f, 0f);
            k.Lathe(b, new[] { new Vector2(0f, h * 0.9f), new Vector2(0.03f, h * 0.92f) }, 8, metal, 1f, Quaternion.Euler(0f, 22.5f, 0f));
            // Spikes from the front centre round to the sides; the front one is tallest and the back is bare.
            float[] heights = { 0.2f, 0.12f, 0.15f, 0.08f };
            for (int i = 0; i < heights.Length; i++)
                for (int side = -1; side <= 1; side += 2)
                {
                    if (i == 0 && side > 0) continue;
                    float a = Mathf.PI * 0.5f + side * i * 0.42f;
                    var root = b + new Vector3(Mathf.Cos(a) * h * 0.9f, 0.02f, Mathf.Sin(a) * h * 0.9f);
                    var tip = root + new Vector3(Mathf.Cos(a) * 0.02f, heights[i], Mathf.Sin(a) * 0.02f);
                    k.Limb(root, tip, h * (i == 0 ? 0.16f : 0.11f), 0f, metal, 4);
                }
            k.Gem(b + new Vector3(0f, 0.04f, h * 0.95f), new Vector3(0.022f, 0.03f, 0.014f), stone);
            k.Smooth = smooth;
            k.Body();
        }

        /// <summary>Skybreaker: a tall spear with a long diamond blade, a cross guard and a compass ring below the head.</summary>
        static void Skybreaker(Figure f, Color shaft, Color blade, Color gold)
        {
            var k = f.Kit;
            f.HeldPart(false);
            var hand = f.HandR;
            var bottom = hand - Vector3.up * 0.75f;
            var top = hand + Vector3.up * 1.1f;
            k.Limb(bottom, top, 0.018f, 0.018f, shaft, 6);
            k.Limb(bottom, bottom + Vector3.up * 0.06f, 0f, 0.022f, gold, 6);
            // Compass ring: a circle of short segments facing forward, with a bar and a stone through it.
            var ring = top - Vector3.up * 0.04f;
            const int n = 10;
            for (int i = 0; i < n; i++)
            {
                float a0 = i * Mathf.PI * 2f / n, a1 = (i + 1) * Mathf.PI * 2f / n;
                k.Limb(ring + new Vector3(Mathf.Cos(a0), Mathf.Sin(a0), 0f) * 0.09f, ring + new Vector3(Mathf.Cos(a1), Mathf.Sin(a1), 0f) * 0.09f, 0.012f, 0.012f, gold, 4);
            }
            k.Box(ring, new Vector3(0.2f, 0.016f, 0.014f), gold);
            k.Gem(ring, new Vector3(0.03f, 0.03f, 0.02f), blade);
            // Cross guard with upturned tips, then the blade: a flat diamond, long above and short below.
            var guard = top + Vector3.up * 0.09f;
            k.Box(guard, new Vector3(0.18f, 0.025f, 0.03f), gold);
            k.Limb(guard + new Vector3(-0.09f, 0f, 0f), guard + new Vector3(-0.12f, 0.06f, 0f), 0.012f, 0f, gold, 4);
            k.Limb(guard + new Vector3(0.09f, 0f, 0f), guard + new Vector3(0.12f, 0.06f, 0f), 0.012f, 0f, gold, 4);
            var mid = guard + Vector3.up * 0.1f;
            k.Box(mid + Vector3.up * 0.2f, new Vector3(0.11f, 0.4f, 0.025f), blade, 0f);
            k.Box(mid - Vector3.up * 0.04f, new Vector3(0.11f, 0.08f, 0.025f), blade, 0f, Quaternion.Euler(0f, 0f, 180f));
            k.Body();
        }

        /// <summary>
        /// The Storm god's finer layers: robe hem bands, white side drapes, runes on the centre panel,
        /// sash tails, a standing collar, a chain across the chest, bell cuffs, ridged pauldrons, an
        /// edged cape, loose strands of hair and beard braids.
        /// </summary>
        static void StormDetail(Figure f, Color white)
        {
            var s = f.Spec;
            var p = f.P;
            var k = f.Kit;
            Color gold = p.Trim, navy = p.Cloth, dark = p.Cloth2, metal = p.Metal * 0.8f, glow = p.Glow;
            float squash = s.TorsoDepth / s.TorsoWidth * 1.08f;
            k.Body();

            // Robe hem: a gold band with a white band above it.
            k.Lathe(Vector3.zero, new[] { new Vector2(0.032f, s.TorsoWidth * 2.03f), new Vector2(0.075f, s.TorsoWidth * 1.99f) }, 16, gold, squash);
            k.Lathe(Vector3.zero, new[] { new Vector2(0.1f, s.TorsoWidth * 1.95f), new Vector2(0.125f, s.TorsoWidth * 1.92f) }, 16, white, squash);

            // White drapes falling from the sash on either side of the front, edged in gold, with pointed ends.
            for (int side = -1; side <= 1; side += 2)
            {
                var rot = Quaternion.Euler(0f, side * 38f, 0f) * Quaternion.Euler(-13f, 0f, 0f);
                var c = new Vector3(side * 0.2f, 0.5f, 0.165f);
                k.Box(c, new Vector3(0.13f, 0.8f, 0.012f), white, 0.75f, rot);
                k.Box(c + rot * new Vector3(side * -0.06f, 0f, 0.008f), new Vector3(0.012f, 0.8f, 0.01f), gold, 1f, rot);
                k.Limb(c + rot * new Vector3(0f, -0.4f, 0f), c + rot * new Vector3(side * 0.02f, -0.5f, 0.01f), 0.05f, 0f, white, 4);
            }

            // Runes down the dark centre panel: diamonds, then a compass ring near the hem.
            float top = s.LegLength + s.TorsoLength * 0.1f, z = s.TorsoDepth * 1.25f + 0.08f;
            var lean = Quaternion.Euler(-9f, 0f, 0f);
            Vector3 OnPanel(float y) => new Vector3(0f, top * 0.5f, 0f) + lean * new Vector3(0f, y - top * 0.5f, z);
            foreach (float y in new[] { 0.72f, 0.62f, 0.52f })
                k.Gem(OnPanel(y), new Vector3(0.022f, 0.03f, 0.008f), gold);
            var rune = OnPanel(0.3f);
            for (int i = 0; i < 8; i++)
            {
                float a0 = i * Mathf.PI / 4f, a1 = (i + 1) * Mathf.PI / 4f;
                k.Limb(rune + lean * new Vector3(Mathf.Cos(a0), Mathf.Sin(a0), 0f) * 0.045f, rune + lean * new Vector3(Mathf.Cos(a1), Mathf.Sin(a1), 0f) * 0.045f, 0.006f, 0.006f, gold, 4);
            }
            k.Box(rune, new Vector3(0.012f, 0.11f, 0.01f), gold, 1f, lean);
            k.Box(rune, new Vector3(0.11f, 0.012f, 0.01f), gold, 1f, lean);
            k.Gem(rune + lean * Vector3.forward * 0.006f, new Vector3(0.014f, 0.014f, 0.008f), glow);

            // Sash tails hanging on the left hip, with gold ends and tassels.
            var hip = f.Torso(-s.TorsoWidth * 0.75f, s.TorsoLength * 0.08f, s.TorsoDepth * 0.95f);
            for (int i = 0; i < 2; i++)
            {
                float len = 0.32f - i * 0.08f;
                var rot = Quaternion.Euler(-6f, -30f, i * 6f);
                var c = hip + new Vector3(-0.02f + i * 0.035f, -len * 0.5f, 0.02f + i * 0.01f);
                k.Box(c, new Vector3(0.055f, len, 0.012f), navy, 1f, rot);
                var end = c + rot * new Vector3(0f, -len * 0.5f, 0f);
                k.Box(end + rot * new Vector3(0f, 0.02f, 0.004f), new Vector3(0.06f, 0.03f, 0.012f), gold, 1f, rot);
                k.Limb(end, end + Vector3.down * 0.07f, 0.012f, 0.02f, gold, 5);
            }

            // A standing collar behind the neck: three dark plates flaring back, rimmed in gold.
            for (int i = -1; i <= 1; i++)
            {
                var rot = f.TorsoRotation * Quaternion.Euler(-22f, i * 40f, 0f);
                var at = f.NeckBase + f.TorsoRotation * new Vector3(i * 0.075f, 0.05f, -0.07f + Mathf.Abs(i) * 0.03f);
                k.Block(at, new Vector3(0.1f, 0.13f, 0.02f), dark, 0.3f, rot);
                k.Box(at + rot * new Vector3(0f, 0.066f, 0f), new Vector3(0.1f, 0.012f, 0.026f), gold, 1f, rot);
            }

            // A gold chain across the chest between two clasps, sagging in the middle.
            var clL = f.Torso(-s.TorsoWidth * 0.7f, s.TorsoLength * 0.78f, s.TorsoDepth * 1.25f);
            var clR = f.Torso(s.TorsoWidth * 0.7f, s.TorsoLength * 0.78f, s.TorsoDepth * 1.25f);
            foreach (var cl in new[] { clL, clR })
            {
                k.Lathe(cl, new[] { new Vector2(0f, 0.028f), new Vector2(0.012f, 0.028f) }, 8, gold, 1f, f.TorsoRotation * Quaternion.Euler(90f, 0f, 0f));
                k.Gem(cl + f.TorsoRotation * Vector3.forward * 0.014f, new Vector3(0.014f, 0.014f, 0.008f), glow);
            }
            Vector3 prev = clL;
            for (int i = 1; i <= 6; i++)
            {
                float t = i / 6f;
                var pt = Vector3.Lerp(clL, clR, t) + Vector3.down * Mathf.Sin(t * Mathf.PI) * 0.05f + Vector3.forward * (0.01f + Mathf.Sin(t * Mathf.PI) * 0.045f);
                k.Limb(prev, pt, 0.006f, 0.006f, gold, 4);
                prev = pt;
            }

            // Ridges and gold rims on the shoulder plates.
            for (int side = 0; side < 2; side++)
            {
                bool left = side == 0;
                float sx = left ? -1f : 1f;
                f.ArmPart(left);
                var sh = left ? f.ShoulderL : f.ShoulderR;
                var ridge = sh + new Vector3(sx * 0.03f, 0.05f, 0f);
                k.Limb(ridge + Vector3.back * 0.08f, ridge + Vector3.forward * 0.08f, 0.012f, 0.012f, gold, 4);
                k.Limb(ridge, ridge + new Vector3(sx * 0.02f, 0.06f, 0f), 0.016f, 0f, metal, 4);
                k.Block(sh + new Vector3(sx * 0.1f, -0.06f, 0f), new Vector3(0.15f, 0.03f, 0.15f), metal * 0.9f, 0.5f, Quaternion.Euler(0f, 0f, sx * -55f));
            }

            // Bell cuffs: wide white lining flaring from the wrist, with a gold rim, and gold bands on the upper arm.
            for (int side = 0; side < 2; side++)
            {
                bool left = side == 0;
                f.ArmPart(left);
                Vector3 sh = left ? f.ShoulderL : f.ShoulderR, el = left ? f.ElbowL : f.ElbowR, wr = left ? f.WristL : f.WristR;
                var dir = (wr - el).normalized;
                var a = wr - dir * 0.12f;
                var b = wr - dir * 0.01f;
                k.Limb(a, b, s.ArmThickness * 1.7f, s.ArmThickness * 2.4f, navy, 8);
                k.Limb(b - dir * 0.012f, b + dir * 0.004f, s.ArmThickness * 2.42f, s.ArmThickness * 2.42f, gold, 8);
                k.Limb(b - dir * 0.02f, b - dir * 0.005f, s.ArmThickness * 2.2f, s.ArmThickness * 2.2f, white, 8);
                var up = Vector3.Lerp(sh, el, 0.62f);
                var ud = (el - sh).normalized;
                k.Limb(up - ud * 0.012f, up + ud * 0.012f, s.ArmThickness * 1.48f, s.ArmThickness * 1.42f, gold, 8);
            }

            // Gold edging down the sides and along the bottom of the cape (the same corners as Figure.Cloak).
            k.Body();
            var ct = f.Torso(0f, s.TorsoLength * 0.92f, -s.TorsoDepth * 0.85f);
            float w = s.ShoulderWidth * 1.15f;
            Vector3 tl = ct + new Vector3(-w, 0f, 0f), tr = ct + new Vector3(w, 0f, 0f);
            Vector3 bl = new Vector3(-w * 1.25f, 0.04f, ct.z - 0.18f), br = new Vector3(w * 1.25f, 0.04f, ct.z - 0.18f);
            Vector3 ml = Vector3.Lerp(tl, bl, 0.5f) + new Vector3(0f, 0f, -0.07f), mr = Vector3.Lerp(tr, br, 0.5f) + new Vector3(0f, 0f, -0.07f);
            var bc = (bl + br) * 0.5f + new Vector3(0f, 0f, -0.05f);
            Vector3 o = new Vector3(0f, 0f, -0.006f);
            k.Limb(tl + o, ml + o, 0.01f, 0.01f, gold, 4); k.Limb(ml + o, bl + o, 0.01f, 0.01f, gold, 4);
            k.Limb(tr + o, mr + o, 0.01f, 0.01f, gold, 4); k.Limb(mr + o, br + o, 0.01f, 0.01f, gold, 4);
            k.Limb(bl + o, bc + o, 0.012f, 0.012f, gold, 4); k.Limb(bc + o, br + o, 0.012f, 0.012f, gold, 4);
            // A storm sigil on the back of the cape: a ring with a cross.
            var sig = Vector3.Lerp((ml + mr) * 0.5f, bc, 0.15f) + new Vector3(0f, 0f, -0.075f);
            for (int i = 0; i < 8; i++)
            {
                float a0 = i * Mathf.PI / 4f, a1 = (i + 1) * Mathf.PI / 4f;
                k.Limb(sig + new Vector3(Mathf.Cos(a0), Mathf.Sin(a0), 0f) * 0.08f, sig + new Vector3(Mathf.Cos(a1), Mathf.Sin(a1), 0f) * 0.08f, 0.008f, 0.008f, gold, 4);
            }
            k.Box(sig, new Vector3(0.014f, 0.2f, 0.01f), gold);
            k.Box(sig, new Vector3(0.2f, 0.014f, 0.01f), gold);

            // Loose strands of hair over the back, and two thin braids beside the beard.
            bool smooth = k.Smooth;
            k.Smooth = false;
            float h = s.HeadSize;
            for (int i = 0; i < 5; i++)
            {
                float x = (i - 2) * h * 0.38f;
                var root = f.Head + new Vector3(x, -h * 0.2f, -h * 0.9f);
                var tip = root + new Vector3(x * 0.6f, -0.3f - (i % 2) * 0.08f, -0.08f);
                k.Limb(root, tip, 0.03f, 0f, p.Hair * (i % 2 == 0 ? 1f : 0.92f), 4);
            }
            k.Part(BodyPart.Head, f.NeckBase);
            for (int side = -1; side <= 1; side += 2)
                for (int i = 0; i < 3; i++)
                    k.Gem(f.Head + new Vector3(side * h * 0.52f, -h * (1.0f + i * 0.32f), h * 0.62f), new Vector3(h * 0.13f, h * 0.18f, h * 0.13f), p.Hair * (i % 2 == 0 ? 1f : 0.9f));
            // More stones on the crown.
            var band = f.Head + new Vector3(0f, h * 0.55f + 0.015f, 0f);
            for (int side = -1; side <= 1; side += 2)
                k.Gem(band + new Vector3(side * h * 0.62f, 0f, h * 0.7f), new Vector3(0.012f, 0.016f, 0.008f), glow);
            k.Smooth = smooth;

            // Skybreaker: gold bands up the grip, side prongs on the ring and a tassel below it.
            f.HeldPart(false);
            var hand = f.HandR;
            foreach (float y in new[] { -0.2f, 0.12f, 0.25f, 0.38f })
                k.Limb(hand + Vector3.up * y, hand + Vector3.up * (y + 0.02f), 0.023f, 0.023f, gold, 6);
            var ring = hand + Vector3.up * 1.06f;
            for (int side = -1; side <= 1; side += 2)
                k.Limb(ring + new Vector3(side * 0.095f, 0f, 0f), ring + new Vector3(side * 0.15f, 0.03f, 0f), 0.014f, 0f, gold, 4);
            k.Limb(ring + Vector3.down * 0.095f, ring + Vector3.down * 0.13f, 0.006f, 0.006f, gold, 4);
            k.Limb(ring + Vector3.down * 0.13f, ring + Vector3.down * 0.22f, 0.012f, 0.022f, navy, 5);
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
