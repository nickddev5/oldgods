using System.Collections.Generic;
using OldGods.Rules;
using OldGods.Runtime;
using UnityEditor;
using UnityEngine;

namespace OldGods.Editor
{
    /// <summary>
    /// Creates the starting content assets the first time, then leaves their values alone
    /// so tuning done in the Inspector is kept. New entries are added when missing.
    /// Every name and number here is PLACEHOLDER (gameBible/04-content.md).
    /// </summary>
    public static class ContentAuthoring
    {
        public const string WeaponsDir = ProjectBuilder.Root + "/Content/Weapons";
        public const string PassivesDir = ProjectBuilder.Root + "/Content/Passives";
        public const string BossesDir = ProjectBuilder.Root + "/Content/Bosses";
        public const string ItemsDir = ProjectBuilder.Root + "/Content/Items";
        public const string BiomesDir = ProjectBuilder.Root + "/Content/Biomes";

        public static void Populate(ContentLibrary library)
        {
            library.Enemies.RemoveAll(e => e == null);
            library.Weapons.RemoveAll(e => e == null);
            library.Passives.RemoveAll(e => e == null);
            library.Items.RemoveAll(e => e == null);
            Enemies(library);
            Weapons(library);
            Passives(library);
            Items(library);
        }

        static void Enemies(ContentLibrary library)
        {
            Enemy(library, "Husk", e =>
            {
                e.Id = "enemy.husk"; e.DisplayName = "Husk";
                e.MaxHealth = 12f; e.MoveSpeed = 3.3f; e.Radius = 0.4f; e.ContactDamage = 4f; e.XpValue = 1;
                e.Color = new Color(0.55f, 0.32f, 0.26f);
            });
            Enemy(library, "Runner", e =>
            {
                e.Id = "enemy.runner"; e.DisplayName = "Runner";
                e.MaxHealth = 7f; e.MoveSpeed = 5.6f; e.Radius = 0.35f; e.ContactDamage = 3f; e.XpValue = 1; e.Scale = 0.8f;
                e.Color = new Color(0.62f, 0.5f, 0.3f);
            });
            Enemy(library, "Brute", e =>
            {
                e.Id = "enemy.brute"; e.DisplayName = "Brute";
                e.MaxHealth = 55f; e.MoveSpeed = 2.6f; e.Radius = 0.5f; e.ContactDamage = 9f; e.XpValue = 4; e.Scale = 1.5f;
                e.Color = new Color(0.32f, 0.3f, 0.36f); e.GoldChance = 0.1f;
            });
            Enemy(library, "Champion", e =>
            {
                e.Id = "enemy.champion"; e.DisplayName = "A Husk Champion";
                e.MaxHealth = 600f; e.MoveSpeed = 3.2f; e.Radius = 0.55f; e.ContactDamage = 14f; e.XpValue = 30; e.Scale = 2.3f;
                e.Color = new Color(0.5f, 0.2f, 0.18f); e.Emission = new Color(0.35f, 0.08f, 0.02f); e.IsElite = true; e.GoldChance = 1f;
            });
            Enemy(library, "Ghost", e =>
            {
                e.Id = "enemy.ghost"; e.DisplayName = "Ghost";
                e.MaxHealth = 20f; e.MoveSpeed = 5f; e.Radius = 0.4f; e.ContactDamage = 8f; e.XpValue = 2;
                e.Color = new Color(0.75f, 0.85f, 0.95f); e.Emission = new Color(0.25f, 0.4f, 0.6f); e.GoldChance = 0f;
            });
        }

        public static BiomeDefinition GreyboxBiome()
        {
            var boss = LoadOrCreate<BossDefinition>(BossesDir + "/StoneWarden.asset", b =>
            {
                b.Id = "boss.stone_warden"; b.DisplayName = "The Stone Warden"; b.Epithet = "It remembers the first wall";
                b.MaxHealth = 3000f; b.MoveSpeed = 2.6f; b.Scale = 3.2f; b.Radius = 1.6f; b.ContactDamage = 14f; b.Rest = 1.3f;
                b.MinionId = "enemy.husk"; b.Model = BossModel.Warden;
                b.Color = new Color(0.55f, 0.53f, 0.5f); b.Accent = new Color(1.6f, 0.7f, 0.25f);
                b.Attacks = new List<BossAttackDef>
                {
                    new BossAttackDef { Attack = BossAttack.Slam, Weight = 3f, Cooldown = 4f, Damage = 24f, Telegraph = 1.1f, Size = 4.5f },
                    new BossAttackDef { Attack = BossAttack.Charge, Weight = 2f, Cooldown = 6f, Damage = 20f, Telegraph = 0.9f, Size = 16f },
                    new BossAttackDef { Attack = BossAttack.Shockwave, Weight = 2f, Cooldown = 8f, Damage = 18f, Telegraph = 0.8f, Size = 22f },
                    new BossAttackDef { Attack = BossAttack.Summon, Weight = 1f, Cooldown = 14f, Damage = 0f, Telegraph = 0.8f, Size = 8f },
                };
            });
            return LoadOrCreate<BiomeDefinition>(BiomesDir + "/Greybox.asset", b =>
            {
                b.Id = "biome.greybox"; b.DisplayName = "The Proving Ground";
                b.Terrain = new TerrainProfile { HillHeight = 18f, HillScale = 55f, CliffStep = 3f, CliffAmount = 0.3f };
                b.Palette = new GroundPalette();
                b.Timeline = DefaultTimelines.Greybox();
                b.Boss = boss;
                b.TreeCount = 40;
            });
        }

        static WeaponStats Stats(float damage, float cooldown, float count, float size, float speed, float duration, float range,
            float knockback, float pierce = 0f) =>
            new WeaponStats { Damage = damage, Cooldown = cooldown, Count = count, Size = size, Speed = speed, Duration = duration, Range = range, Knockback = knockback, Pierce = pierce };

        static List<WeaponUpgrade> Ups(params (WeaponStat stat, float amount)[] ups)
        {
            var list = new List<WeaponUpgrade>();
            foreach (var (stat, amount) in ups) list.Add(new WeaponUpgrade(stat, amount));
            return list;
        }

        static void Weapons(ContentLibrary l)
        {
            Weapon(l, "ChainLightning", "weapon.chain_lightning", "Chain Lightning", "Lightning strikes the nearest foe and leaps to the next.",
                WeaponShape.Chain, Stats(9f, 1.4f, 3f, 6f, 0f, 0f, 16f, 0.5f),
                Ups((WeaponStat.Damage, 3f), (WeaponStat.Count, 1f), (WeaponStat.Cooldown, 0.1f), (WeaponStat.Range, 0.15f)),
                new Color(0.6f, 0.8f, 1f), new Color(1.2f, 1.6f, 2.4f), Vector3.one);
            Weapon(l, "HammerOrbit", "weapon.hammer_orbit", "Hammer Orbit", "Forge hammers circle you and batter what they touch.",
                WeaponShape.Orbit, Stats(12f, 0.5f, 2f, 1f, 3f, 0f, 2.8f, 6f),
                Ups((WeaponStat.Damage, 4f), (WeaponStat.Count, 1f), (WeaponStat.Size, 0.15f), (WeaponStat.Speed, 0.15f)),
                new Color(0.75f, 0.6f, 0.45f), new Color(1.8f, 1.1f, 0.5f), new Vector3(1.4f, 1.4f, 1.4f));
            Weapon(l, "TidalWave", "weapon.tidal_wave", "Tidal Wave", "A slow wall of water that sweeps through every foe in its path.",
                WeaponShape.Projectile, Stats(14f, 2.2f, 1f, 2.2f, 11f, 1.6f, 20f, 8f, 99f),
                Ups((WeaponStat.Damage, 5f), (WeaponStat.Size, 0.15f), (WeaponStat.Cooldown, 0.1f), (WeaponStat.Knockback, 0.2f)),
                new Color(0.35f, 0.6f, 0.85f), new Color(0.4f, 1f, 1.6f), new Vector3(5f, 2.2f, 1.2f));
            Weapon(l, "SpearVolley", "weapon.spear_volley", "Spear Volley", "Hunting spears fly at the nearest foes.",
                WeaponShape.Projectile, Stats(13f, 1f, 2f, 1f, 26f, 1.1f, 26f, 2f, 1f),
                Ups((WeaponStat.Damage, 3f), (WeaponStat.Count, 1f), (WeaponStat.Pierce, 1f), (WeaponStat.Cooldown, 0.1f)),
                new Color(0.85f, 0.8f, 0.65f), new Color(1.6f, 1.5f, 1.1f), new Vector3(0.8f, 0.8f, 2.2f));
            Weapon(l, "FlameAura", "weapon.flame_aura", "Flame Aura", "A ring of fire burns everything close to you.",
                WeaponShape.Aura, Stats(4f, 0.45f, 1f, 3f, 0f, 0f, 0f, 0.5f),
                Ups((WeaponStat.Damage, 1.5f), (WeaponStat.Size, 0.15f), (WeaponStat.Cooldown, 0.1f)),
                new Color(1f, 0.5f, 0.2f), new Color(2.2f, 0.9f, 0.3f), Vector3.one);
            Weapon(l, "Quake", "weapon.quake", "Quake", "The ground breaks around you, hurling foes back.",
                WeaponShape.Area, Stats(20f, 2.5f, 1f, 5.5f, 0f, 0.15f, 0f, 10f),
                Ups((WeaponStat.Damage, 6f), (WeaponStat.Size, 0.15f), (WeaponStat.Cooldown, 0.1f), (WeaponStat.Knockback, 0.2f)),
                new Color(0.6f, 0.5f, 0.35f), new Color(1.4f, 1.1f, 0.6f), Vector3.one);
            Weapon(l, "CrownLight", "weapon.crown_light", "Crown Light", "Light from above smites a nearby foe.",
                WeaponShape.Area, Stats(30f, 1.6f, 1f, 2.2f, 0f, 0.35f, 22f, 3f),
                Ups((WeaponStat.Damage, 8f), (WeaponStat.Count, 1f), (WeaponStat.Cooldown, 0.1f), (WeaponStat.Size, 0.15f)),
                new Color(1f, 0.92f, 0.6f), new Color(2.4f, 2.1f, 1.2f), Vector3.one);
            Weapon(l, "FrostShards", "weapon.frost_shards", "Frost Shards", "Shards of ice that slow what they hit.",
                WeaponShape.Projectile, Stats(7f, 0.9f, 3f, 0.9f, 18f, 1.2f, 20f, 1f),
                Ups((WeaponStat.Damage, 2f), (WeaponStat.Count, 1f), (WeaponStat.Cooldown, 0.1f), (WeaponStat.Speed, 0.15f)),
                new Color(0.7f, 0.9f, 1f), new Color(1.1f, 1.8f, 2.2f), new Vector3(0.9f, 0.9f, 1.3f), slow: 1.5f);
            Weapon(l, "GravePull", "weapon.grave_pull", "Grave Pull", "A dark well drags foes together and grinds them down.",
                WeaponShape.Pull, Stats(5f, 4f, 1f, 4f, 0f, 2.5f, 16f, 9f),
                Ups((WeaponStat.Damage, 2f), (WeaponStat.Size, 0.15f), (WeaponStat.Duration, 0.2f), (WeaponStat.Cooldown, 0.1f)),
                new Color(0.45f, 0.3f, 0.6f), new Color(0.9f, 0.4f, 1.6f), Vector3.one);
            Weapon(l, "BoneRing", "weapon.bone_ring", "Bone Ring", "A ring of bones spins wide around you.",
                WeaponShape.Orbit, Stats(6f, 0.4f, 4f, 0.8f, 4.5f, 0f, 4.2f, 2f),
                Ups((WeaponStat.Damage, 2f), (WeaponStat.Count, 1f), (WeaponStat.Speed, 0.15f), (WeaponStat.Range, 0.1f)),
                new Color(0.9f, 0.88f, 0.8f), new Color(1.6f, 1.6f, 1.4f), new Vector3(0.8f, 0.8f, 0.8f));
            Weapon(l, "ThunderCloud", "weapon.thunder_cloud", "Thunder Cloud", "Thunder falls on foes around you.",
                WeaponShape.Area, Stats(16f, 1.8f, 2f, 2.5f, 0f, 0.5f, 20f, 3f),
                Ups((WeaponStat.Damage, 5f), (WeaponStat.Count, 1f), (WeaponStat.Size, 0.12f), (WeaponStat.Cooldown, 0.1f)),
                new Color(0.7f, 0.75f, 1f), new Color(1.2f, 1.3f, 2.4f), Vector3.one);
            Weapon(l, "EmberRain", "weapon.ember_rain", "Ember Rain", "Burning embers fall in a scatter around foes.",
                WeaponShape.Area, Stats(9f, 1.2f, 4f, 1.8f, 0f, 0.8f, 14f, 1f),
                Ups((WeaponStat.Damage, 3f), (WeaponStat.Count, 1f), (WeaponStat.Size, 0.1f), (WeaponStat.Cooldown, 0.1f)),
                new Color(1f, 0.45f, 0.25f), new Color(2.2f, 0.7f, 0.3f), Vector3.one);
        }

        static void Passives(ContentLibrary l)
        {
            Passive(l, "Swiftness", "passive.swiftness", "Swiftness", StatId.MoveSpeed, 0.08f, new Color(0.6f, 0.9f, 0.6f));
            Passive(l, "IronSkin", "passive.iron_skin", "Iron Skin", StatId.Armor, 1f, new Color(0.7f, 0.7f, 0.75f));
            Passive(l, "Regeneration", "passive.regeneration", "Regeneration", StatId.Regen, 0.3f, new Color(0.9f, 0.5f, 0.6f));
            Passive(l, "KeenEye", "passive.keen_eye", "Keen Eye", StatId.CritChance, 0.05f, new Color(1f, 0.85f, 0.4f));
            Passive(l, "Fury", "passive.fury", "Fury", StatId.Damage, 0.08f, new Color(1f, 0.4f, 0.3f));
            Passive(l, "Bulwark", "passive.bulwark", "Bulwark", StatId.MaxHealth, 15f, new Color(0.8f, 0.3f, 0.3f));
            Passive(l, "Resolve", "passive.resolve", "Resolve", StatId.XpGain, 0.08f, new Color(1f, 0.95f, 0.7f));
            Passive(l, "Haste", "passive.haste", "Haste", StatId.AttackSpeed, 0.08f, new Color(0.9f, 0.9f, 0.5f));
            Passive(l, "Reach", "passive.reach", "Reach", StatId.Area, 0.1f, new Color(0.6f, 0.7f, 1f));
            Passive(l, "Multitude", "passive.multitude", "Multitude", StatId.ProjectileCount, 1f, new Color(0.8f, 0.6f, 1f));
            Passive(l, "Magnetism", "passive.magnetism", "Magnetism", StatId.PickupRange, 1f, new Color(0.5f, 0.8f, 1f));
            Passive(l, "Fortune", "passive.fortune", "Fortune", StatId.Luck, 0.08f, new Color(0.4f, 1f, 0.5f));
        }

        static void Items(ContentLibrary l)
        {
            Item(l, "Whetstone", "item.whetstone", "Whetstone", Rarity.Common, "A stone that remembers every edge.", new StatMod(StatId.Damage, 0.06f));
            Item(l, "RunnersSandals", "item.runners_sandals", "Runner's Sandals", Rarity.Common, "Worn thin on long roads.", new StatMod(StatId.MoveSpeed, 0.06f));
            Item(l, "HeartOfOak", "item.heart_of_oak", "Heart of Oak", Rarity.Common, "Slow to fall.", new StatMod(StatId.MaxHealth, 15f));
            Item(l, "LodeStone", "item.lodestone", "Lodestone", Rarity.Common, "Small things drift toward it.", new StatMod(StatId.PickupRange, 1.2f));
            Item(l, "WarDrum", "item.war_drum", "War Drum", Rarity.Uncommon, "Every beat a little faster.", new StatMod(StatId.AttackSpeed, 0.08f));
            Item(l, "GoldenThread", "item.golden_thread", "Golden Thread", Rarity.Uncommon, "Fortune follows it.", new StatMod(StatId.Luck, 0.08f), new StatMod(StatId.GoldGain, 0.1f));
            Item(l, "WideHorn", "item.wide_horn", "Wide Horn", Rarity.Uncommon, "Its call carries far.", new StatMod(StatId.Area, 0.1f));
            Item(l, "IronCollar", "item.iron_collar", "Iron Collar", Rarity.Rare, "Heavy, and worth it.", new StatMod(StatId.Armor, 2f), new StatMod(StatId.MaxHealth, 10f));
            Item(l, "Hourglass", "item.hourglass", "Hourglass", Rarity.Rare, "Sand that falls slowly.", new StatMod(StatId.Duration, 0.15f), new StatMod(StatId.XpGain, 0.06f));
            Item(l, "Bloodstone", "item.bloodstone", "Bloodstone", Rarity.Epic, "Each fallen foe may mend you.", "heal_on_kill", 0.08f, new StatMod(StatId.Regen, 0.2f));
            Item(l, "QuiverOfDawn", "item.quiver_of_dawn", "Quiver of Dawn", Rarity.Epic, "One more, always one more.", new StatMod(StatId.ProjectileCount, 1f));
            Item(l, "WingedCrown", "item.winged_crown", "Winged Crown", Rarity.Legendary, "Lighter than air.", new StatMod(StatId.ExtraJumps, 1f), new StatMod(StatId.MoveSpeed, 0.08f), new StatMod(StatId.CritChance, 0.06f));
        }

        static void Item(ContentLibrary library, string file, string id, string name, Rarity rarity, string description, params StatMod[] mods) =>
            Item(library, file, id, name, rarity, description, null, 0f, mods);

        static void Item(ContentLibrary library, string file, string id, string name, Rarity rarity, string description, string special, float specialValue, params StatMod[] mods)
        {
            var asset = LoadOrCreate<ItemDefinition>($"{ItemsDir}/{file}.asset", it =>
            {
                it.Id = id; it.DisplayName = name; it.Rarity = rarity; it.Description = description;
                it.Mods = new List<StatMod>(mods); it.Special = special ?? ""; it.SpecialValue = specialValue;
            });
            if (!library.Items.Contains(asset)) library.Items.Add(asset);
        }

        static EnemyDefinition Enemy(ContentLibrary library, string file, System.Action<EnemyDefinition> init)
        {
            var asset = LoadOrCreate($"{ProjectBuilder.EnemiesDir}/{file}.asset", init);
            if (!library.Enemies.Contains(asset)) library.Enemies.Add(asset);
            return asset;
        }

        static void Weapon(ContentLibrary library, string file, string id, string name, string description, WeaponShape shape,
            WeaponStats stats, List<WeaponUpgrade> ups, Color color, Color glow, Vector3 visualScale, float slow = 0f)
        {
            var asset = LoadOrCreate<WeaponDefinition>($"{WeaponsDir}/{file}.asset", w =>
            {
                w.Id = id; w.DisplayName = name; w.Description = description; w.Shape = shape;
                w.Base = stats; w.Upgrades = ups; w.Color = color; w.Glow = glow; w.VisualScale = visualScale;
                w.SlowSeconds = slow;
            });
            if (!library.Weapons.Contains(asset)) library.Weapons.Add(asset);
        }

        static void Passive(ContentLibrary library, string file, string id, string name, StatId stat, float add, Color color)
        {
            var asset = LoadOrCreate<PassiveDefinition>($"{PassivesDir}/{file}.asset", p =>
            {
                p.Id = id; p.DisplayName = name; p.Stat = stat; p.AddPerLevel = add; p.Color = color;
                p.Description = StatText.Describe(new StatMod(stat, add)) + " per level.";
            });
            if (!library.Passives.Contains(asset)) library.Passives.Add(asset);
        }

        static T LoadOrCreate<T>(string path, System.Action<T> init) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            ProjectBuilder.EnsureFolder(System.IO.Path.GetDirectoryName(path).Replace('\\', '/'));
            asset = ScriptableObject.CreateInstance<T>();
            init(asset);
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }
    }
}
