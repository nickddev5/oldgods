namespace OldGods.Rules
{
    /// <summary>
    /// The look of the slide, the player's evasive move. Each god slides in its own way; the
    /// slide's speed, length and hitbox are the same for all of them (MotorTuning), so a style
    /// only changes what the player sees.
    /// </summary>
    public enum DodgeStyle { Slide, StormCloud, ForgeCharge, TideWave, BeastBound, EmberDive, EarthBurrow, EliasGlide }

    /// <summary>What rises under the god's feet while it slides.</summary>
    public enum DodgeMount { None, Cloud, Wave, Furrow, Halo }

    /// <summary>What the slide leaves behind.</summary>
    public enum DodgeTrail { None, Mist, Sparks, Spray, Dust, Embers, Rocks, Motes }

    /// <summary>
    /// A slide pose, at full blend. Limb angles are radians, as the shader's walk uses them: a
    /// negative hip or shoulder swings the foot or hand forward, a positive knee or elbow bends
    /// the lower limb back, a negative elbow bends the forearm up. Raise lifts an arm sideways
    /// (left arm out is negative, right arm out is positive). Waist bends everything above the
    /// hips forward about the hips; Nod tips the head forward. Gallop swings legs and arms in
    /// opposite pairs with the stride. Body angles are degrees and Height is metres, applied
    /// to the whole model: Pitch tips it forward (negative leans back), Yaw turns it from the
    /// direction of travel.
    /// </summary>
    public struct DodgePose
    {
        public float HipLeft, HipRight, KneeLeft, KneeRight;
        public float ShoulderLeft, ShoulderRight, RaiseLeft, RaiseRight;
        public float ElbowLeft, ElbowRight, Waist, Nod;
        public float Gallop;
        public float Pitch, Yaw, Roll, Height;
        public DodgeMount Mount;
        public DodgeTrail Trail;
    }

    public static class Dodges
    {
        /// <summary>The pose for a style. Every number is PLACEHOLDER until Nick marks it FINAL.</summary>
        public static DodgePose Pose(DodgeStyle style)
        {
            switch (style)
            {
                case DodgeStyle.StormCloud:
                    // Rides a storm cloud: a low surfing crouch, the free hand reaching ahead and the
                    // spear raised over the shoulder point first, as if to throw.
                    return new DodgePose
                    {
                        HipLeft = -0.55f, HipRight = 0.25f, KneeLeft = 0.8f, KneeRight = 0.6f,
                        ShoulderLeft = -0.9f, ShoulderRight = 2.4f, RaiseLeft = -0.3f, RaiseRight = 0.15f,
                        ElbowLeft = -0.2f, ElbowRight = -0.9f, Waist = 0.18f, Nod = -0.1f,
                        Pitch = -5f, Height = 0.42f, Mount = DodgeMount.Cloud, Trail = DodgeTrail.Mist,
                    };
                case DodgeStyle.ForgeCharge:
                    // Lowers a shoulder and charges, the left forearm up as a guard and the hammer on
                    // the right shoulder, boots skidding sparks.
                    return new DodgePose
                    {
                        HipLeft = -1.0f, HipRight = 0.7f, KneeLeft = 0.75f, KneeRight = 0.5f,
                        ShoulderLeft = -0.7f, ShoulderRight = -0.3f, RaiseLeft = 0.35f, RaiseRight = 0.1f,
                        ElbowLeft = -1.4f, ElbowRight = -1.6f, Waist = 0.62f, Nod = 0.1f,
                        Pitch = 6f, Yaw = 22f, Height = -0.2f, Trail = DodgeTrail.Sparks,
                    };
                case DodgeStyle.TideWave:
                    // Surfs a wave that rises under the feet, side-on, arms out for balance.
                    return new DodgePose
                    {
                        HipLeft = -0.55f, HipRight = -0.55f, KneeLeft = 1.0f, KneeRight = 1.0f,
                        ShoulderLeft = 0.2f, ShoulderRight = -0.3f, RaiseLeft = -1.15f, RaiseRight = 0.95f,
                        ElbowLeft = -0.2f, ElbowRight = -0.3f, Waist = 0.25f,
                        Yaw = 70f, Roll = -6f, Height = 0.32f, Mount = DodgeMount.Wave, Trail = DodgeTrail.Spray,
                    };
                case DodgeStyle.BeastBound:
                    // Drops to all fours and bounds: the chest down level, hands to the ground, head up.
                    return new DodgePose
                    {
                        HipLeft = -0.35f, HipRight = -0.6f, KneeLeft = 0.85f, KneeRight = 0.75f,
                        ShoulderLeft = -1.35f, ShoulderRight = -1.35f, ElbowRight = -0.15f,
                        Waist = 1.4f, Nod = -1.05f, Gallop = 0.45f,
                        Height = -0.24f, Trail = DodgeTrail.Dust,
                    };
                case DodgeStyle.EmberDive:
                    // Dives forward into a streak of flame, arms swept back, legs trailing.
                    return new DodgePose
                    {
                        HipLeft = 0.45f, HipRight = 0.7f, KneeLeft = 0.6f, KneeRight = 0.9f,
                        ShoulderLeft = 1.0f, ShoulderRight = 1.0f, RaiseLeft = -0.45f, RaiseRight = 0.45f,
                        ElbowLeft = -0.1f, ElbowRight = -0.1f, Waist = 0.8f, Nod = -0.6f,
                        Pitch = 22f, Height = 0.15f, Trail = DodgeTrail.Embers,
                    };
                case DodgeStyle.EarthBurrow:
                    // Sinks into the ground to the waist and ploughs through it, the staff held level.
                    return new DodgePose
                    {
                        HipLeft = -0.2f, HipRight = 0.2f,
                        ShoulderLeft = -0.8f, ShoulderRight = -0.95f, RaiseLeft = 0.3f, RaiseRight = -0.3f,
                        ElbowLeft = -0.8f, ElbowRight = -0.6f, Waist = 0.25f,
                        Height = -0.85f, Mount = DodgeMount.Furrow, Trail = DodgeTrail.Rocks,
                    };
                case DodgeStyle.EliasGlide:
                    // Glides upright a hand above the ground on a ring of light, arms open, the book in hand.
                    return new DodgePose
                    {
                        HipLeft = 0.12f, HipRight = 0.18f, KneeLeft = 0.1f, KneeRight = 0.15f,
                        ShoulderLeft = -0.2f, ShoulderRight = -0.4f, RaiseLeft = -0.45f, RaiseRight = 0.3f,
                        ElbowLeft = -0.3f, ElbowRight = -0.5f, Nod = 0.1f,
                        Pitch = 4f, Height = 0.2f, Mount = DodgeMount.Halo, Trail = DodgeTrail.Motes,
                    };
                default:
                    // The plain slide: legs forward, arms back, leaning back low to the ground.
                    return new DodgePose
                    {
                        HipLeft = -1.4f, HipRight = -1.15f, KneeLeft = 0.25f, KneeRight = 1.5f,
                        ShoulderLeft = 0.6f, ShoulderRight = 0.6f, RaiseLeft = -0.35f, RaiseRight = 0.21f,
                        ElbowLeft = -0.3f, ElbowRight = -0.15f,
                        Pitch = -16f, Height = -0.32f,
                    };
            }
        }
    }
}
