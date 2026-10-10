using System;
using System.Collections.Generic;

namespace OldGods.Runtime
{
    /// <summary>
    /// What one play-bot run did, written as JSON for Tools/playbot.py to summarise. Plain
    /// public fields so JsonUtility can write it. Version goes up when fields change meaning.
    /// </summary>
    [Serializable]
    public sealed class PlayBotReport
    {
        public int version = 1;
        public string god = "", godName = "", seed = "", picks = "", build = "";
        /// <summary>won, died, timeout, locked or error.</summary>
        public string outcome = "";
        public string note = "";
        public string killedBy = "";
        public int killedInStage = -1;
        public float seconds, realSeconds, speedup;
        public int stagesCleared, bossesKilled, level, kills, chestsOpened, shrinesUsed, itemsFound, goldEarned, goldLeft, loreRead;
        public bool reachedThrone, won;
        public int embersEarned, embersBefore, embersAfter;
        public List<string> questsCompleted = new List<string>();
        public List<string> purchases = new List<string>();
        public List<Stage> stages = new List<Stage>();
        public List<Source> damage = new List<Source>();
        /// <summary>Seconds spent on each branch of the bot's decision trees (amount is seconds).</summary>
        public List<Source> decisions = new List<Source>();
        public float closeReach;
        /// <summary>True if anything turned the game's volume up during the run (the bot puts it back to 0).</summary>
        public bool audioHeard;
        public List<Pick> draft = new List<Pick>();
        public List<Held> weapons = new List<Held>();
        public List<Held> passives = new List<Held>();
        public List<string> items = new List<string>();
        public List<Spot> stuck = new List<Spot>();
        public List<Spot> fellOut = new List<Spot>();
        public Moves moves = new Moves();
        public Perf perf = new Perf();
        public Catalog content = new Catalog();
        public List<string> log = new List<string>();

        [Serializable]
        public sealed class Stage
        {
            public int index;
            public string biome = "", biomeName = "", boss = "";
            public bool final, cleared, reachedSwarm;
            public float startedAt, seconds, bossWokeAt = -1f, bossSeconds = -1f, damageTaken, bossDamageTaken, lowestHealth = 1f;
            /// <summary>The run's difficulty coefficient when the stage began and when it ended.</summary>
            public float difficultyStart, difficultyEnd;
            /// <summary>Paid chests left closed on the map (and how many of them the bot had seen), and gold offerings made.</summary>
            public int chestsUnopened, chestsUnopenedFound, offerings;
            public int levelAtEnd, kills, chestsOpened, shrinesUsed, goldAtEnd, stuck, fellOut, peakAlive;
        }

        [Serializable]
        public sealed class Source
        {
            public string name = "";
            public float amount;
            public int hits;
        }

        [Serializable]
        public sealed class Pick
        {
            public float t;
            public int stage, level;
            public List<string> offered = new List<string>();
            public string taken = "", kind = "", name = "", rarity = "";
        }

        [Serializable]
        public sealed class Held
        {
            public string id = "", name = "";
            public int level;
            public float damage;
        }

        [Serializable]
        public sealed class Spot
        {
            public int stage;
            public float t, x, z;
            public string doing = "";
        }

        [Serializable]
        public sealed class Moves
        {
            public int jumps, slides, dodges, interactions;
            public float metres;
        }

        [Serializable]
        public sealed class Perf
        {
            public int frames, peakAlive;
            public float fpsMean, fpsLow1, fpsMin, simMsMean, simMsMax;
        }

        [Serializable]
        public sealed class Catalog
        {
            public List<Named> gods = new List<Named>();
            public List<Named> weapons = new List<Named>();
            public List<Named> passives = new List<Named>();
        }

        [Serializable]
        public sealed class Named
        {
            public string id = "", name = "", weapon = "";
            public bool unlocked;
            public int cost;
        }

        public Source SourceForDecision(string name)
        {
            var s = decisions.Find(d => d.name == name);
            if (s == null) decisions.Add(s = new Source { name = name });
            return s;
        }

        public Source SourceFor(string name)
        {
            var s = damage.Find(d => d.name == name);
            if (s == null) damage.Add(s = new Source { name = name });
            return s;
        }
    }
}
