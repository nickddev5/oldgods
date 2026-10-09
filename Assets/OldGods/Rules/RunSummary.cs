using System;

namespace OldGods.Rules
{
    /// <summary>What a run achieved, for the results screen and the currency payout.</summary>
    public sealed class RunSummary
    {
        public string Seed = "";
        public string GodId = "";
        public int StagesCleared;   // stage bosses killed and left through the portal
        public int BossesKilled;    // includes cursed extra bosses and The Last Test
        public int Kills;
        public int Level;
        public float Seconds;
        /// <summary>Longest final swarm survived in any stage, in seconds.</summary>
        public float BestSwarmSeconds;
        public bool ReachedThrone;
        public bool Won;
        public int ChestsOpened;
        public int ShrinesUsed;
        public int GoldEarned;
        /// <summary>Sum of active difficulty modifiers' payout bonuses (0.25 = +25%).</summary>
        public float DifficultyBonus;
    }

    /// <summary>The Last Test can be beaten by any god; only Elias takes the throne.</summary>
    public static class LastTest
    {
        public const string EliasId = "god.elias";
        public static bool TakesTheThrone(string godId) => godId == EliasId;
    }

    /// <summary>The between-runs currency ("Embers", working name). PLACEHOLDER numbers.</summary>
    public static class RunRewards
    {
        public static int Embers(RunSummary s)
        {
            double basePay = 20.0 * s.StagesCleared
                             + 30.0 * s.BossesKilled
                             + s.Kills / 30.0
                             + (s.ReachedThrone ? 40.0 : 0.0)
                             + (s.Won ? 100.0 : 0.0);
            double swarm = FinalSwarm.SurvivalMultiplier(s.BestSwarmSeconds);
            double difficulty = 1.0 + Math.Max(0f, s.DifficultyBonus);
            return (int)Math.Round(basePay * swarm * difficulty);
        }
    }
}
