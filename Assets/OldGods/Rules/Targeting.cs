namespace OldGods.Rules
{
    /// <summary>
    /// Which foe an aimed weapon fires at. Projectile, chain and single-target area weapons take
    /// a boss in range before the nearest foe, so a thick horde does not soak every shot during a
    /// boss fight. Auras, orbits and swipes hit what is around the player and do not aim.
    /// </summary>
    public static class Targeting
    {
        /// <summary>
        /// A foe this close (metres) is shot before the boss: the player defends first. Without
        /// this the first boss-first sweep let the horde walk in during boss fights (Grey Steppe
        /// boss damage 3 to 16).
        /// </summary>
        public const float SelfDefenceRange = 3.5f;

        /// <summary>Whether a boss in range should be aimed at, given how far the nearest foe is.</summary>
        public static bool AimAtBoss(int boss, float nearestDistance) => boss >= 0 && nearestDistance > SelfDefenceRange;

        /// <summary>The boss slot when one is in range (boss &gt;= 0), otherwise the nearest foe.</summary>
        public static int Choose(int nearest, int boss) => boss >= 0 ? boss : nearest;
        /// <summary>
        /// Puts the boss first in a nearest-first list of count slots: moved up if it is already
        /// listed, otherwise inserted (dropping the farthest when the list is full). Returns the
        /// new count. No boss (boss &lt; 0) leaves the list as it is.
        /// </summary>
        public static int BossFirst(int[] list, int count, int boss)
        {
            if (boss < 0 || list == null || list.Length == 0) return count;
            int at = -1;
            for (int i = 0; i < count; i++)
                if (list[i] == boss) { at = i; break; }
            if (at < 0)
            {
                if (count < list.Length) count++;
                at = count - 1;
            }
            for (int i = at; i > 0; i--) list[i] = list[i - 1];
            list[0] = boss;
            return count;
        }
    }
}
