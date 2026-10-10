using System.Collections.Generic;
using UnityEngine;

namespace OldGods.Runtime
{
    public enum EnemyModel { Husk, Runner, Brute, Ghost, Ashling, Cinder, Hulk, Drowned, BrineRunner, ShellBrute, Champion, AshChampion, TideChampion }

    /// <summary>
    /// Enemy bodies built in code (made for this project, so licence-safe): dressed, jointed
    /// figures with monster faces, claws, rags, armour and weapons, coloured in the mesh. All face
    /// +z with feet at y = 0 and are about 1.6 m before the enemy's scale. Legs and arms are tagged
    /// so the horde shader walks them.
    /// </summary>
    public static class EnemyModels
    {
        static readonly Dictionary<EnemyModel, Mesh> cache = new Dictionary<EnemyModel, Mesh>();

        public static Mesh Get(EnemyModel model)
        {
            if (cache.TryGetValue(model, out var m) && m != null) return m;
            var k = new MeshKit();
            switch (model)
            {
                case EnemyModel.Husk: Husk(k, HuskPalette); break;
                case EnemyModel.Runner: Runner(k); break;
                case EnemyModel.Brute: Brute(k, BrutePalette, false); break;
                case EnemyModel.Ghost: Ghost(k); break;
                case EnemyModel.Ashling: Ashling(k); break;
                case EnemyModel.Cinder: Cinder(k); break;
                case EnemyModel.Hulk: Hulk(k); break;
                case EnemyModel.Drowned: Drowned(k, false); break;
                case EnemyModel.BrineRunner: BrineRunner(k); break;
                case EnemyModel.ShellBrute: Brute(k, ShellPalette, true); break;
                case EnemyModel.Champion: Champion(k); break;
                case EnemyModel.AshChampion: AshChampion(k); break;
                case EnemyModel.TideChampion: Drowned(k, true); break;
            }
            m = k.Build("Enemy" + model);
            cache[model] = m;
            return m;
        }

        static Palette HuskPalette => new Palette
        {
            Skin = new Color(0.62f, 0.52f, 0.46f), SkinShade = new Color(0.46f, 0.38f, 0.34f),
            Cloth = new Color(0.42f, 0.36f, 0.3f), Cloth2 = new Color(0.32f, 0.28f, 0.25f),
            Leather = new Color(0.36f, 0.25f, 0.18f), Metal = new Color(0.55f, 0.55f, 0.57f), Trim = new Color(0.7f, 0.62f, 0.5f),
            Hair = new Color(0.2f, 0.18f, 0.16f), Eye = new Color(0.92f, 0.88f, 0.78f), Pupil = new Color(0.12f, 0.1f, 0.1f), Glow = new Color(1f, 0.85f, 0.45f),
        };

        static Palette BrutePalette
        {
            get
            {
                var p = HuskPalette;
                p.Skin = new Color(0.5f, 0.52f, 0.58f); p.SkinShade = new Color(0.38f, 0.4f, 0.45f);
                p.Cloth = new Color(0.45f, 0.3f, 0.22f); p.Glow = new Color(1f, 0.55f, 0.3f);
                return p;
            }
        }

        static Palette AshPalette
        {
            get
            {
                var p = HuskPalette;
                p.Skin = new Color(0.25f, 0.22f, 0.21f); p.SkinShade = new Color(0.16f, 0.14f, 0.14f);
                p.Cloth = new Color(0.3f, 0.2f, 0.16f); p.Cloth2 = new Color(0.2f, 0.17f, 0.16f);
                p.Glow = new Color(1f, 0.5f, 0.15f); p.Metal = new Color(0.25f, 0.24f, 0.24f);
                return p;
            }
        }

        static Palette SeaPalette
        {
            get
            {
                var p = HuskPalette;
                p.Skin = new Color(0.5f, 0.62f, 0.58f); p.SkinShade = new Color(0.38f, 0.48f, 0.46f);
                p.Cloth = new Color(0.4f, 0.42f, 0.38f); p.Cloth2 = new Color(0.32f, 0.35f, 0.34f);
                p.Hair = new Color(0.2f, 0.32f, 0.22f); p.Glow = new Color(0.6f, 1f, 0.9f); p.Trim = new Color(0.9f, 0.86f, 0.76f);
                return p;
            }
        }

        static Palette ShellPalette
        {
            get
            {
                var p = SeaPalette;
                p.Skin = new Color(0.62f, 0.5f, 0.45f); p.SkinShade = new Color(0.48f, 0.38f, 0.34f);
                return p;
            }
        }

        static void Rags(Figure f, Color c, float drop)
        {
            f.Tabard(c, drop, 0.16f);
            var k = f.Kit;
            k.Body();
            k.Box(f.Torso(0f, f.Spec.TorsoLength * 0.1f, -f.Spec.TorsoDepth * 1.1f) + new Vector3(0f, -drop * 0.45f, -0.02f), new Vector3(0.16f, drop * 0.9f, 0.015f), c * 0.9f, 0.85f, Quaternion.Euler(8f, 0f, 0f));
        }

        static void Bandages(Figure f, Color c)
        {
            var k = f.Kit;
            var s = f.Spec;
            for (int side = 0; side < 2; side++)
            {
                bool left = side == 0;
                f.ArmPart(left);
                var el = left ? f.ElbowL : f.ElbowR;
                var wr = left ? f.WristL : f.WristR;
                for (int i = 0; i < 3; i++)
                {
                    var at = Vector3.Lerp(el, wr, 0.25f + i * 0.25f);
                    k.Limb(at - (wr - el).normalized * 0.012f, at + (wr - el).normalized * 0.012f, s.ArmThickness * 1.05f, s.ArmThickness * 1.05f, c, 7);
                }
            }
            k.Body();
        }

        static Figure Husk(MeshKit k, Palette p)
        {
            var f = new Figure(k, FigureSpec.Husk, p).Layout().Body().MonsterFace();
            f.Trousers(p.Cloth2, 1.05f);
            f.Belt(p.Leather, p.Metal, 1);
            Rags(f, p.Cloth, 0.3f);
            Bandages(f, p.Trim);
            f.Hair(p.Hair, 0.7f).Spines(3, 0.06f, p.SkinShade);
            // Exposed ribs.
            for (int i = 0; i < 3; i++)
            {
                float y = f.Spec.TorsoLength * (0.48f + i * 0.11f);
                k.Limb(f.Torso(-f.Spec.TorsoWidth * 0.75f, y, f.Spec.TorsoDepth * 0.82f), f.Torso(f.Spec.TorsoWidth * 0.75f, y, f.Spec.TorsoDepth * 0.82f), 0.016f, 0.016f, p.Trim, 5);
            }
            return f;
        }

        static void Runner(MeshKit k)
        {
            var p = HuskPalette;
            p.Skin = new Color(0.72f, 0.5f, 0.42f); p.SkinShade = new Color(0.55f, 0.36f, 0.3f); p.Cloth = new Color(0.55f, 0.2f, 0.16f);
            var s = FigureSpec.Husk;
            s.LegLength = 0.8f; s.LegThickness = 0.06f; s.Hunch = 46f; s.TorsoLength = 0.46f; s.TorsoWidth = 0.16f; s.TorsoDepth = 0.12f;
            s.ArmLength = 0.6f; s.ArmThickness = 0.042f; s.HandSize = 0.07f; s.HeadSize = 0.12f; s.ShoulderWidth = 0.18f;
            var f = new Figure(k, s, p).Layout().Body().MonsterFace(1.2f);
            f.Trousers(p.Cloth2, 1f);
            // A long scarf trailing behind.
            k.Body();
            var neck = f.NeckBase;
            k.Lathe(neck - Vector3.up * 0.04f, new[] { new Vector2(0f, s.HeadSize * 0.85f), new Vector2(0.08f, s.HeadSize * 0.8f) }, 12, p.Cloth, 1f, f.TorsoRotation);
            k.Limb(neck + new Vector3(0f, 0f, -0.08f), neck + new Vector3(0.05f, -0.1f, -0.45f), 0.045f, 0.03f, p.Cloth, 6);
            f.Spines(5, 0.09f, p.Hair).Ears(p.Skin, 0.12f);
        }

        static void Brute(MeshKit k, Palette p, bool shell)
        {
            var s = FigureSpec.Husk;
            s.LegLength = 0.6f; s.LegThickness = 0.12f; s.HipWidth = 0.18f; s.TorsoLength = 0.7f; s.TorsoWidth = 0.38f; s.TorsoDepth = 0.28f;
            s.Hunch = shell ? 30f : 16f; s.ShoulderWidth = 0.42f; s.ArmLength = 0.82f; s.ArmThickness = 0.1f; s.HandSize = 0.13f; s.HeadSize = 0.12f; s.NeckLength = 0.01f;
            var f = new Figure(k, s, p).Layout().Body().MonsterFace(0.9f);
            f.Trousers(p.Cloth, 1.1f).Boots(p.Leather, 0.18f, p.Metal);
            f.Belt(p.Leather, p.Metal, 3).Strap(p.Leather);
            // Bracers and one heavy pauldron.
            f.ArmPart(false);
            k.Block(f.ShoulderR + new Vector3(0.07f, 0.08f, 0f), new Vector3(0.36f, 0.09f, 0.34f), p.Metal, 0.5f, Quaternion.Euler(0f, 0f, -25f));
            k.Block(f.ShoulderR + new Vector3(0.13f, 0.0f, 0f), new Vector3(0.3f, 0.08f, 0.3f), p.Metal * 0.85f, 0.5f, Quaternion.Euler(0f, 0f, -42f));
            for (int side = 0; side < 2; side++)
            {
                bool left = side == 0;
                f.ArmPart(left);
                var el = left ? f.ElbowL : f.ElbowR;
                var wr = left ? f.WristL : f.WristR;
                k.Limb(Vector3.Lerp(el, wr, 0.35f), wr, s.ArmThickness * 1.2f, s.ArmThickness * 1.15f, p.Leather, 9);
            }
            k.Body();
            if (shell)
            {
                var c = f.Torso(0f, s.TorsoLength * 0.55f, -s.TorsoDepth * 1.05f);
                k.Smooth = false;
                k.Ball(c, new Vector3(s.TorsoWidth * 1.25f, s.TorsoLength * 0.62f, s.TorsoDepth * 0.95f), p.Trim, 12, 6, f.TorsoRotation, 0.05f, 3);
                k.Smooth = true;
                for (int i = 0; i < 4; i++)
                {
                    var at = f.Torso(-0.25f + i * 0.17f, s.TorsoLength * (0.4f + (i % 2) * 0.3f), -s.TorsoDepth * 1.85f);
                    k.Limb(at, at + f.TorsoRotation * new Vector3(0f, 0.08f, -0.1f), 0.03f, 0f, new Color(0.95f, 0.55f, 0.5f), 5);
                }
            }
            else f.Spines(3, 0.1f, p.SkinShade);
        }

        static void Ghost(MeshKit k)
        {
            var p = HuskPalette;
            p.Cloth = new Color(0.78f, 0.86f, 0.92f); p.Cloth2 = new Color(0.6f, 0.68f, 0.76f); p.Skin = new Color(0.86f, 0.9f, 0.94f);
            p.Pupil = new Color(0.08f, 0.1f, 0.14f); p.Glow = new Color(0.7f, 0.95f, 1f);
            var s = FigureSpec.Husk;
            s.Hunch = 8f;
            var f = new Figure(k, s, p).Layout();
            // A drifting shroud: no legs, long sleeves with bony hands, a deep hood.
            k.Body();
            k.Lathe(new Vector3(0f, 0.12f, 0f), new[] { new Vector2(0f, 0.36f), new Vector2(0.2f, 0.3f), new Vector2(0.6f, 0.24f), new Vector2(1.05f, 0.24f), new Vector2(1.28f, 0.14f) }, 16, p.Cloth, 0.82f);
            for (int i = 0; i < 10; i++)
            {
                float a = i * Mathf.PI * 2f / 10f;
                var root = new Vector3(Mathf.Cos(a) * 0.33f, 0.16f, Mathf.Sin(a) * 0.28f);
                k.Limb(root, root + new Vector3(Mathf.Cos(a) * 0.06f, -0.16f, Mathf.Sin(a) * 0.05f), 0.055f, 0f, p.Cloth2, 5);
            }
            f.Sleeves(p.Cloth, false, p.Cloth2);
            for (int side = 0; side < 2; side++)
            {
                bool left = side == 0;
                f.ArmPart(left);
                f.Hand(left ? f.HandL : f.HandR, left, p.Skin);
            }
            k.Body();
            f.Hood(p.Cloth);
            k.Part(BodyPart.Head, f.NeckBase);
            k.Ball(f.Head + new Vector3(0f, -0.01f, s.HeadSize * 0.55f), new Vector3(s.HeadSize * 0.72f, s.HeadSize * 0.78f, s.HeadSize * 0.45f), p.Pupil, 10, 6);
            for (int side = -1; side <= 1; side += 2)
                k.Ball(f.Head + new Vector3(side * s.HeadSize * 0.3f, s.HeadSize * 0.05f, s.HeadSize * 0.92f), new Vector3(0.022f, 0.016f, 0.01f), p.Glow, 6, 3);
            k.Body();
        }

        static void Ashling(MeshKit k)
        {
            var p = AshPalette;
            var s = FigureSpec.Husk;
            s.LegLength = 0.38f; s.LegThickness = 0.05f; s.HipWidth = 0.09f; s.TorsoLength = 0.34f; s.TorsoWidth = 0.2f; s.TorsoDepth = 0.18f;
            s.Hunch = 26f; s.ShoulderWidth = 0.18f; s.ArmLength = 0.38f; s.ArmThickness = 0.038f; s.HandSize = 0.055f; s.HeadSize = 0.16f; s.NeckLength = 0.01f;
            var f = new Figure(k, s, p).Layout().Body().MonsterFace(1.5f, false);
            f.Horns(0.14f, 0.03f, p.SkinShade * 2f, 0.6f).Spines(5, 0.12f, p.SkinShade).Tail(p.Skin, p.Glow, 0.3f);
            // Glowing cracks on the chest.
            k.Body();
            for (int i = 0; i < 3; i++)
                k.Box(f.Torso((i - 1) * 0.06f, s.TorsoLength * (0.4f + i * 0.12f), s.TorsoDepth * 1.02f), new Vector3(0.012f, 0.06f, 0.01f), p.Glow, 1f, f.TorsoRotation * Quaternion.Euler(0f, 0f, (i - 1) * 25f));
        }

        static void Cinder(MeshKit k)
        {
            var p = AshPalette;
            p.Skin = new Color(0.32f, 0.27f, 0.25f);
            var f = Husk(k, p);
            f.Horns(0.16f, 0.03f, p.SkinShade * 1.8f);
            for (int i = 0; i < 4; i++)
                k.Box(f.Torso((i % 2 == 0 ? -1 : 1) * 0.07f, f.Spec.TorsoLength * (0.35f + i * 0.13f), f.Spec.TorsoDepth * 1.05f), new Vector3(0.01f, 0.07f, 0.01f), p.Glow, 1f, f.TorsoRotation * Quaternion.Euler(0f, 0f, i * 35f));
        }

        static void Hulk(MeshKit k)
        {
            var p = AshPalette;
            var s = FigureSpec.Husk;
            s.LegLength = 0.55f; s.LegThickness = 0.13f; s.HipWidth = 0.2f; s.TorsoLength = 0.78f; s.TorsoWidth = 0.42f; s.TorsoDepth = 0.32f;
            s.Hunch = 32f; s.ShoulderWidth = 0.46f; s.ArmLength = 0.95f; s.ArmThickness = 0.12f; s.HandSize = 0.16f; s.HeadSize = 0.13f; s.NeckLength = 0f;
            var f = new Figure(k, s, p).Layout().Body().MonsterFace(0.9f);
            f.Trousers(p.Cloth, 1.1f).Belt(p.Metal, p.Glow, 2);
            f.Horns(0.3f, 0.06f, p.SkinShade * 2f, 0.85f).Spines(6, 0.18f, p.SkinShade);
            // A chain across the chest.
            for (int i = 0; i < 7; i++)
            {
                var at = Vector3.Lerp(f.Torso(-s.TorsoWidth * 0.95f, s.TorsoLength * 0.9f, s.TorsoDepth * 0.6f), f.Torso(s.TorsoWidth * 0.95f, s.TorsoLength * 0.15f, s.TorsoDepth * 1.05f), i / 6f);
                k.Ball(at, new Vector3(0.035f, 0.025f, 0.02f), p.Metal, 6, 3);
            }
            for (int i = 0; i < 4; i++)
                k.Box(f.Torso((i - 1.5f) * 0.1f, s.TorsoLength * 0.55f, s.TorsoDepth * 1.08f), new Vector3(0.012f, 0.12f, 0.01f), p.Glow, 1f, f.TorsoRotation * Quaternion.Euler(0f, 0f, i * 20f - 30f));
        }

        static void Drowned(MeshKit k, bool champion)
        {
            var p = SeaPalette;
            var s = FigureSpec.Husk;
            s.Hunch = champion ? 16f : 28f; s.TorsoWidth = 0.23f; s.TorsoDepth = 0.18f; s.HeadSize = 0.14f;
            if (champion) { s.ShoulderWidth = 0.27f; s.ArmThickness = 0.065f; s.LegThickness = 0.085f; }
            var f = new Figure(k, s, p).Layout().Body().MonsterFace(1f, !champion);
            f.Trousers(p.Cloth, 1.15f).Boots(p.Cloth2, 0.3f, p.Leather);
            f.Belt(p.Leather, p.Metal, 2);
            // Weed hair hanging over the face and shoulders.
            k.Part(BodyPart.Head, f.NeckBase);
            for (int i = 0; i < 9; i++)
            {
                float a = Mathf.Lerp(-2.2f, 2.2f, i / 8f);
                var root = f.Head + new Vector3(Mathf.Sin(a) * s.HeadSize * 0.9f, s.HeadSize * 0.6f, -Mathf.Cos(a) * s.HeadSize * 0.6f);
                k.Limb(root, root + new Vector3(Mathf.Sin(a) * 0.04f, -0.32f, -Mathf.Cos(a) * 0.05f), 0.022f, 0.006f, p.Hair, 5);
            }
            k.Body();
            // Barnacles.
            var rnd = new System.Random(4);
            for (int i = 0; i < 9; i++)
            {
                var at = f.Torso(((float)rnd.NextDouble() - 0.5f) * 0.4f, (float)rnd.NextDouble() * 0.55f, ((float)rnd.NextDouble() > 0.5f ? 1f : -1f) * 0.18f);
                k.Lathe(at, new[] { new Vector2(0f, 0.035f), new Vector2(0.03f, 0.018f) }, 6, p.Trim);
            }
            if (champion)
            {
                f.Pauldrons(p.Trim, 0.13f, new Color(0.95f, 0.6f, 0.55f));
                f.Breastplate(p.Trim, p.Metal);
                k.Part(BodyPart.Head, f.NeckBase);
                for (int i = 0; i < 6; i++)
                {
                    float a = Mathf.Lerp(-1f, 1f, i / 5f);
                    var root = f.Head + new Vector3(Mathf.Sin(a) * s.HeadSize * 0.8f, s.HeadSize * 0.7f, 0f);
                    k.Limb(root, root + new Vector3(Mathf.Sin(a) * 0.08f, 0.2f - Mathf.Abs(a) * 0.06f, -0.04f), 0.02f, 0f, new Color(0.95f, 0.55f, 0.5f), 5);
                }
                k.Body();
                f.Weapon("trident", 1.5f, p.Trim, p.Leather);
            }
        }

        static void BrineRunner(MeshKit k)
        {
            var shell = new Color(0.78f, 0.42f, 0.32f);
            var under = new Color(0.92f, 0.78f, 0.66f);
            var dark = new Color(0.4f, 0.2f, 0.16f);
            var c = new Vector3(0f, 0.55f, 0f);
            k.Body();
            k.Ball(c, new Vector3(0.42f, 0.17f, 0.34f), shell, 16, 8);
            k.Ball(c - Vector3.up * 0.06f, new Vector3(0.38f, 0.1f, 0.3f), under, 14, 6);
            for (int i = -2; i <= 2; i++)
                k.Box(c + new Vector3(i * 0.08f, 0.16f, -0.02f), new Vector3(0.025f, 0.02f, 0.4f - Mathf.Abs(i) * 0.06f), dark, 1f);
            // Eye stalks.
            for (int side = -1; side <= 1; side += 2)
            {
                var root = c + new Vector3(side * 0.09f, 0.12f, 0.28f);
                k.Limb(root, root + new Vector3(side * 0.02f, 0.12f, 0.03f), 0.016f, 0.014f, shell, 6);
                k.Ball(root + new Vector3(side * 0.02f, 0.14f, 0.035f), Vector3.one * 0.03f, Color.black, 8, 5);
            }
            void Leg(BodyPart part, float side, float z, float zTip)
            {
                var hip = c + new Vector3(side * 0.3f, -0.03f, z);
                k.Part(part, hip);
                var knee = hip + new Vector3(side * 0.22f, 0.14f, (zTip - z) * 0.5f);
                var foot = new Vector3(hip.x + side * 0.36f, 0.02f, zTip);
                k.Limb(hip, knee, 0.04f, 0.035f, shell, 7);
                k.Ball(knee, Vector3.one * 0.038f, dark, 7, 4);
                k.Limb(knee, foot, 0.035f, 0f, shell, 7);
            }
            Leg(BodyPart.LeftLeg, -1f, 0.14f, 0.3f);
            Leg(BodyPart.LeftLeg, 1f, -0.02f, -0.05f);
            Leg(BodyPart.LeftLeg, -1f, -0.18f, -0.34f);
            Leg(BodyPart.RightLeg, 1f, 0.14f, 0.3f);
            Leg(BodyPart.RightLeg, -1f, -0.02f, -0.05f);
            Leg(BodyPart.RightLeg, 1f, -0.18f, -0.34f);
            for (int side = -1; side <= 1; side += 2)
            {
                var root = c + new Vector3(side * 0.26f, 0f, 0.26f);
                k.Part(side < 0 ? BodyPart.LeftArm : BodyPart.RightArm, root);
                var elbow = root + new Vector3(side * 0.12f, 0.08f, 0.18f);
                k.Limb(root, elbow, 0.045f, 0.04f, shell, 8);
                var claw = elbow + new Vector3(side * 0.02f, 0.02f, 0.14f);
                k.Ball(claw, new Vector3(0.075f, 0.06f, 0.12f), shell, 10, 6);
                k.Limb(claw + new Vector3(0f, 0.03f, 0.08f), claw + new Vector3(side * 0.02f, 0.04f, 0.22f), 0.035f, 0f, under, 6);
                k.Limb(claw + new Vector3(0f, -0.02f, 0.08f), claw + new Vector3(side * 0.01f, -0.02f, 0.19f), 0.028f, 0f, under, 6);
            }
            k.Body();
        }

        static void Champion(MeshKit k)
        {
            var p = HuskPalette;
            p.Metal = new Color(0.42f, 0.44f, 0.5f); p.Cloth = new Color(0.55f, 0.15f, 0.12f); p.Trim = new Color(0.85f, 0.7f, 0.35f);
            var s = FigureSpec.Husk;
            s.Hunch = 8f; s.TorsoWidth = 0.24f; s.TorsoDepth = 0.17f; s.ShoulderWidth = 0.27f; s.ArmThickness = 0.06f; s.LegThickness = 0.08f; s.Claws = false;
            var f = new Figure(k, s, p).Layout().Body().MonsterFace(1f, false);
            f.Trousers(p.Cloth2, 1.1f).Boots(p.Metal * 0.8f, 0.36f, p.Metal);
            f.Breastplate(p.Metal, p.Trim).Pauldrons(p.Metal, 0.13f, p.Trim).Gloves(p.Metal * 0.75f);
            f.Belt(p.Leather, p.Trim, 2).Tabard(p.Cloth, 0.5f, 0.16f);
            f.Cloak(p.Cloth, 0.3f, p.Cloth2);
            // Helm with a crest.
            k.Part(BodyPart.Head, f.NeckBase);
            k.Ball(f.Head + Vector3.up * s.HeadSize * 0.15f, new Vector3(s.HeadSize * 1.1f, s.HeadSize * 0.95f, s.HeadSize * 1.12f), p.Metal, 14, 8);
            k.Box(f.Head + new Vector3(0f, s.HeadSize * 0.02f, s.HeadSize * 1.02f), new Vector3(s.HeadSize * 1.2f, s.HeadSize * 0.12f, 0.03f), p.Pupil);
            k.Lathe(f.Head + new Vector3(0f, s.HeadSize * 0.95f, -0.02f), new[] { new Vector2(0f, 0.02f), new Vector2(0.12f, 0.015f) }, 6, p.Cloth, 6f);
            k.Body();
            f.Weapon("sword", 0.95f, p.Metal, p.Leather);
        }

        static void AshChampion(MeshKit k)
        {
            var p = AshPalette;
            var s = FigureSpec.Husk;
            s.LegLength = 0.58f; s.LegThickness = 0.13f; s.HipWidth = 0.2f; s.TorsoLength = 0.74f; s.TorsoWidth = 0.4f; s.TorsoDepth = 0.3f;
            s.Hunch = 22f; s.ShoulderWidth = 0.44f; s.ArmLength = 0.9f; s.ArmThickness = 0.12f; s.HandSize = 0.15f; s.HeadSize = 0.14f; s.Claws = false;
            var f = new Figure(k, s, p).Layout().Body().MonsterFace(1f);
            f.Trousers(p.Cloth, 1.1f).Boots(p.Metal, 0.25f, p.Glow);
            f.Breastplate(p.Metal, p.Glow).Pauldrons(p.Metal, 0.2f, p.Glow).Gloves(p.Metal);
            f.Belt(p.Leather, p.Glow, 2).Horns(0.38f, 0.07f, p.SkinShade * 2.2f, 0.85f);
            f.Weapon("axe", 1.05f, p.Metal, p.Leather);
        }
    }
}
