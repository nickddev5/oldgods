using System;
using System.Collections.Generic;

namespace OldGods.Rules
{
    /// <summary>A common horde enemy. Built from content assets at load.</summary>
    public sealed class EnemyDef
    {
        public string Id;
        public string DisplayName;
        public float MaxHealth = 10f;
        public float MoveSpeed = 3.5f;
        public float Radius = 0.45f;
        public float ContactDamage = 5f;
        public int XpValue = 1;
        public float Scale = 1f;
        /// <summary>Chance to drop gold on death.</summary>
        public float GoldChance = 0.02f;
        public bool IsElite;
    }

    /// <summary>All content for a session, as engine-free records.</summary>
    public sealed class ContentSet
    {
        public readonly Dictionary<string, EnemyDef> Enemies = new Dictionary<string, EnemyDef>();

        public EnemyDef Enemy(string id)
        {
            if (Enemies.TryGetValue(id, out var def)) return def;
            throw new KeyNotFoundException($"No enemy with id '{id}'");
        }

        /// <summary>Returns a list of problems: empty ids, duplicate ids, non-positive stats.</summary>
        public List<string> Validate()
        {
            var problems = new List<string>();
            foreach (var kv in Enemies)
            {
                var e = kv.Value;
                if (string.IsNullOrEmpty(e.Id)) problems.Add("Enemy with empty id");
                if (e.MaxHealth <= 0f) problems.Add($"Enemy '{e.Id}' has MaxHealth <= 0");
                if (e.MoveSpeed < 0f) problems.Add($"Enemy '{e.Id}' has negative MoveSpeed");
                if (e.Radius <= 0f) problems.Add($"Enemy '{e.Id}' has Radius <= 0");
            }
            return problems;
        }

        public void Add(EnemyDef def)
        {
            if (def == null) throw new ArgumentNullException(nameof(def));
            if (Enemies.ContainsKey(def.Id)) throw new ArgumentException($"Duplicate enemy id '{def.Id}'");
            Enemies.Add(def.Id, def);
        }
    }
}
