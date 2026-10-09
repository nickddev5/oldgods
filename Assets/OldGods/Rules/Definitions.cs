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
        public readonly List<WeaponDef> Weapons = new List<WeaponDef>();
        public readonly List<PassiveDef> Passives = new List<PassiveDef>();

        public WeaponDef Weapon(string id) => Weapons.Find(w => w.Id == id) ?? throw new KeyNotFoundException($"No weapon with id '{id}'");
        public PassiveDef Passive(string id) => Passives.Find(p => p.Id == id) ?? throw new KeyNotFoundException($"No passive with id '{id}'");

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
            var ids = new HashSet<string>();
            foreach (var w in Weapons)
            {
                if (string.IsNullOrEmpty(w.Id) || !ids.Add(w.Id)) problems.Add($"Weapon id '{w.Id}' is empty or duplicated");
                if (w.Base.Cooldown <= 0f) problems.Add($"Weapon '{w.Id}' has Cooldown <= 0");
                if (w.Base.Damage <= 0f) problems.Add($"Weapon '{w.Id}' has Damage <= 0");
                if (w.UpgradePool.Count == 0) problems.Add($"Weapon '{w.Id}' has no upgrades");
            }
            foreach (var p in Passives)
                if (string.IsNullOrEmpty(p.Id) || !ids.Add(p.Id)) problems.Add($"Passive id '{p.Id}' is empty or duplicated");
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
