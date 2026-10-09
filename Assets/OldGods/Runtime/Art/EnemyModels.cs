using System.Collections.Generic;
using UnityEngine;

namespace OldGods.Runtime
{
    public enum EnemyModel { Husk, Runner, Brute, Ghost, Ashling, Cinder, Hulk, Drowned, BrineRunner, ShellBrute, Champion, AshChampion, TideChampion }

    /// <summary>
    /// Enemy bodies built in code (made for this project, so licence-safe): jointed figures
    /// with faceted muscles, hands, faces and gear. All face +z with feet at y = 0 and are about
    /// 1.6 m tall before the enemy's scale. Legs and arms are tagged so the horde shader walks them.
    /// </summary>
    public static class EnemyModels
    {
        static readonly Dictionary<EnemyModel, Mesh> cache = new Dictionary<EnemyModel, Mesh>();

        static readonly Color Pale = new Color(1f, 1f, 1f);
        static readonly Color Bone = new Color(0.92f, 0.9f, 0.84f);
        static readonly Color Black = new Color(0.22f, 0.22f, 0.22f);
        static readonly Color Metal = new Color(0.75f, 0.75f, 0.78f);
        static readonly Color Wood = new Color(0.5f, 0.42f, 0.34f);
        static readonly Color GlowEye = new Color(1f, 0.95f, 0.75f);

        public static Mesh Get(EnemyModel model)
        {
            if (cache.TryGetValue(model, out var m) && m != null) return m;
            var k = new MeshKit();
            switch (model)
            {
                case EnemyModel.Husk: Husk(k); break;
                case EnemyModel.Runner: Runner(k); break;
                case EnemyModel.Brute: Brute(k); break;
                case EnemyModel.Ghost: Ghost(k); break;
                case EnemyModel.Ashling: Ashling(k); break;
                case EnemyModel.Cinder: Cinder(k); break;
                case EnemyModel.Hulk: Hulk(k); break;
                case EnemyModel.Drowned: Drowned(k); break;
                case EnemyModel.BrineRunner: BrineRunner(k); break;
                case EnemyModel.ShellBrute: ShellBrute(k); break;
                case EnemyModel.Champion: Champion(k); break;
                case EnemyModel.AshChampion: AshChampion(k); break;
                case EnemyModel.TideChampion: TideChampion(k); break;
            }
            m = k.Build("Enemy" + model);
            cache[model] = m;
            return m;
        }

        static Figure Husk(MeshKit k)
        {
            var f = new Figure(k, FigureSpec.Husk).Build();
            // Ribs and a ragged loincloth.
            for (int i = 0; i < 3; i++)
            {
                float y = f.Spec.TorsoLength * (0.45f + i * 0.12f);
                k.Limb(f.Torso(-f.Spec.TorsoWidth * 0.8f, y, f.Spec.TorsoDepth * 0.6f), f.Torso(f.Spec.TorsoWidth * 0.8f, y, f.Spec.TorsoDepth * 0.6f), 0.02f, 0.02f, f.Spec.Dark, 4);
            }
            k.Box(f.Pelvis + new Vector3(0f, -0.12f, 0.06f), new Vector3(0.3f, 0.24f, 0.2f), f.Spec.Accent, 0.9f);
            f.Spines(4, 0.08f, f.Spec.Dark);
            return f;
        }

        static void Runner(MeshKit k)
        {
            var s = FigureSpec.Husk;
            s.LegLength = 0.8f; s.LegThickness = 0.07f; s.Hunch = 48f; s.TorsoLength = 0.5f; s.TorsoWidth = 0.22f; s.TorsoDepth = 0.17f;
            s.ArmLength = 0.55f; s.ArmThickness = 0.05f; s.HeadSize = 0.13f; s.ShoulderWidth = 0.2f;
            var f = new Figure(k, s).Build();
            // A long jaw and a crest: built for running down prey.
            k.Part(BodyPart.Head, f.NeckBase);
            k.Box(f.Head + new Vector3(0f, -0.06f, 0.16f), new Vector3(0.12f, 0.08f, 0.22f), s.Dark, 0.7f);
            k.Limb(f.Head + new Vector3(0f, 0.1f, 0f), f.Head + new Vector3(0f, 0.12f, -0.3f), 0.04f, 0f, s.Accent, 4);
            k.Body();
            f.Spines(5, 0.1f, s.Accent);
        }

        static void Brute(MeshKit k)
        {
            var s = FigureSpec.Husk;
            s.LegLength = 0.6f; s.LegThickness = 0.14f; s.HipWidth = 0.2f; s.TorsoLength = 0.75f; s.TorsoWidth = 0.48f; s.TorsoDepth = 0.34f;
            s.Hunch = 18f; s.ShoulderWidth = 0.46f; s.ArmLength = 0.8f; s.ArmThickness = 0.12f; s.HandSize = 0.15f; s.HeadSize = 0.13f;
            var f = new Figure(k, s).Build();
            // Muscled shoulders and a small, sunk head.
            k.Ball(f.Torso(-0.3f, 0.66f, -0.05f), new Vector3(0.22f, 0.18f, 0.22f), s.Skin, 7, 4, f.TorsoRotation);
            k.Ball(f.Torso(0.3f, 0.66f, -0.05f), new Vector3(0.22f, 0.18f, 0.22f), s.Skin, 7, 4, f.TorsoRotation);
            k.Box(f.Pelvis + new Vector3(0f, -0.08f, 0.1f), new Vector3(0.5f, 0.26f, 0.3f), s.Accent, 0.9f);
        }

        static void Ghost(MeshKit k)
        {
            var s = FigureSpec.Husk;
            // A drifting shroud: the robe is body; the sleeves are arms; no legs.
            k.Body();
            k.Lathe(new Vector3(0f, 0.08f, 0f), new[] { new Vector2(0f, 0.42f), new Vector2(0.25f, 0.34f), new Vector2(0.7f, 0.28f), new Vector2(1.15f, 0.26f), new Vector2(1.3f, 0.18f) }, 10, Pale, 0.85f);
            // Tattered hem.
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI * 2f / 8f;
                var root = new Vector3(Mathf.Cos(a) * 0.38f, 0.15f, Mathf.Sin(a) * 0.32f);
                k.Limb(root, root + new Vector3(Mathf.Cos(a) * 0.08f, -0.14f, Mathf.Sin(a) * 0.06f), 0.06f, 0f, Bone, 3);
            }
            var shoulderL = new Vector3(-0.24f, 1.18f, 0f);
            var shoulderR = new Vector3(0.24f, 1.18f, 0f);
            k.Part(BodyPart.LeftArm, shoulderL);
            k.Limb(shoulderL, shoulderL + new Vector3(-0.1f, -0.35f, 0.35f), 0.08f, 0.12f, Pale, 6);
            k.Limb(shoulderL + new Vector3(-0.1f, -0.35f, 0.35f), shoulderL + new Vector3(-0.08f, -0.45f, 0.55f), 0.035f, 0f, Bone, 4);
            k.Part(BodyPart.RightArm, shoulderR);
            k.Limb(shoulderR, shoulderR + new Vector3(0.1f, -0.35f, 0.35f), 0.08f, 0.12f, Pale, 6);
            k.Limb(shoulderR + new Vector3(0.1f, -0.35f, 0.35f), shoulderR + new Vector3(0.08f, -0.45f, 0.55f), 0.035f, 0f, Bone, 4);
            // Hood with a dark hollow and two pale eyes.
            var head = new Vector3(0f, 1.45f, 0.04f);
            k.Part(BodyPart.Head, new Vector3(0f, 1.3f, 0f));
            k.Ball(head, new Vector3(0.2f, 0.22f, 0.21f), Pale, 8, 5);
            k.Ball(head + new Vector3(0f, -0.02f, 0.12f), new Vector3(0.13f, 0.14f, 0.1f), Black, 7, 4);
            var f = new Figure(k, s);
            f.Eyes(head + new Vector3(0f, 0f, 0.2f), 0.05f, 0.025f, GlowEye);
            k.Body();
        }

        static void Ashling(MeshKit k)
        {
            var s = FigureSpec.Husk;
            s.LegLength = 0.42f; s.LegThickness = 0.06f; s.HipWidth = 0.1f; s.TorsoLength = 0.35f; s.TorsoWidth = 0.3f; s.TorsoDepth = 0.28f;
            s.Hunch = 30f; s.ShoulderWidth = 0.22f; s.ArmLength = 0.4f; s.ArmThickness = 0.045f; s.HandSize = 0.06f; s.HeadSize = 0.17f; s.NeckLength = 0.02f;
            var f = new Figure(k, s).Build();
            f.Eyes(f.Head + new Vector3(0f, 0.02f, 0.15f), 0.07f, 0.05f, GlowEye);
            f.Spines(5, 0.16f, Black);
            k.Part(BodyPart.Head, f.NeckBase);
            for (int i = -1; i <= 1; i++)
                k.Limb(f.Head + new Vector3(i * 0.08f, 0.12f, -0.04f), f.Head + new Vector3(i * 0.14f, 0.32f, -0.16f), 0.035f, 0f, Black, 4);
            k.Body();
        }

        static void Cinder(MeshKit k)
        {
            var f = Husk(k);
            f.Horns(0.22f, 0.04f, Black);
            // Charred plates on chest and forearms.
            k.Block(f.Torso(0f, f.Spec.TorsoLength * 0.62f, f.Spec.TorsoDepth * 0.85f), new Vector3(0.3f, 0.25f, 0.06f), Black, 0.4f, f.TorsoRotation);
        }

        static void Hulk(MeshKit k)
        {
            var s = FigureSpec.Husk;
            s.LegLength = 0.55f; s.LegThickness = 0.15f; s.HipWidth = 0.22f; s.TorsoLength = 0.8f; s.TorsoWidth = 0.52f; s.TorsoDepth = 0.4f;
            s.Hunch = 34f; s.ShoulderWidth = 0.5f; s.ArmLength = 0.95f; s.ArmThickness = 0.15f; s.HandSize = 0.2f; s.HeadSize = 0.14f; s.NeckLength = 0f;
            var f = new Figure(k, s).Build();
            f.Horns(0.3f, 0.06f, Black, 0.8f);
            f.Spines(5, 0.22f, Black);
            k.Ball(f.Torso(0f, 0.75f, -0.15f), new Vector3(0.45f, 0.22f, 0.3f), s.Dark, 8, 4, f.TorsoRotation);
        }

        static void Drowned(MeshKit k)
        {
            var s = FigureSpec.Husk;
            s.Hunch = 28f; s.TorsoWidth = 0.32f; s.TorsoDepth = 0.26f; s.HeadSize = 0.16f;
            var f = new Figure(k, s).Build();
            // Barnacles and hanging weed.
            var rnd = new System.Random(4);
            for (int i = 0; i < 7; i++)
            {
                var at = f.Torso(((float)rnd.NextDouble() - 0.5f) * 0.5f, (float)rnd.NextDouble() * 0.6f, ((float)rnd.NextDouble() > 0.5f ? 1f : -1f) * 0.2f);
                k.Ball(at, Vector3.one * 0.04f, Bone, 4, 2);
            }
            for (int i = 0; i < 5; i++)
            {
                var root = f.Torso(-0.16f + i * 0.08f, 0.62f, -0.18f);
                k.Limb(root, root + new Vector3(0.02f, -0.4f, -0.06f), 0.025f, 0.008f, Black, 3);
            }
            k.Part(BodyPart.Head, f.NeckBase);
            for (int i = 0; i < 4; i++)
            {
                var root = f.Head + new Vector3(-0.09f + i * 0.06f, -0.02f, -0.12f);
                k.Limb(root, root + new Vector3(0f, -0.28f, -0.05f), 0.02f, 0.006f, Black, 3);
            }
            k.Body();
        }

        static void BrineRunner(MeshKit k)
        {
            // Crab-like: a low shell on four legs (diagonal pairs move together) and two claws.
            var shell = new Vector3(0f, 0.62f, 0f);
            k.Body();
            k.Ball(shell, new Vector3(0.42f, 0.2f, 0.36f), Pale, 9, 4);
            k.Ball(shell + Vector3.up * 0.1f, new Vector3(0.3f, 0.12f, 0.25f), Bone, 8, 3);
            var f = new Figure(k, FigureSpec.Husk);
            f.Eyes(shell + new Vector3(0f, 0.16f, 0.34f), 0.08f, 0.04f, Black);
            void LegPair(BodyPart part, float side, float z, float zTip)
            {
                var hip = shell + new Vector3(side * 0.3f, -0.05f, z);
                k.Part(part, hip);
                var knee = hip + new Vector3(side * 0.25f, 0.1f, (zTip - z) * 0.5f);
                var foot = new Vector3(hip.x + side * 0.38f, 0f, zTip);
                k.Limb(hip, knee, 0.05f, 0.04f, Wood, 5);
                k.Limb(knee, foot, 0.04f, 0f, Wood, 5);
            }
            LegPair(BodyPart.LeftLeg, -1f, 0.12f, 0.25f);
            LegPair(BodyPart.LeftLeg, 1f, -0.12f, -0.25f);
            LegPair(BodyPart.RightLeg, 1f, 0.12f, 0.25f);
            LegPair(BodyPart.RightLeg, -1f, -0.12f, -0.25f);
            for (int side = -1; side <= 1; side += 2)
            {
                var root = shell + new Vector3(side * 0.28f, 0f, 0.28f);
                k.Part(side < 0 ? BodyPart.LeftArm : BodyPart.RightArm, root);
                var elbow = root + new Vector3(side * 0.12f, 0.1f, 0.22f);
                k.Limb(root, elbow, 0.05f, 0.045f, Wood, 5);
                k.Ball(elbow + new Vector3(0f, 0.02f, 0.14f), new Vector3(0.08f, 0.06f, 0.14f), Pale, 6, 3);
                k.Limb(elbow + new Vector3(side * 0.03f, 0.04f, 0.2f), elbow + new Vector3(side * 0.03f, 0.04f, 0.34f), 0.035f, 0f, Bone, 4);
            }
            k.Body();
        }

        static void Shell(Figure f, Color color)
        {
            var k = f.Kit;
            k.Body();
            var c = f.Torso(0f, f.Spec.TorsoLength * 0.55f, -f.Spec.TorsoDepth * 1.1f);
            k.Ball(c, new Vector3(f.Spec.TorsoWidth * 1.15f, f.Spec.TorsoLength * 0.6f, f.Spec.TorsoDepth * 0.9f), color, 10, 5, f.TorsoRotation, 0.06f, 3);
            for (int i = -1; i <= 1; i++)
                k.Limb(f.Torso(i * 0.18f, f.Spec.TorsoLength * 0.1f, -f.Spec.TorsoDepth * 1.6f), f.Torso(i * 0.16f, f.Spec.TorsoLength * 1.05f, -f.Spec.TorsoDepth * 1.6f), 0.04f, 0.03f, Black, 4);
        }

        static void ShellBrute(MeshKit k)
        {
            var s = FigureSpec.Husk;
            s.LegLength = 0.6f; s.LegThickness = 0.14f; s.HipWidth = 0.2f; s.TorsoLength = 0.72f; s.TorsoWidth = 0.46f; s.TorsoDepth = 0.32f;
            s.Hunch = 30f; s.ShoulderWidth = 0.44f; s.ArmLength = 0.75f; s.ArmThickness = 0.12f; s.HandSize = 0.17f; s.HeadSize = 0.13f;
            var f = new Figure(k, s).Build();
            Shell(f, Bone);
        }

        static void Champion(MeshKit k)
        {
            var s = FigureSpec.Husk;
            s.Hunch = 10f; s.TorsoWidth = 0.34f; s.TorsoDepth = 0.24f; s.ShoulderWidth = 0.3f; s.ArmThickness = 0.07f;
            var f = new Figure(k, s).Build();
            f.Pauldrons(0.22f, Metal);
            // Helm with a crest, a breastplate and a long blade.
            k.Part(BodyPart.Head, f.NeckBase);
            k.Ball(f.Head + Vector3.up * 0.03f, new Vector3(0.16f, 0.15f, 0.17f), Metal, 8, 4);
            k.Block(f.Head + new Vector3(0f, 0.17f, -0.02f), new Vector3(0.04f, 0.14f, 0.34f), Black, 0.3f);
            k.Body();
            k.Block(f.Torso(0f, s.TorsoLength * 0.6f, s.TorsoDepth * 0.7f), new Vector3(0.42f, 0.38f, 0.1f), Metal, 0.35f, f.TorsoRotation);
            f.Weapon("blade", 0.9f, Metal, Wood);
        }

        static void AshChampion(MeshKit k)
        {
            var s = FigureSpec.Husk;
            s.LegLength = 0.58f; s.LegThickness = 0.15f; s.HipWidth = 0.22f; s.TorsoLength = 0.78f; s.TorsoWidth = 0.5f; s.TorsoDepth = 0.36f;
            s.Hunch = 24f; s.ShoulderWidth = 0.48f; s.ArmLength = 0.9f; s.ArmThickness = 0.14f; s.HandSize = 0.18f; s.HeadSize = 0.15f;
            var f = new Figure(k, s).Build();
            f.Horns(0.38f, 0.07f, Black, 0.8f);
            f.Pauldrons(0.3f, Black);
            k.Ball(f.Torso(0f, s.TorsoLength * 0.62f, s.TorsoDepth * 0.95f), new Vector3(0.09f, 0.11f, 0.06f), GlowEye, 6, 3);
            f.Weapon("axe", 1f, Black, Wood);
        }

        static void TideChampion(MeshKit k)
        {
            var s = FigureSpec.Husk;
            s.Hunch = 20f; s.TorsoWidth = 0.36f; s.TorsoDepth = 0.28f; s.ShoulderWidth = 0.32f; s.ArmThickness = 0.08f; s.HeadSize = 0.16f;
            var f = new Figure(k, s).Build();
            Shell(f, Bone);
            f.Pauldrons(0.22f, Bone);
            k.Part(BodyPart.Head, f.NeckBase);
            for (int i = 0; i < 5; i++)
            {
                float a = Mathf.Lerp(-0.9f, 0.9f, i / 4f);
                k.Limb(f.Head + new Vector3(Mathf.Sin(a) * 0.12f, 0.12f, 0f), f.Head + new Vector3(Mathf.Sin(a) * 0.2f, 0.34f, -0.05f), 0.03f, 0f, Bone, 4);
            }
            k.Body();
            f.Weapon("trident", 1.2f, Bone, Wood);
        }
    }
}
