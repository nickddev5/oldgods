using UnityEngine;

namespace OldGods.Runtime
{
    /// <summary>Colours for one character. Built-in models carry their colours in the mesh.</summary>
    public struct Palette
    {
        public Color Skin, SkinShade, Cloth, Cloth2, Leather, Metal, Trim, Hair, Eye, Pupil, Glow;

        public static Palette Neutral => new Palette
        {
            Skin = new Color(0.86f, 0.72f, 0.6f), SkinShade = new Color(0.7f, 0.56f, 0.46f),
            Cloth = new Color(0.4f, 0.45f, 0.55f), Cloth2 = new Color(0.3f, 0.3f, 0.32f),
            Leather = new Color(0.45f, 0.3f, 0.2f), Metal = new Color(0.72f, 0.72f, 0.75f), Trim = new Color(0.95f, 0.78f, 0.35f),
            Hair = new Color(0.25f, 0.2f, 0.17f), Eye = Color.white, Pupil = new Color(0.12f, 0.1f, 0.1f), Glow = new Color(1f, 0.9f, 0.6f),
        };
    }

    /// <summary>
    /// Proportions for a two-legged figure, in metres, facing +z, feet at y = 0. Stylised: a large
    /// head, broad hands and chunky feet so silhouettes read from the chase camera.
    /// </summary>
    public struct FigureSpec
    {
        public float LegLength;      // hip height
        public float LegThickness;
        public float HipWidth;       // half distance between the hips
        public float TorsoLength;    // pelvis to the base of the neck
        public float TorsoWidth;     // half width of the chest
        public float TorsoDepth;     // half depth of the chest
        public float Hunch;          // forward lean of the torso, degrees
        public float ShoulderWidth;  // half distance between the shoulders
        public float ArmLength;
        public float ArmThickness;
        public float HandSize;
        public float HeadSize;       // head radius
        public float NeckLength;
        public bool Claws;           // claws instead of fingers
        public int Sides;            // segments around limbs

        public static FigureSpec Human => new FigureSpec
        {
            LegLength = 0.86f, LegThickness = 0.075f, HipWidth = 0.1f,
            TorsoLength = 0.5f, TorsoWidth = 0.2f, TorsoDepth = 0.13f, Hunch = 4f,
            ShoulderWidth = 0.22f, ArmLength = 0.56f, ArmThickness = 0.05f, HandSize = 0.06f,
            HeadSize = 0.135f, NeckLength = 0.05f, Sides = 8,
        };

        public static FigureSpec Husk => new FigureSpec
        {
            LegLength = 0.74f, LegThickness = 0.07f, HipWidth = 0.11f,
            TorsoLength = 0.55f, TorsoWidth = 0.2f, TorsoDepth = 0.14f, Hunch = 24f,
            ShoulderWidth = 0.23f, ArmLength = 0.7f, ArmThickness = 0.05f, HandSize = 0.07f,
            HeadSize = 0.13f, NeckLength = 0.06f, Claws = true, Sides = 7,
        };
    }

    /// <summary>
    /// A jointed figure built into a MeshKit, then dressed in layers: boots, trousers, tunic,
    /// belt and pouches, sleeves, gloves, mantle, cape, hats and weapons. Legs and arms (and
    /// whatever is worn on them) are tagged so the shaders can walk them.
    /// </summary>
    public sealed class Figure
    {
        public readonly MeshKit Kit;
        public readonly FigureSpec Spec;
        public readonly Palette P;
        public Vector3 Pelvis, Chest, NeckBase, Head;
        public Vector3 HipL, HipR, KneeL, KneeR, AnkleL, AnkleR;
        public Vector3 ShoulderL, ShoulderR, ElbowL, ElbowR, WristL, WristR, HandL, HandR;
        public Quaternion TorsoRotation;

        public Figure(MeshKit kit, FigureSpec spec, Palette palette)
        {
            Kit = kit;
            Spec = spec;
            P = palette;
        }

        public Vector3 Torso(float x, float y, float z) => Pelvis + TorsoRotation * new Vector3(x, y, z);

        int Sides => Mathf.Max(6, Spec.Sides);

        /// <summary>Lays out the joints. Call before Body or any clothing.</summary>
        public Figure Layout()
        {
            var s = Spec;
            Pelvis = new Vector3(0f, s.LegLength, 0f);
            TorsoRotation = Quaternion.Euler(s.Hunch, 0f, 0f);
            Chest = Torso(0f, s.TorsoLength * 0.68f, 0f);
            NeckBase = Torso(0f, s.TorsoLength, 0f);
            Head = NeckBase + TorsoRotation * Vector3.up * (s.NeckLength + s.HeadSize * 0.85f) + Vector3.forward * s.HeadSize * 0.15f;
            HipL = new Vector3(-s.HipWidth, s.LegLength, 0f);
            HipR = new Vector3(s.HipWidth, s.LegLength, 0f);
            KneeL = new Vector3(-s.HipWidth * 1.05f, s.LegLength * 0.53f, s.LegLength * 0.05f);
            KneeR = new Vector3(s.HipWidth * 1.05f, s.LegLength * 0.53f, s.LegLength * 0.05f);
            AnkleL = new Vector3(-s.HipWidth * 1.05f, s.LegThickness * 1.3f, 0f);
            AnkleR = new Vector3(s.HipWidth * 1.05f, s.LegThickness * 1.3f, 0f);
            ShoulderL = Torso(-s.ShoulderWidth, s.TorsoLength * 0.9f, 0f);
            ShoulderR = Torso(s.ShoulderWidth, s.TorsoLength * 0.9f, 0f);
            ElbowL = ShoulderL + new Vector3(-s.ArmThickness * 0.6f, -s.ArmLength * 0.5f, -0.01f);
            ElbowR = ShoulderR + new Vector3(s.ArmThickness * 0.6f, -s.ArmLength * 0.5f, -0.01f);
            WristL = ElbowL + new Vector3(-s.ArmThickness * 0.2f, -s.ArmLength * 0.42f, s.ArmLength * 0.14f);
            WristR = ElbowR + new Vector3(s.ArmThickness * 0.2f, -s.ArmLength * 0.42f, s.ArmLength * 0.14f);
            HandL = WristL + new Vector3(0f, -s.HandSize * 0.9f, s.HandSize * 0.2f);
            HandR = WristR + new Vector3(0f, -s.HandSize * 0.9f, s.HandSize * 0.2f);
            return this;
        }

        /// <summary>Starts a leg part: it swings at the hip and bends at the knee.</summary>
        public void LegPart(bool left) => Kit.Part(left ? BodyPart.LeftLeg : BodyPart.RightLeg, left ? HipL : HipR, (left ? KneeL : KneeR).y);
        /// <summary>Starts an arm part: it swings at the shoulder and bends at the elbow.</summary>
        public void ArmPart(bool left) => Kit.Part(left ? BodyPart.LeftArm : BodyPart.RightArm, left ? ShoulderL : ShoulderR, (left ? ElbowL : ElbowR).y);
        /// <summary>Starts something held in a hand: all of it turns with the forearm.</summary>
        public void HeldPart(bool left) => Kit.Part(left ? BodyPart.LeftArm : BodyPart.RightArm, left ? ShoulderL : ShoulderR, -(left ? ElbowL : ElbowR).y);
        void HeadPart() => Kit.Part(BodyPart.Head, NeckBase);

        // ---- The body ----

        /// <summary>Skin: legs, torso, arms, hands, neck and head with a face.</summary>
        public Figure Body(Color? legColor = null, Color? torsoColor = null, Color? armColor = null)
        {
            var s = Spec;
            int n = Sides;
            Color leg = legColor ?? P.Skin, torso = torsoColor ?? P.Skin, arm = armColor ?? P.Skin;
            for (int side = 0; side < 2; side++)
            {
                bool left = side == 0;
                LegPart(left);
                Vector3 hip = left ? HipL : HipR, knee = left ? KneeL : KneeR, ankle = left ? AnkleL : AnkleR;
                Kit.Limb(hip + Vector3.up * 0.03f, knee, s.LegThickness * 1.35f, s.LegThickness * 0.95f, leg, n);
                Kit.Ball(knee, Vector3.one * s.LegThickness * 0.98f, leg, n, 4);
                Kit.Limb(knee, ankle, s.LegThickness * 0.95f, s.LegThickness * 0.7f, leg, n);
                Foot(ankle, P.SkinShade, 1f);
            }
            Kit.Body();
            Kit.Ball(Pelvis + Vector3.up * 0.02f, new Vector3(s.TorsoWidth * 0.95f, s.TorsoLength * 0.2f, s.TorsoDepth * 1.05f), torso, 12, 6, TorsoRotation);
            Kit.Ball(Torso(0f, s.TorsoLength * 0.36f, 0f), new Vector3(s.TorsoWidth * 0.85f, s.TorsoLength * 0.26f, s.TorsoDepth * 0.95f), torso, 12, 6, TorsoRotation);
            Kit.Ball(Chest, new Vector3(s.TorsoWidth * 1.08f, s.TorsoLength * 0.34f, s.TorsoDepth * 1.1f), torso, 12, 7, TorsoRotation);
            // A flat yoke across the top of the chest so the shoulder line reads square.
            Kit.Block(Torso(0f, s.TorsoLength * 0.9f, 0f), new Vector3(s.TorsoWidth * 1.9f, s.TorsoLength * 0.2f, s.TorsoDepth * 1.7f), torso, 0.4f, TorsoRotation);
            for (int side = 0; side < 2; side++)
            {
                bool left = side == 0;
                ArmPart(left);
                Vector3 sh = left ? ShoulderL : ShoulderR, el = left ? ElbowL : ElbowR, wr = left ? WristL : WristR, hand = left ? HandL : HandR;
                Deltoid(left, s.ArmThickness * 1.55f, torso);
                Kit.Limb(sh, el, s.ArmThickness * 1.2f, s.ArmThickness * 0.95f, arm, n);
                Kit.Ball(el, Vector3.one * s.ArmThickness * 0.95f, arm, n, 4);
                Kit.Limb(el, wr, s.ArmThickness * 0.95f, s.ArmThickness * 0.75f, arm, n);
                Hand(hand, left, P.Skin);
            }
            HeadPart();
            Kit.Limb(NeckBase - TorsoRotation * Vector3.up * 0.03f, Head - Vector3.up * s.HeadSize * 0.55f, s.ArmThickness * 1.3f, s.ArmThickness * 1.15f, P.Skin, n);
            Skull(P.Skin);
            Kit.Body();
            return this;
        }

        /// <summary>
        /// The head: an eight-sided faceted skull, widest at the cheekbones, with a flat crown and a
        /// squared jaw that narrows to the chin. A flat plane faces forward for the face to sit on.
        /// </summary>
        public void Skull(Color color, float jaw = 1f)
        {
            float h = Spec.HeadSize;
            bool smooth = Kit.Smooth;
            Kit.Smooth = false;
            Kit.Lathe(Head + new Vector3(0f, -h, h * 0.04f), new[]
            {
                new Vector2(0f, h * 0.38f * jaw), new Vector2(h * 0.4f, h * 0.72f * jaw), new Vector2(h * 0.92f, h * 0.93f),
                new Vector2(h * 1.45f, h * 0.9f), new Vector2(h * 1.8f, h * 0.68f), new Vector2(h * 1.95f, h * 0.36f),
            }, 8, color, 1f, Facing);
            Kit.Smooth = smooth;
        }

        // Turns an eight-sided lathe or ball so a flat face points forward instead of an edge.
        static readonly Quaternion Facing = Quaternion.Euler(0f, 22.5f, 0f);

        /// <summary>A squared shoulder cap: a chamfered block sloping down and out from the collar.</summary>
        public void Deltoid(bool left, float radius, Color color)
        {
            var sh = left ? ShoulderL : ShoulderR;
            float sx = left ? -1f : 1f;
            Kit.Block(sh + TorsoRotation * new Vector3(sx * radius * 0.12f, -radius * 0.32f, 0f), new Vector3(radius * 1.85f, radius * 1.4f, radius * 1.85f), color, 0.5f,
                TorsoRotation * Quaternion.Euler(0f, 0f, sx * -16f));
        }

        public void Foot(Vector3 ankle, Color color, float size)
        {
            float t = Spec.LegThickness * size;
            Kit.Ball(new Vector3(ankle.x, t * 0.55f, ankle.z + t * 0.9f), new Vector3(t * 0.95f, t * 0.6f, t * 1.7f), color, 9, 5);
        }

        public void Hand(Vector3 hand, bool left, Color color)
        {
            var s = Spec;
            float h = s.HandSize;
            Kit.Ball(hand, new Vector3(h * 0.75f, h * 0.9f, h * 0.5f), color, 9, 5);
            int fingers = s.Claws ? 3 : 4;
            for (int f = 0; f < fingers; f++)
            {
                float x = Mathf.Lerp(-0.45f, 0.45f, fingers == 1 ? 0.5f : f / (float)(fingers - 1)) * h;
                var root = hand + new Vector3(x, -h * 0.7f, h * 0.1f);
                var tip = root + new Vector3(x * 0.15f, -h * (s.Claws ? 0.9f : 0.75f), h * (s.Claws ? 0.45f : 0.25f));
                Kit.Limb(root, tip, h * 0.2f, s.Claws ? 0f : h * 0.16f, s.Claws ? P.Pupil : color, 5);
            }
            if (!s.Claws)
            {
                float sx = left ? 1f : -1f;
                Kit.Limb(hand + new Vector3(sx * h * 0.55f, -h * 0.1f, h * 0.2f), hand + new Vector3(sx * h * 0.75f, -h * 0.55f, h * 0.45f), h * 0.2f, h * 0.15f, color, 5);
            }
        }

        // ---- Faces ----

        /// <summary>A person's face: eyes with pupils, brows, nose and mouth.</summary>
        public Figure Face(bool brows = true)
        {
            float h = Spec.HeadSize;
            HeadPart();
            var f = Head + new Vector3(0f, 0f, h * 0.86f);
            for (int side = -1; side <= 1; side += 2)
            {
                var eye = f + new Vector3(side * h * 0.33f, h * 0.08f, -h * 0.01f);
                Kit.Box(eye, new Vector3(h * 0.3f, h * 0.26f, h * 0.1f), P.Eye);
                Kit.Box(eye + new Vector3(0f, -h * 0.01f, h * 0.05f), new Vector3(h * 0.13f, h * 0.17f, h * 0.04f), P.Pupil);
                if (brows) Kit.Box(eye + new Vector3(0f, h * 0.22f, h * 0.03f), new Vector3(h * 0.32f, h * 0.06f, h * 0.06f), P.Hair, 1f, Quaternion.Euler(0f, 0f, side * -8f));
            }
            // A wedge nose and a straight mouth.
            Kit.Box(f + new Vector3(0f, -h * 0.26f, h * 0.02f), new Vector3(h * 0.2f, h * 0.32f, h * 0.16f), P.SkinShade, 0.35f);
            Kit.Box(f + new Vector3(0f, -h * 0.42f, -h * 0.05f), new Vector3(h * 0.34f, h * 0.05f, h * 0.05f), P.SkinShade);
            Kit.Body();
            return this;
        }

        /// <summary>A monster's face: deep sockets with glowing eyes, a heavy brow and a row of teeth.</summary>
        public Figure MonsterFace(float eyeSize = 1f, bool teeth = true)
        {
            float h = Spec.HeadSize;
            HeadPart();
            var f = Head + new Vector3(0f, 0f, h * 0.84f);
            for (int side = -1; side <= 1; side += 2)
            {
                var eye = f + new Vector3(side * h * 0.34f, h * 0.1f, -h * 0.08f);
                Kit.Ball(eye, new Vector3(h * 0.2f, h * 0.15f, h * 0.08f) * eyeSize, P.Pupil, 7, 4);
                Kit.Ball(eye + new Vector3(0f, 0f, h * 0.05f), new Vector3(h * 0.1f, h * 0.08f, h * 0.04f) * eyeSize, P.Glow, 6, 3);
            }
            Kit.Box(f + new Vector3(0f, h * 0.3f, -h * 0.02f), new Vector3(h * 1.1f, h * 0.16f, h * 0.2f), P.SkinShade, 0.9f);
            // Jaw.
            Kit.Block(Head + new Vector3(0f, -h * 0.6f, h * 0.38f), new Vector3(h * 1.25f, h * 0.58f, h * 0.95f), P.SkinShade, 0.45f);
            if (teeth)
                for (int i = -2; i <= 2; i++)
                {
                    var root = Head + new Vector3(i * h * 0.13f, -h * 0.42f, h * 0.82f);
                    Kit.Limb(root, root + new Vector3(0f, -h * 0.17f, h * 0.02f), h * 0.05f, 0f, P.Eye, 4);
                }
            Kit.Body();
            return this;
        }

        public Figure Hair(Color color, float volume = 1f)
        {
            float h = Spec.HeadSize;
            HeadPart();
            bool smooth = Kit.Smooth;
            Kit.Smooth = false;
            Kit.Ball(Head + new Vector3(0f, h * 0.24f, -h * 0.12f), new Vector3(h * 1.02f, h * 0.85f, h * 0.98f) * volume, color, 8, 5, Facing);
            Kit.Ball(Head + new Vector3(0f, -h * 0.15f, -h * 0.55f), new Vector3(h * 0.84f, h * 0.7f, h * 0.45f) * volume, color, 8, 4, Facing);
            Kit.Smooth = smooth;
            Kit.Body();
            return this;
        }

        public Figure Ears(Color color, float length = 0.1f, bool pointed = true)
        {
            float h = Spec.HeadSize;
            HeadPart();
            for (int side = -1; side <= 1; side += 2)
            {
                var root = Head + new Vector3(side * h * 0.86f, h * 0.05f, -h * 0.05f);
                Kit.Limb(root, root + new Vector3(side * length, pointed ? length * 0.6f : 0f, -length * 0.3f), h * 0.18f, pointed ? 0f : h * 0.12f, color, 5);
            }
            Kit.Body();
            return this;
        }

        // ---- Clothing ----

        public Figure Boots(Color color, float height = 0.4f, Color? cuff = null)
        {
            var s = Spec;
            for (int side = 0; side < 2; side++)
            {
                bool left = side == 0;
                LegPart(left);
                Vector3 ankle = left ? AnkleL : AnkleR, knee = left ? KneeL : KneeR;
                var top = Vector3.Lerp(ankle, knee, Mathf.Clamp01(height / Mathf.Max(0.01f, knee.y - ankle.y)));
                Kit.Limb(new Vector3(ankle.x, s.LegThickness * 0.5f, ankle.z), top, s.LegThickness * 1.15f, s.LegThickness * 1.25f, color, Sides);
                Kit.Limb(top - Vector3.up * 0.02f, top + Vector3.up * 0.04f, s.LegThickness * 1.42f, s.LegThickness * 1.42f, cuff ?? color, Sides);
                Foot(ankle, color, 1.3f);
                Kit.Box(new Vector3(ankle.x, s.LegThickness * 0.12f, ankle.z + s.LegThickness * 0.55f), new Vector3(s.LegThickness * 2.3f, s.LegThickness * 0.25f, s.LegThickness * 3.8f), P.Leather * 0.6f);
            }
            Kit.Body();
            return this;
        }

        public Figure Trousers(Color color, float bagginess = 1.25f)
        {
            var s = Spec;
            for (int side = 0; side < 2; side++)
            {
                bool left = side == 0;
                LegPart(left);
                Vector3 hip = left ? HipL : HipR, knee = left ? KneeL : KneeR, ankle = left ? AnkleL : AnkleR;
                Kit.Limb(hip + Vector3.up * 0.05f, knee, s.LegThickness * 1.5f * bagginess, s.LegThickness * 1.15f * bagginess, color, Sides);
                Kit.Ball(knee, Vector3.one * s.LegThickness * 1.15f * bagginess, color, Sides, 4);
                Kit.Limb(knee, Vector3.Lerp(knee, ankle, 0.75f), s.LegThickness * 1.15f * bagginess, s.LegThickness * 1.0f * bagginess, color, Sides);
            }
            Kit.Body();
            Kit.Ball(Pelvis + Vector3.up * 0.03f, new Vector3(s.TorsoWidth * 1.02f, s.TorsoLength * 0.22f, s.TorsoDepth * 1.12f), color, 12, 6, TorsoRotation);
            return this;
        }

        /// <summary>A tunic over the torso with a skirt to the given drop below the hips.</summary>
        public Figure Tunic(Color color, float drop = 0.18f, float flare = 1.3f)
        {
            var s = Spec;
            Kit.Body();
            Kit.Lathe(Pelvis, new[]
            {
                new Vector2(-drop, s.TorsoWidth * flare), new Vector2(-drop * 0.4f, s.TorsoWidth * 1.15f),
                new Vector2(s.TorsoLength * 0.15f, s.TorsoWidth * 0.98f), new Vector2(s.TorsoLength * 0.62f, s.TorsoWidth * 1.15f),
                new Vector2(s.TorsoLength * 0.9f, s.TorsoWidth * 1.12f), new Vector2(s.TorsoLength * 0.97f, s.TorsoWidth * 0.9f), new Vector2(s.TorsoLength * 1.0f, s.TorsoWidth * 0.5f),
            }, 14, color, s.TorsoDepth / s.TorsoWidth * 1.05f, TorsoRotation);
            return this;
        }

        /// <summary>A long robe from the neck to the ground; the figure's legs stay inside it.</summary>
        public Figure Robe(Color color, float hem = 0.04f, float flare = 1.9f)
        {
            var s = Spec;
            Kit.Body();
            float top = s.LegLength + s.TorsoLength;
            Kit.Lathe(Vector3.zero, new[]
            {
                new Vector2(hem, s.TorsoWidth * flare), new Vector2(s.LegLength * 0.45f, s.TorsoWidth * 1.45f),
                new Vector2(s.LegLength, s.TorsoWidth * 1.12f), new Vector2(s.LegLength + s.TorsoLength * 0.62f, s.TorsoWidth * 1.18f),
                new Vector2(top - 0.06f, s.TorsoWidth * 1.12f), new Vector2(top - 0.015f, s.TorsoWidth * 0.9f), new Vector2(top, s.TorsoWidth * 0.45f),
            }, 16, color, s.TorsoDepth / s.TorsoWidth * 1.08f);
            return this;
        }

        public Figure Belt(Color color, Color buckle, int pouches = 2, float height = 0.04f)
        {
            var s = Spec;
            Kit.Body();
            var waist = Torso(0f, s.TorsoLength * 0.12f, 0f);
            float rx = s.TorsoWidth * 1.08f, rz = s.TorsoDepth * 1.18f;
            Kit.Lathe(waist - TorsoRotation * Vector3.up * 0.03f, new[] { new Vector2(0f, rx), new Vector2(0.065f, rx) }, 16, color, rz / rx, TorsoRotation);
            var front = waist + TorsoRotation * new Vector3(0f, 0.002f, rz + 0.005f);
            Kit.Block(front, new Vector3(0.07f, 0.06f, 0.02f), buckle, 0.3f, TorsoRotation);
            for (int i = 0; i < pouches; i++)
            {
                float a = (i % 2 == 0 ? 1f : -1f) * (0.9f + (i / 2) * 0.5f);
                var at = waist + TorsoRotation * new Vector3(Mathf.Sin(a) * rx * 1.05f, -0.06f, Mathf.Cos(a) * rz * 1.05f);
                Kit.Block(at, new Vector3(0.09f, 0.1f, 0.06f), P.Leather, 0.35f, TorsoRotation * Quaternion.Euler(0f, a * Mathf.Rad2Deg, 0f));
                Kit.Box(at + TorsoRotation * new Vector3(0f, 0.05f, 0f), new Vector3(0.1f, 0.025f, 0.07f), P.Leather * 0.8f, 1f, TorsoRotation * Quaternion.Euler(0f, a * Mathf.Rad2Deg, 0f));
            }
            return this;
        }

        /// <summary>A strip of cloth hanging from the belt at the front, like a tabard tail.</summary>
        public Figure Tabard(Color color, float length = 0.45f, float width = 0.1f)
        {
            var s = Spec;
            Kit.Body();
            var top = Torso(0f, s.TorsoLength * 0.12f, s.TorsoDepth * 1.2f);
            Kit.Box(top + new Vector3(0f, -length * 0.5f, 0.02f), new Vector3(width, length, 0.015f), color, 0.9f, Quaternion.Euler(-6f, 0f, 0f));
            return this;
        }

        public Figure Sleeves(Color color, bool rolled = false, Color? cuff = null)
        {
            var s = Spec;
            for (int side = 0; side < 2; side++)
            {
                bool left = side == 0;
                ArmPart(left);
                Vector3 sh = left ? ShoulderL : ShoulderR, el = left ? ElbowL : ElbowR, wr = left ? WristL : WristR;
                Deltoid(left, s.ArmThickness * 1.85f, color);
                Kit.Limb(sh, el, s.ArmThickness * 1.5f, s.ArmThickness * 1.3f, color, Sides);
                if (rolled)
                {
                    Kit.Limb(el - (el - sh).normalized * 0.03f, el + (wr - el).normalized * 0.03f, s.ArmThickness * 1.55f, s.ArmThickness * 1.5f, cuff ?? color, Sides);
                }
                else
                {
                    Kit.Ball(el, Vector3.one * s.ArmThickness * 1.3f, color, Sides, 4);
                    Kit.Limb(el, wr, s.ArmThickness * 1.3f, s.ArmThickness * 1.55f, color, Sides);
                    Kit.Limb(wr - (wr - el).normalized * 0.04f, wr, s.ArmThickness * 1.62f, s.ArmThickness * 1.62f, cuff ?? color, Sides);
                }
            }
            Kit.Body();
            return this;
        }

        public Figure Gloves(Color color)
        {
            var s = Spec;
            for (int side = 0; side < 2; side++)
            {
                bool left = side == 0;
                ArmPart(left);
                Vector3 el = left ? ElbowL : ElbowR, wr = left ? WristL : WristR, hand = left ? HandL : HandR;
                Kit.Limb(Vector3.Lerp(el, wr, 0.5f), wr, s.ArmThickness * 1.05f, s.ArmThickness * 1.15f, color, Sides);
                Hand(hand, left, color);
            }
            Kit.Body();
            return this;
        }

        /// <summary>A short shoulder cape: a domed collar with a jagged hem. Its back sways a little (CapeSway).</summary>
        public Figure Mantle(Color color, float length = 0.2f, float width = 1.6f, Color? lining = null)
        {
            var s = Spec;
            var top = Torso(0f, s.TorsoLength * 0.98f, 0f);
            Kit.Part(BodyPart.Cape, top + Vector3.up * 0.03f, length + 0.03f);
            float r = s.ShoulderWidth * width * 0.75f;
            Kit.Lathe(top, new[] { new Vector2(0.03f, s.TorsoWidth * 0.55f), new Vector2(-0.02f, r * 0.8f), new Vector2(-length * 0.6f, r), new Vector2(-length, r * 1.04f) }, 16, color, 0.78f, TorsoRotation);
            if (lining.HasValue) Kit.Lathe(top, new[] { new Vector2(-length, r * 1.0f), new Vector2(-length * 0.7f, r * 0.9f) }, 16, lining.Value, 0.76f, TorsoRotation);
            Kit.Body();
            return this;
        }

        /// <summary>
        /// A cloak hanging down the back to the given length above the ground, folded down the
        /// middle and billowing out below the shoulders. It is a grid of rows so it can curve
        /// when it swings (CapeSway).
        /// </summary>
        public Figure Cloak(Color color, float bottom = 0.25f, Color? inner = null)
        {
            var s = Spec;
            var top = Torso(0f, s.TorsoLength * 0.92f, -s.TorsoDepth * 0.85f);
            Kit.Part(BodyPart.Cape, top + new Vector3(0f, 0f, 0.04f), top.y - bottom);
            float w = s.ShoulderWidth * 1.15f;
            var bl = new Vector3(-w * 1.25f, bottom, top.z - 0.18f);
            var br = new Vector3(w * 1.25f, bottom, top.z - 0.18f);
            var tl = top + new Vector3(-w, 0f, 0f);
            var tr = top + new Vector3(w, 0f, 0f);
            const int rows = 6, cols = 4;
            var grid = new Vector3[rows + 1, cols + 1];
            for (int i = 0; i <= rows; i++)
                for (int j = 0; j <= cols; j++)
                {
                    float v = i / (float)rows, u = j / (float)cols;
                    var at = Vector3.Lerp(Vector3.Lerp(tl, tr, u), Vector3.Lerp(bl, br, u), v);
                    // Billows out at mid height; the centre fold sits furthest back.
                    float fold = 1f - Mathf.Abs(u * 2f - 1f);
                    at.z -= 0.07f * Mathf.Sin(v * Mathf.PI) + fold * Mathf.Lerp(0.03f, 0.05f, v);
                    grid[i, j] = at;
                }
            var ci = inner ?? color * 0.7f;
            for (int i = 0; i < rows; i++)
                for (int j = 0; j < cols; j++)
                {
                    Vector3 a = grid[i, j], b = grid[i, j + 1], c = grid[i + 1, j + 1], d = grid[i + 1, j];
                    Kit.Quad(a, b, c, d, color);
                    Kit.Quad(d, c, b, a, ci);
                }
            Kit.Body();
            return this;
        }

        public Figure Hood(Color color)
        {
            float h = Spec.HeadSize;
            HeadPart();
            // Set back from the face so it frames it, with a peak at the top.
            bool smooth = Kit.Smooth;
            Kit.Smooth = false;
            Kit.Ball(Head + new Vector3(0f, h * 0.18f, -h * 0.38f), new Vector3(h * 1.16f, h * 1.2f, h * 1.05f), color, 8, 6, Facing);
            Kit.Smooth = smooth;
            Kit.Limb(Head + new Vector3(0f, h * 1.05f, -h * 0.5f), Head + new Vector3(0f, h * 1.3f, -h * 1.0f), h * 0.35f, 0f, color, 8);
            Kit.Lathe(Head + new Vector3(0f, -h * 0.9f, -h * 0.2f), new[] { new Vector2(0f, h * 1.5f), new Vector2(h * 0.35f, h * 1.05f) }, 14, color, 0.9f);
            Kit.Body();
            return this;
        }

        /// <summary>A wide-brimmed pointed hat with a band, bent at the tip.</summary>
        public Figure WizardHat(Color color, Color band, float height = 0.42f)
        {
            float h = Spec.HeadSize;
            HeadPart();
            var b = Head + new Vector3(0f, h * 0.62f, -h * 0.05f);
            Kit.Lathe(b, new[] { new Vector2(0f, h * 2.1f), new Vector2(0.025f, h * 2.0f), new Vector2(0.03f, h * 1.05f) }, 16, color, 1f, Quaternion.Euler(-6f, 0f, 0f));
            Kit.Lathe(b, new[] { new Vector2(0.02f, h * 1.02f), new Vector2(0.08f, h * 1.0f) }, 14, band, 1f, Quaternion.Euler(-6f, 0f, 0f));
            Kit.Lathe(b, new[] { new Vector2(0.08f, h * 0.98f), new Vector2(height * 0.6f, h * 0.52f), new Vector2(height * 0.85f, h * 0.22f) }, 14, color, 1f, Quaternion.Euler(-6f, 0f, 0f));
            var bendBase = b + Quaternion.Euler(-6f, 0f, 0f) * Vector3.up * height * 0.85f;
            Kit.Limb(bendBase, bendBase + new Vector3(0f, height * 0.12f, -height * 0.22f), h * 0.22f, 0f, color, 7);
            Kit.Body();
            return this;
        }

        public Figure Crown(Color color, int spikes = 7, float height = 0.07f)
        {
            float h = Spec.HeadSize;
            HeadPart();
            var b = Head + new Vector3(0f, h * 0.65f, -h * 0.02f);
            Kit.Lathe(b, new[] { new Vector2(0f, h * 0.8f), new Vector2(0.035f, h * 0.82f) }, 14, color);
            for (int i = 0; i < spikes; i++)
            {
                float a = i * Mathf.PI * 2f / spikes;
                var root = b + new Vector3(Mathf.Cos(a) * h * 0.8f, 0.03f, Mathf.Sin(a) * h * 0.8f);
                Kit.Limb(root, root + Vector3.up * height * (i % 2 == 0 ? 1.3f : 0.8f), h * 0.12f, 0f, color, 5);
            }
            Kit.Body();
            return this;
        }

        public Figure Pauldrons(Color color, float size = 0.12f, Color? trim = null)
        {
            for (int side = 0; side < 2; side++)
            {
                bool left = side == 0;
                ArmPart(left);
                var sh = left ? ShoulderL : ShoulderR;
                float sx = left ? -1f : 1f;
                // Two overlapping plates, each a chamfered slab angled further down the arm.
                for (int layer = 0; layer < 2; layer++)
                    Kit.Block(sh + new Vector3(sx * size * (0.3f + layer * 0.25f), size * (0.38f - layer * 0.42f), 0f),
                        new Vector3(size * (2.1f - layer * 0.2f), size * 0.42f, size * (2.0f - layer * 0.1f)), layer == 0 ? color : (trim ?? color * 0.85f), 0.5f,
                        Quaternion.Euler(0f, 0f, sx * -(20f + layer * 18f)));
            }
            Kit.Body();
            return this;
        }

        public Figure Breastplate(Color color, Color? trim = null)
        {
            var s = Spec;
            Kit.Body();
            Kit.Ball(Chest + TorsoRotation * new Vector3(0f, 0f, s.TorsoDepth * 0.12f), new Vector3(s.TorsoWidth * 1.16f, s.TorsoLength * 0.38f, s.TorsoDepth * 1.18f), color, 14, 7, TorsoRotation);
            if (trim.HasValue)
                Kit.Box(Chest + TorsoRotation * new Vector3(0f, 0f, s.TorsoDepth * 1.26f), new Vector3(0.025f, s.TorsoLength * 0.5f, 0.02f), trim.Value, 1f, TorsoRotation);
            return this;
        }

        public Figure Horns(float length, float thickness, Color color, float spread = 0.7f, float curl = 1f)
        {
            float h = Spec.HeadSize;
            HeadPart();
            for (int side = -1; side <= 1; side += 2)
            {
                var root = Head + new Vector3(side * h * spread, h * 0.55f, -h * 0.05f);
                var p1 = root + new Vector3(side * length * 0.3f, length * 0.4f, -length * 0.1f);
                var p2 = p1 + new Vector3(side * length * 0.2f, length * 0.3f * curl, -length * 0.35f);
                var p3 = p2 + new Vector3(side * length * 0.02f, -length * 0.05f * curl, -length * 0.3f);
                Kit.Limb(root, p1, thickness, thickness * 0.75f, color, 7);
                Kit.Limb(p1, p2, thickness * 0.75f, thickness * 0.45f, color, 7);
                Kit.Limb(p2, p3, thickness * 0.45f, 0f, color, 6);
            }
            Kit.Body();
            return this;
        }

        public Figure Spines(int count, float length, Color color)
        {
            var s = Spec;
            Kit.Body();
            for (int i = 0; i < count; i++)
            {
                float t = (i + 0.5f) / count;
                var root = Torso(0f, s.TorsoLength * Mathf.Lerp(0.2f, 0.95f, t), -s.TorsoDepth * 0.92f);
                var tip = root + TorsoRotation * new Vector3(0f, length * 0.45f, -length);
                Kit.Limb(root, tip, length * 0.22f, 0f, color, 6);
            }
            return this;
        }

        public Figure Tail(Color color, Color tip, float length = 0.5f)
        {
            Kit.Body();
            var root = Torso(0f, 0.02f, -Spec.TorsoDepth * 0.95f);
            var mid = root + new Vector3(0f, -0.1f, -length * 0.5f);
            var end = mid + new Vector3(0f, -0.15f, -length * 0.5f);
            Kit.Limb(root, mid, 0.05f, 0.09f, color, 8);
            Kit.Limb(mid, end, 0.09f, 0.05f, color, 8);
            Kit.Ball(end, Vector3.one * 0.06f, tip, 8, 5);
            return this;
        }

        /// <summary>A weapon in the right hand. Kinds: staff, sword, axe, trident, spear, hammer.</summary>
        public Figure Weapon(string kind, float length, Color head, Color shaft)
        {
            HeldPart(false);
            var h = HandR;
            var up = Vector3.up;
            var dir = kind == "staff" || kind == "spear" || kind == "trident" ? up : new Vector3(0f, 0.35f, 1f).normalized;
            var bottom = kind == "staff" || kind == "spear" || kind == "trident" ? h - up * length * 0.42f : h - dir * 0.12f;
            var top = bottom + dir * length;
            switch (kind)
            {
                case "staff":
                    Kit.Limb(bottom, top, 0.022f, 0.028f, shaft, 7);
                    Kit.Limb(top, top + new Vector3(0.05f, 0.08f, 0f), 0.028f, 0.04f, shaft, 7);
                    Kit.Ball(top + new Vector3(0.06f, 0.14f, 0f), new Vector3(0.06f, 0.07f, 0.06f), head, 10, 6);
                    Kit.Limb(top + new Vector3(0.1f, 0.06f, 0f), top + new Vector3(0.1f, 0.22f, 0f), 0.02f, 0.012f, shaft, 6);
                    break;
                case "spear":
                case "trident":
                    Kit.Limb(bottom, top, 0.018f, 0.018f, shaft, 7);
                    if (kind == "spear") Kit.Limb(top, top + up * 0.22f, 0.04f, 0f, head, 6);
                    else
                    {
                        Kit.Limb(top + Vector3.left * 0.09f, top + Vector3.right * 0.09f, 0.018f, 0.018f, head, 6);
                        for (int i = -1; i <= 1; i++) Kit.Limb(top + Vector3.right * i * 0.08f, top + Vector3.right * i * 0.09f + up * 0.24f, 0.022f, 0f, head, 5);
                    }
                    break;
                case "axe":
                    Kit.Limb(bottom, top, 0.022f, 0.02f, shaft, 7);
                    Kit.Block(top - dir * 0.1f + Vector3.right * 0.11f, new Vector3(0.22f, 0.28f, 0.04f), head, 0.3f, Quaternion.LookRotation(Vector3.right, dir));
                    break;
                case "hammer":
                    Kit.Limb(bottom, top, 0.025f, 0.025f, shaft, 7);
                    Kit.Block(top, new Vector3(0.24f, 0.14f, 0.14f), head, 0.25f, Quaternion.LookRotation(dir) * Quaternion.Euler(90f, 0f, 0f));
                    break;
                default:
                    Kit.Limb(bottom, h + dir * 0.06f, 0.022f, 0.022f, shaft, 7);
                    Kit.Limb(h + dir * 0.05f + Vector3.left * 0.1f, h + dir * 0.05f + Vector3.right * 0.1f, 0.022f, 0.022f, P.Trim, 6);
                    Kit.Block(Vector3.Lerp(h + dir * 0.08f, top, 0.5f), new Vector3(0.06f, (top - h).magnitude * 0.9f, 0.015f), head, 0.2f, Quaternion.LookRotation(Vector3.right, dir));
                    Kit.Limb(top - dir * 0.02f, top + dir * 0.12f, 0.03f, 0f, head, 4);
                    break;
            }
            Kit.Body();
            return this;
        }

        /// <summary>A book held in the left hand.</summary>
        public Figure Book(Color cover, Color pages)
        {
            HeldPart(true);
            var at = HandL + new Vector3(0f, -0.02f, 0.06f);
            Kit.Block(at, new Vector3(0.05f, 0.18f, 0.14f), cover, 0.15f);
            Kit.Box(at + new Vector3(0.012f, 0f, 0f), new Vector3(0.04f, 0.16f, 0.13f), pages);
            Kit.Body();
            return this;
        }

        /// <summary>A strap across the chest, for quivers and packs.</summary>
        public Figure Strap(Color color)
        {
            var s = Spec;
            Kit.Body();
            var a = Torso(-s.TorsoWidth * 0.9f, s.TorsoLength * 0.95f, s.TorsoDepth * 0.4f);
            var b = Torso(s.TorsoWidth * 0.95f, s.TorsoLength * 0.15f, s.TorsoDepth * 1.05f);
            var c = Torso(s.TorsoWidth * 0.9f, s.TorsoLength * 0.2f, -s.TorsoDepth * 1.05f);
            var d = Torso(-s.TorsoWidth * 0.9f, s.TorsoLength * 0.95f, -s.TorsoDepth * 0.4f);
            var mid = Torso(0f, s.TorsoLength * 0.55f, s.TorsoDepth * 1.15f);
            var back = Torso(0f, s.TorsoLength * 0.55f, -s.TorsoDepth * 1.15f);
            Kit.Limb(a, mid, 0.02f, 0.02f, color, 5); Kit.Limb(mid, b, 0.02f, 0.02f, color, 5);
            Kit.Limb(d, back, 0.02f, 0.02f, color, 5); Kit.Limb(back, c, 0.02f, 0.02f, color, 5);
            return this;
        }
    }
}
