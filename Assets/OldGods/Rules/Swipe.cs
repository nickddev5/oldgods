using System;

namespace OldGods.Rules
{
    /// <summary>
    /// A close-range swipe (Rending Claws): an arc in front of the player, aimed at the nearest
    /// foe. Size is the reach in metres and Range is the arc's full width in degrees. Extra
    /// swipes are spaced evenly round the player, so the second one strikes behind.
    /// Headings are on the ground plane, in radians from +z toward +x.
    /// </summary>
    public static class Swipe
    {
        /// <summary>The heading toward a point dx, dz from the player.</summary>
        public static float HeadingTo(float dx, float dz) => (float)Math.Atan2(dx, dz);

        /// <summary>The heading of swipe index (0-based) of count, the first along aim.</summary>
        public static float Heading(float aim, int index, int count) =>
            count <= 1 ? aim : aim + index * (float)(Math.PI * 2.0) / count;

        /// <summary>
        /// Whether a foe dx, dz from the player with the given body radius is caught by a swipe
        /// along heading: its body reaches inside the reach, and its centre lies within the arc.
        /// A foe standing on the player is always caught.
        /// </summary>
        public static bool Catches(float dx, float dz, float targetRadius, float heading, float reach, float arcDegrees)
        {
            float d2 = dx * dx + dz * dz;
            float r = reach + targetRadius;
            if (d2 > r * r) return false;
            if (d2 <= targetRadius * targetRadius) return true;
            float off = Math.Abs(Wrap(HeadingTo(dx, dz) - heading));
            return off <= Math.Min(360f, arcDegrees) * (float)(Math.PI / 360.0);
        }

        /// <summary>An angle wrapped into -pi..pi.</summary>
        static float Wrap(float a)
        {
            const float tau = (float)(Math.PI * 2.0);
            a %= tau;
            if (a > Math.PI) a -= tau;
            if (a < -Math.PI) a += tau;
            return a;
        }
    }
}
