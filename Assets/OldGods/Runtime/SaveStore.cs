using System;
using System.IO;
using OldGods.Rules;
using UnityEngine;

namespace OldGods.Runtime
{
    /// <summary>
    /// Reads and writes the save as JSON in persistentDataPath/oldgods/. Writes go to a
    /// temp file first and then replace the save, so a crash mid-write never corrupts it.
    /// A save that cannot be read or migrated is kept aside as .corrupt and a fresh one starts.
    /// </summary>
    public static class SaveStore
    {
        public const string FileName = "save.json";

        /// <summary>Override for tests and the smoke runner so they never touch the real save.</summary>
        public static string FolderOverride;

        public static string Folder => FolderOverride ?? Path.Combine(Application.persistentDataPath, "oldgods");
        public static string SavePath => Path.Combine(Folder, FileName);

        static SaveData current;

        public static SaveData Current
        {
            get
            {
                if (current == null) current = Load(out _);
                return current;
            }
        }

        public enum LoadResult { Loaded, NewSave, RecoveredFromCorrupt }

        public static SaveData Load(out LoadResult result)
        {
            Directory.CreateDirectory(Folder);
            if (!File.Exists(SavePath))
            {
                result = LoadResult.NewSave;
                return current = new SaveData();
            }
            try
            {
                string json = File.ReadAllText(SavePath);
                var data = JsonUtility.FromJson<SaveData>(json);
                if (data != null && SaveMigration.Migrate(data))
                {
                    result = LoadResult.Loaded;
                    return current = data;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"OldGods: save could not be read: {e.Message}");
            }

            string aside = SavePath + "." + DateTime.UtcNow.ToString("yyyyMMddHHmmss") + ".corrupt";
            try { File.Move(SavePath, aside); } catch (Exception) { /* keep going with a fresh save */ }
            Debug.LogWarning($"OldGods: unreadable save moved to {aside}; starting fresh");
            result = LoadResult.RecoveredFromCorrupt;
            return current = new SaveData();
        }

        public static void Save(SaveData data = null)
        {
            data ??= current;
            if (data == null) return;
            current = data;
            Directory.CreateDirectory(Folder);
            string tmp = SavePath + ".tmp";
            File.WriteAllText(tmp, JsonUtility.ToJson(data, true));
            // Windows can briefly lock the old file (indexing, antivirus); try again before giving up.
            for (int attempt = 0; ; attempt++)
            {
                try
                {
                    if (File.Exists(SavePath)) File.Replace(tmp, SavePath, null);
                    else File.Move(tmp, SavePath);
                    return;
                }
                catch (IOException) when (attempt < 4)
                {
                    System.Threading.Thread.Sleep(25 * (attempt + 1));
                }
            }
        }

        /// <summary>Deletes the save and starts a new one (the "new game" option).</summary>
        public static SaveData Reset()
        {
            if (File.Exists(SavePath)) File.Delete(SavePath);
            current = new SaveData();
            Save(current);
            return current;
        }

        /// <summary>Forgets the cached save so the next access reloads from disk.</summary>
        public static void Forget() => current = null;
    }
}
