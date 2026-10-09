using UnityEngine;

namespace OldGods.Runtime
{
    /// <summary>
    /// Proportions for a two-legged figure. Every number is in metres for a figure facing +z
    /// with its feet at y = 0; the figure is scaled per enemy type afterwards.
    /// </summary>
    public struct FigureSpec
    {
        public float LegLength;      // hip height
        public float LegThickness;
        public float HipWidth;       // half distance between the hips
        public float TorsoLength;    // pelvis to the base of the neck
        public float TorsoWidth;
        public float TorsoDepth;
        public float Hunch;          // forward lean of the torso, degrees
        public float ShoulderWidth;  // half distance between the shoulders
        public float ArmLength;
        public float ArmThickness;
        public float HandSize;
        public float HeadSize;
        public float NeckLength;
        public Color Skin, Dark, Accent, Eye;
        public int Sides;            // facets around limbs

        public static FigureSpec Husk => new FigureSpec
        {
            LegLength = 0.72f, LegThickness = 0.085f, HipWidth = 0.13f,
            TorsoLength = 0.62f, TorsoWidth = 0.3f, TorsoDepth = 0.2f, Hunch = 22f,
            ShoulderWidth = 0.27f, ArmLength = 0.72f, ArmThickness = 0.06f, HandSize = 0.08f,
            HeadSize = 0.15f, NeckLength = 0.08f,
            Skin = new Color(0.86f, 0.84f, 0.82f), Dark = new Color(0.55f, 0.53f, 0.52f),
            Accent = new Color(0.35f, 0.33f, 0.32f), Eye = new Color(0.15f, 0.15f, 0.15f), Sides = 6,
        };
    }

    /// <summary>
    /// Builds a jointed figure into a MeshKit and remembers where things ended up (shoulders,
    /// hands, head, back) so callers can hang horns, weapons, shells and plates on it.
    /// Legs and arms are tagged with their body part so the shaders can walk them.
    /// </summary>
    public sealed class Figure
    {
        public readonly MeshKit Kit;
        public readonly FigureSpec Spec;
        public Vector3 Pelvis, Chest, NeckBase, Head, LeftShoulder, RightShoulder, LeftHand, RightHand, Back;
        public Quaternion TorsoRotation;

        public Figure(MeshKit kit, FigureSpec spec)
        {
            Kit = kit;
            Spec = spec;
        }

        /// <summary>A point in the torso's frame (origin at the pelvis, y up the spine), into model space.</summary>
        public Vector3 Torso(float x, float y, float z) => Pelvis + TorsoRotation * new Vector3(x, y, z);

        public Figure Build(bool legs = true, bool arms = true, bool head = true)
        {
            var s = Spec;
            int sides = Mathf.Max(5, s.Sides);
            Pelvis = new Vector3(0f, s.LegLength, 0f);
            TorsoRotation = Quaternion.Euler(s.Hunch, 0f, 0f);
            Chest = Torso(0f, s.TorsoLength * 0.62f, 0f);
            NeckBase = Torso(0f, s.TorsoLength, 0f);
            Back = Torso(0f, s.TorsoLength * 0.6f, -s.TorsoDepth * 0.9f);
            LeftShoulder = Torso(-s.ShoulderWidth, s.TorsoLength * 0.9f, 0f);
            RightShoulder = Torso(s.ShoulderWidth, s.TorsoLength * 0.9f, 0f);

            if (legs)
            {
                Leg(BodyPart.LeftLeg, -1f, sides);
                Leg(BodyPart.RightLeg, 1f, sides);
            }

            Kit.Body();
            // Pelvis, belly and ribcage as overlapping faceted balls along the spine.
            Kit.Ball(Pelvis + Vector3.up * 0.02f, new Vector3(s.TorsoWidth * 0.75f, s.TorsoLength * 0.2f, s.TorsoDepth * 0.85f), s.Dark, 7, 4, TorsoRotation);
            Kit.Ball(Torso(0f, s.TorsoLength * 0.32f, 0.01f), new Vector3(s.TorsoWidth * 0.7f, s.TorsoLength * 0.26f, s.TorsoDepth * 0.85f), s.Skin, 8, 4, TorsoRotation);
            Kit.Ball(Chest, new Vector3(s.TorsoWidth, s.TorsoLength * 0.34f, s.TorsoDepth), s.Skin, 9, 5, TorsoRotation);
            // Shoulder caps.
            Kit.Ball(LeftShoulder, Vector3.one * s.ArmThickness * 1.7f, s.Skin, 6, 3);
            Kit.Ball(RightShoulder, Vector3.one * s.ArmThickness * 1.7f, s.Skin, 6, 3);

            if (arms)
            {
                LeftHand = Arm(BodyPart.LeftArm, LeftShoulder, -1f, sides);
                RightHand = Arm(BodyPart.RightArm, RightShoulder, 1f, sides);
            }

            if (head)
            {
                Kit.Part(BodyPart.Head, NeckBase);
                Head = NeckBase + TorsoRotation * Vector3.up * (s.NeckLength + s.HeadSize * 0.8f) + Vector3.forward * s.HeadSize * 0.25f;
                Kit.Limb(NeckBase, Head - Vector3.up * s.HeadSize * 0.4f, s.ArmThickness * 1.1f, s.ArmThickness, s.Dark, 5);
                Kit.Ball(Head, new Vector3(s.HeadSize * 0.85f, s.HeadSize, s.HeadSize * 0.95f), s.Skin, 8, 5);
                // Jaw and brow give the head a face and a direction.
                Kit.Box(Head + new Vector3(0f, -s.HeadSize * 0.55f, s.HeadSize * 0.35f), new Vector3(s.HeadSize * 1.1f, s.HeadSize * 0.4f, s.HeadSize * 0.8f), s.Dark, 0.8f);
                Kit.Box(Head + new Vector3(0f, s.HeadSize * 0.2f, s.HeadSize * 0.72f), new Vector3(s.HeadSize * 1.4f, s.HeadSize * 0.18f, s.HeadSize * 0.3f), s.Dark);
                Eyes(Head + new Vector3(0f, 0f, s.HeadSize * 0.8f), s.HeadSize * 0.32f, s.HeadSize * 0.16f, s.Eye);
                Kit.Body();
            }
            return this;
        }

        public void Eyes(Vector3 centre, float spacing, float size, Color color)
        {
            Kit.Ball(centre + Vector3.left * spacing, new Vector3(size, size * 0.8f, size * 0.5f), color, 5, 3);
            Kit.Ball(centre + Vector3.right * spacing, new Vector3(size, size * 0.8f, size * 0.5f), color, 5, 3);
        }

        void Leg(BodyPart part, float side, int sides)
        {
            var s = Spec;
            var hip = new Vector3(side * s.HipWidth, s.LegLength, 0f);
            Kit.Part(part, hip);
            var knee = new Vector3(side * s.HipWidth * 1.1f, s.LegLength * 0.5f, s.LegLength * 0.08f);
            var ankle = new Vector3(side * s.HipWidth * 1.05f, s.LegThickness * 0.9f, -s.LegLength * 0.02f);
            Kit.Limb(hip, knee, s.LegThickness * 1.25f, s.LegThickness, s.Dark, sides);
            Kit.Ball(knee, Vector3.one * s.LegThickness * 1.1f, s.Dark, 5, 3);
            Kit.Limb(knee, ankle, s.LegThickness, s.LegThickness * 0.7f, s.Dark, sides);
            // Foot: a wedge pointing forward.
            Kit.Box(new Vector3(ankle.x, s.LegThickness * 0.45f, ankle.z + s.LegThickness * 0.9f),
                new Vector3(s.LegThickness * 1.6f, s.LegThickness * 0.9f, s.LegThickness * 3f), s.Accent, 0.7f);
            Kit.Body();
        }

        Vector3 Arm(BodyPart part, Vector3 shoulder, float side, int sides)
        {
            var s = Spec;
            Kit.Part(part, shoulder);
            // Arms hang forward with the hunch and bend at the elbow.
            var elbow = shoulder + new Vector3(side * s.ArmThickness * 0.8f, -s.ArmLength * 0.5f, s.ArmLength * 0.08f);
            var wrist = elbow + new Vector3(side * s.ArmThickness * 0.2f, -s.ArmLength * 0.42f, s.ArmLength * 0.22f);
            Kit.Limb(shoulder, elbow, s.ArmThickness * 1.15f, s.ArmThickness * 0.9f, s.Skin, sides);
            Kit.Ball(elbow, Vector3.one * s.ArmThickness, s.Skin, 5, 3);
            Kit.Limb(elbow, wrist, s.ArmThickness * 0.9f, s.ArmThickness * 0.65f, s.Dark, sides);
            var hand = wrist + new Vector3(0f, -s.HandSize * 0.6f, s.HandSize * 0.3f);
            Kit.Ball(hand, new Vector3(s.HandSize * 0.8f, s.HandSize, s.HandSize * 0.7f), s.Dark, 6, 3);
            // Three claws.
            for (int f = -1; f <= 1; f++)
                Kit.Limb(hand + new Vector3(f * s.HandSize * 0.45f, -s.HandSize * 0.5f, s.HandSize * 0.2f),
                    hand + new Vector3(f * s.HandSize * 0.55f, -s.HandSize * 1.5f, s.HandSize * 0.55f), s.HandSize * 0.22f, 0f, s.Accent, 4);
            Kit.Body();
            return hand;
        }

        /// <summary>Two horns curving up and back from the head.</summary>
        public void Horns(float length, float thickness, Color color, float spread = 0.6f)
        {
            Kit.Part(BodyPart.Head, NeckBase);
            for (int side = -1; side <= 1; side += 2)
            {
                var root = Head + new Vector3(side * Spec.HeadSize * spread, Spec.HeadSize * 0.6f, 0f);
                var mid = root + new Vector3(side * length * 0.35f, length * 0.55f, -length * 0.15f);
                var tip = mid + new Vector3(side * length * 0.1f, length * 0.35f, -length * 0.35f);
                Kit.Limb(root, mid, thickness, thickness * 0.6f, color, 5);
                Kit.Limb(mid, tip, thickness * 0.6f, 0f, color, 5);
            }
            Kit.Body();
        }

        /// <summary>A row of spikes along the back.</summary>
        public void Spines(int count, float length, Color color)
        {
            Kit.Body();
            for (int i = 0; i < count; i++)
            {
                float t = (i + 0.5f) / count;
                var root = Torso(0f, Spec.TorsoLength * Mathf.Lerp(0.15f, 0.95f, t), -Spec.TorsoDepth * 0.85f);
                var tip = root + TorsoRotation * new Vector3(0f, length * 0.4f, -length);
                Kit.Limb(root, tip, length * 0.22f, 0f, color, 4);
            }
        }

        /// <summary>Armour plates on both shoulders.</summary>
        public void Pauldrons(float size, Color color)
        {
            Kit.Body();
            Kit.Block(LeftShoulder + new Vector3(-size * 0.15f, size * 0.25f, 0f), new Vector3(size * 1.3f, size * 0.7f, size * 1.2f), color, 0.35f, Quaternion.Euler(0f, 0f, 20f));
            Kit.Block(RightShoulder + new Vector3(size * 0.15f, size * 0.25f, 0f), new Vector3(size * 1.3f, size * 0.7f, size * 1.2f), color, 0.35f, Quaternion.Euler(0f, 0f, -20f));
        }

        /// <summary>A weapon held in the right hand: a blade, axe or trident, pointing forward and down.</summary>
        public void Weapon(string kind, float length, Color metal, Color grip)
        {
            Kit.Part(BodyPart.RightArm, RightShoulder);
            var h = RightHand;
            var tip = h + new Vector3(0.05f, length * 0.35f, length);
            Kit.Limb(h - (tip - h).normalized * 0.12f, h + (tip - h).normalized * 0.1f, 0.035f, 0.035f, grip, 5);
            switch (kind)
            {
                case "axe":
                    Kit.Limb(h, tip, 0.035f, 0.03f, grip, 5);
                    Kit.Block(tip - (tip - h).normalized * 0.12f + Vector3.right * 0.12f, new Vector3(0.3f, 0.35f, 0.06f), metal, 0.3f,
                        Quaternion.LookRotation(tip - h) * Quaternion.Euler(90f, 0f, 0f));
                    break;
                case "trident":
                    Kit.Limb(h, tip, 0.03f, 0.03f, grip, 5);
                    var dir = (tip - h).normalized;
                    for (int i = -1; i <= 1; i++)
                        Kit.Limb(tip + Vector3.right * i * 0.08f, tip + dir * 0.3f + Vector3.right * i * 0.1f, 0.025f, 0f, metal, 4);
                    Kit.Limb(tip + Vector3.left * 0.1f, tip + Vector3.right * 0.1f, 0.025f, 0.025f, metal, 4);
                    break;
                default:
                    Kit.Limb(h + (tip - h).normalized * 0.1f, tip, 0.06f, 0f, metal, 4);
                    Kit.Limb(h + Vector3.left * 0.14f, h + Vector3.right * 0.14f, 0.03f, 0.03f, grip, 4);
                    break;
            }
            Kit.Body();
        }
    }
}
