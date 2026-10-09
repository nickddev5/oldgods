using System;
using System.Collections.Generic;

namespace OldGods.Rules
{
    /// <summary>
    /// The save file. Plain public fields so the runtime can serialize it with JsonUtility.
    /// Bump CurrentVersion and add a step to SaveMigration whenever the shape changes.
    /// </summary>
    [Serializable]
    public sealed class SaveData
    {
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;
        public int currency;
        public int lifetimeCurrency;
        public List<string> unlocked = new List<string>();
        public List<PowerupLevel> powerups = new List<PowerupLevel>();
        public List<QuestProgress> quests = new List<QuestProgress>();
        public List<string> completedQuests = new List<string>();
        public RunRecord lastRun = new RunRecord();
        public int runsStarted;
        public int runsWon;
        public Settings settings = new Settings();
        public List<string> lore = new List<string>();
        public bool seenPremise;

        public bool IsUnlocked(string id) => unlocked.Contains(id);

        public void Unlock(string id)
        {
            if (!unlocked.Contains(id)) unlocked.Add(id);
        }
    }

    [Serializable]
    public sealed class PowerupLevel
    {
        public string id;
        public int level;
    }

    [Serializable]
    public sealed class QuestProgress
    {
        public string id;
        public int value;
    }

    [Serializable]
    public sealed class RunRecord
    {
        public string seed = "";
        public string god = "";
        public int stageReached;
        public float timeSeconds;
        public int kills;
        public int level;
        public int currencyEarned;
        public bool won;
    }

    [Serializable]
    public sealed class Settings
    {
        public float masterVolume = 0.8f;
        public float musicVolume = 0.6f;
        public float sfxVolume = 0.8f;
        public float cameraSensitivity = 1f;
        public float verticalSensitivity = 1f;
        public float screenShake = 1f;
        public bool invertY;
    }

    public static class SaveMigration
    {
        /// <summary>
        /// Brings an older save up to CurrentVersion. Returns false if the version is
        /// unknown (newer than this build, or below 1), in which case the caller should
        /// keep the file aside and start fresh.
        /// </summary>
        public static bool Migrate(SaveData data)
        {
            if (data == null) return false;
            if (data.version < 1 || data.version > SaveData.CurrentVersion) return false;
            // Version steps go here: if (data.version == 1) { ...; data.version = 2; }
            data.unlocked ??= new List<string>();
            data.powerups ??= new List<PowerupLevel>();
            data.quests ??= new List<QuestProgress>();
            data.completedQuests ??= new List<string>();
            data.lastRun ??= new RunRecord();
            data.settings ??= new Settings();
            data.lore ??= new List<string>();
            return data.version == SaveData.CurrentVersion;
        }
    }
}
