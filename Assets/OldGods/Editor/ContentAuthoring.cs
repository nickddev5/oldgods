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
            library.Gods.RemoveAll(e => e == null);
            Enemies(library);
            MoreEnemies(library);
            Weapons(library);
            Passives(library);
            Items(library);
            Gods(library);
        }

        public const string GodsDir = ProjectBuilder.Root + "/Content/Gods";

        static void Gods(ContentLibrary l)
        {
            GodAsset(l, "Storm", "god.storm", "Storm", "The sky's anger, patient until it is not.", "weapon.chain_lightning", "passive.swiftness",
                0, 0, GodLook.Storm, new Color(0.62f, 0.72f, 0.9f), new Color(0.8f, 1.2f, 2.2f), new StatMod(StatId.MoveSpeed, 0.05f));
            GodAsset(l, "Forge", "god.forge", "Forge", "Every blade began in his fire.", "weapon.hammer_orbit", "passive.iron_skin",
                1, 150, GodLook.Forge, new Color(0.72f, 0.52f, 0.35f), new Color(2.2f, 1.2f, 0.4f), new StatMod(StatId.Armor, 1f), new StatMod(StatId.MaxHealth, 10f));
            GodAsset(l, "Tide", "god.tide", "Tide", "What the tide takes, it returns changed.", "weapon.tidal_wave", "passive.regeneration",
                2, 300, GodLook.Tide, new Color(0.38f, 0.62f, 0.62f), new Color(0.3f, 1.4f, 1.8f), new StatMod(StatId.Regen, 0.2f));
            GodAsset(l, "Hunt", "god.hunt", "Hunt", "She has never once missed what she meant to hit.", "weapon.spear_volley", "passive.keen_eye",
                3, 500, GodLook.Hunt, new Color(0.45f, 0.55f, 0.35f), new Color(1.2f, 1.8f, 0.6f), new StatMod(StatId.CritChance, 0.05f));
            GodAsset(l, "Ember", "god.ember", "Ember", "A small fire that refused to go out.", "weapon.flame_aura", "passive.fury",
                4, 750, GodLook.Ember, new Color(0.8f, 0.38f, 0.28f), new Color(2.4f, 0.8f, 0.2f), new StatMod(StatId.Damage, 0.05f));
            GodAsset(l, "Earth", "god.earth", "Earth", "The ground remembers every step.", "weapon.quake", "passive.bulwark",
                5, 1000, GodLook.Earth, new Color(0.65f, 0.55f, 0.38f), new Color(1.6f, 1.2f, 0.5f), new StatMod(StatId.MaxHealth, 25f), new StatMod(StatId.MoveSpeed, -0.05f));
            GodAsset(l, "Elias", "god.elias", "Elias", "The last to come, and the one who ends it.", "weapon.crown_light", "passive.resolve",
                6, 1500, GodLook.Elias, new Color(0.92f, 0.9f, 0.84f), new Color(2.4f, 2.1f, 1.2f), new StatMod(StatId.XpGain, 0.1f));
        }

        static void GodAsset(ContentLibrary library, string file, string id, string name, string lore, string weapon, string passive,
            int order, int cost, GodLook look, Color robe, Color mark, params StatMod[] kit)
        {
            var asset = LoadOrCreate<GodDefinition>($"{GodsDir}/{file}.asset", g =>
            {
                g.Id = id; g.DisplayName = name; g.Lore = lore; g.StartingWeapon = weapon; g.StartingPassive = passive;
                g.Order = order; g.Cost = cost; g.IsLast = id == LastTest.EliasId; g.Look = look; g.Robe = robe; g.Mark = mark;
                g.Kit = new List<StatMod>(kit);
            });
            if (!library.Gods.Contains(asset)) library.Gods.Add(asset);
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
                e.Color = new Color(0.32f, 0.3f, 0.36f); e.GoldChance = 0.5f;
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

        static void MoreEnemies(ContentLibrary library)
        {
            Enemy(library, "Ashling", e =>
            {
                e.Id = "enemy.ashling"; e.DisplayName = "Ashling";
                e.MaxHealth = 6f; e.MoveSpeed = 6.2f; e.Radius = 0.3f; e.ContactDamage = 3f; e.XpValue = 1; e.Scale = 0.7f;
                e.Color = new Color(0.3f, 0.28f, 0.27f); e.Emission = new Color(0.18f, 0.05f, 0f);
            });
            Enemy(library, "Cinder", e =>
            {
                e.Id = "enemy.cinder"; e.DisplayName = "Cinder Husk";
                e.MaxHealth = 14f; e.MoveSpeed = 3.6f; e.Radius = 0.4f; e.ContactDamage = 6f; e.XpValue = 2;
                e.Color = new Color(0.22f, 0.2f, 0.2f); e.Emission = new Color(0.08f, 0.02f, 0f);
            });
            Enemy(library, "Hulk", e =>
            {
                e.Id = "enemy.hulk"; e.DisplayName = "Charred Hulk";
                e.MaxHealth = 60f; e.MoveSpeed = 2.4f; e.Radius = 0.55f; e.ContactDamage = 12f; e.XpValue = 6; e.Scale = 1.7f;
                e.Color = new Color(0.18f, 0.16f, 0.15f); e.Emission = new Color(0.12f, 0.03f, 0f); e.GoldChance = 0.6f;
            });
            Enemy(library, "Drowned", e =>
            {
                e.Id = "enemy.drowned"; e.DisplayName = "Drowned";
                e.MaxHealth = 16f; e.MoveSpeed = 3f; e.Radius = 0.42f; e.ContactDamage = 7f; e.XpValue = 2;
                e.Color = new Color(0.35f, 0.48f, 0.45f);
            });
            Enemy(library, "BrineRunner", e =>
            {
                e.Id = "enemy.brine_runner"; e.DisplayName = "Brine Runner";
                e.MaxHealth = 9f; e.MoveSpeed = 6f; e.Radius = 0.35f; e.ContactDamage = 5f; e.XpValue = 2; e.Scale = 0.85f;
                e.Color = new Color(0.5f, 0.6f, 0.62f);
            });
            Enemy(library, "ShellBrute", e =>
            {
                e.Id = "enemy.shell_brute"; e.DisplayName = "Shell Brute";
                e.MaxHealth = 75f; e.MoveSpeed = 2.2f; e.Radius = 0.6f; e.ContactDamage = 15f; e.XpValue = 8; e.Scale = 1.8f;
                e.Color = new Color(0.55f, 0.45f, 0.4f); e.GoldChance = 0.6f;
            });
            Enemy(library, "AshChampion", e =>
            {
                e.Id = "enemy.ash_champion"; e.DisplayName = "An Ash Champion";
                e.MaxHealth = 700f; e.MoveSpeed = 3.6f; e.Radius = 0.55f; e.ContactDamage = 16f; e.XpValue = 40; e.Scale = 2.4f;
                e.Color = new Color(0.2f, 0.17f, 0.16f); e.Emission = new Color(0.35f, 0.1f, 0.01f); e.IsElite = true; e.GoldChance = 1f;
            });
            Enemy(library, "TideChampion", e =>
            {
                e.Id = "enemy.tide_champion"; e.DisplayName = "A Tide Champion";
                e.MaxHealth = 800f; e.MoveSpeed = 3f; e.Radius = 0.6f; e.ContactDamage = 18f; e.XpValue = 50; e.Scale = 2.5f;
                e.Color = new Color(0.3f, 0.45f, 0.5f); e.Emission = new Color(0.05f, 0.25f, 0.35f); e.IsElite = true; e.GoldChance = 1f;
            });
        }

        static BossAttackDef Atk(BossAttack a, float weight, float cooldown, float damage, float telegraph, float size) =>
            new BossAttackDef { Attack = a, Weight = weight, Cooldown = cooldown, Damage = damage, Telegraph = telegraph, Size = size };

        /// <summary>The three stage biomes and the final arena, with their bosses. Created once.</summary>
        public static void Biomes(GameAssets assets, ContentLibrary library)
        {
            var warden = AssetDatabase.LoadAssetAtPath<BossDefinition>(BossesDir + "/StoneWarden.asset");
            var stag = LoadOrCreate<BossDefinition>(BossesDir + "/AshStag.asset", b =>
            {
                b.Id = "boss.ash_stag"; b.DisplayName = "The Ash Stag"; b.Epithet = "The forest burned, and it did not";
                b.MaxHealth = 3200f; b.MoveSpeed = 4.5f; b.Scale = 2.6f; b.Radius = 1.4f; b.ContactDamage = 16f; b.Rest = 1.1f;
                b.MinionId = "enemy.ashling"; b.Model = BossModel.Stag;
                b.Color = new Color(0.3f, 0.27f, 0.25f); b.Accent = new Color(2f, 0.6f, 0.15f);
                b.Attacks = new List<BossAttackDef>
                {
                    Atk(BossAttack.Charge, 3f, 4f, 24f, 0.8f, 22f), Atk(BossAttack.Volley, 2f, 6f, 20f, 1.2f, 3.5f),
                    Atk(BossAttack.Shockwave, 2f, 8f, 20f, 0.7f, 24f), Atk(BossAttack.Summon, 1f, 12f, 0f, 0.8f, 10f),
                };
            });
            var mother = LoadOrCreate<BossDefinition>(BossesDir + "/TideMother.asset", b =>
            {
                b.Id = "boss.tide_mother"; b.DisplayName = "The Tide Mother"; b.Epithet = "Everything the sea took, she kept";
                b.MaxHealth = 3400f; b.MoveSpeed = 2.2f; b.Scale = 3.4f; b.Radius = 1.7f; b.ContactDamage = 18f; b.Rest = 1.2f;
                b.MinionId = "enemy.drowned"; b.Model = BossModel.Mother;
                b.Color = new Color(0.35f, 0.5f, 0.52f); b.Accent = new Color(0.3f, 1.4f, 1.8f);
                b.Attacks = new List<BossAttackDef>
                {
                    Atk(BossAttack.Volley, 3f, 5f, 26f, 1.2f, 4f), Atk(BossAttack.Summon, 2f, 10f, 0f, 0.8f, 10f),
                    Atk(BossAttack.Slam, 2f, 5f, 30f, 1.2f, 5.5f), Atk(BossAttack.Shockwave, 1f, 10f, 24f, 0.8f, 26f),
                };
            });
            var construct = LoadOrCreate<BossDefinition>(BossesDir + "/LastTest.asset", b =>
            {
                b.Id = "boss.last_test"; b.DisplayName = "The Last Test"; b.Epithet = "It was made to keep the throne empty";
                b.MaxHealth = 4000f; b.MoveSpeed = 3f; b.Scale = 3.6f; b.Radius = 1.8f; b.ContactDamage = 20f; b.Rest = 0.9f;
                b.MinionId = "enemy.ghost"; b.Model = BossModel.Construct; b.EnrageAt = 0.5f;
                b.Color = new Color(0.82f, 0.8f, 0.76f); b.Accent = new Color(2.2f, 1.9f, 1.1f);
                b.Attacks = new List<BossAttackDef>
                {
                    Atk(BossAttack.Slam, 2f, 4f, 30f, 1f, 5f), Atk(BossAttack.Charge, 2f, 6f, 28f, 0.8f, 24f),
                    Atk(BossAttack.Shockwave, 2f, 7f, 26f, 0.7f, 28f), Atk(BossAttack.Volley, 2f, 6f, 26f, 1.1f, 4f),
                    Atk(BossAttack.Summon, 1f, 12f, 0f, 0.8f, 12f),
                };
            });
            foreach (var boss in new[] { warden, stag, mother, construct })
                if (boss != null && !library.Bosses.Contains(boss)) library.Bosses.Add(boss);

            var steppe = LoadOrCreate<BiomeDefinition>(BiomesDir + "/GreySteppe.asset", b =>
            {
                b.Id = "biome.grey_steppe"; b.DisplayName = "The Grey Steppe";
                b.Terrain = new TerrainProfile { HillHeight = 14f, HillScale = 70f, CliffStep = 3f, CliffAmount = 0.15f };
                b.Palette = new GroundPalette { Low = new Color(0.42f, 0.56f, 0.3f), High = new Color(0.6f, 0.64f, 0.4f), Cliff = new Color(0.55f, 0.52f, 0.47f), Rim = new Color(0.42f, 0.44f, 0.38f) };
                b.Timeline = DefaultTimelines.Standard("enemy.husk", "enemy.runner", "enemy.brute", "enemy.champion");
                b.Boss = warden;
                b.Sun = new Color(1f, 0.94f, 0.82f); b.SunIntensity = 1.35f;
                b.AmbientSky = new Color(0.6f, 0.74f, 0.9f); b.AmbientEquator = new Color(0.55f, 0.6f, 0.55f); b.AmbientGround = new Color(0.3f, 0.28f, 0.22f);
                b.Fog = new Color(0.66f, 0.8f, 0.9f); b.FogStart = 22f; b.FogEnd = 135f;
                b.RockCount = 110; b.TreeCount = 15; b.RockColor = new Color(0.58f, 0.56f, 0.54f);
            });
            var wood = LoadOrCreate<BiomeDefinition>(BiomesDir + "/AshWood.asset", b =>
            {
                b.Id = "biome.ash_wood"; b.DisplayName = "The Ash Wood";
                b.Terrain = new TerrainProfile { HillHeight = 22f, HillScale = 40f, CliffStep = 3.5f, CliffAmount = 0.4f };
                b.Palette = new GroundPalette { Low = new Color(0.36f, 0.33f, 0.33f), High = new Color(0.5f, 0.45f, 0.43f), Cliff = new Color(0.38f, 0.34f, 0.33f), Rim = new Color(0.25f, 0.19f, 0.18f) };
                b.Timeline = DefaultTimelines.Standard("enemy.cinder", "enemy.ashling", "enemy.hulk", "enemy.ash_champion");
                b.Boss = stag;
                b.Sun = new Color(1f, 0.66f, 0.45f); b.SunIntensity = 1.25f; b.SunEuler = new Vector3(25f, -60f, 0f);
                b.AmbientSky = new Color(0.62f, 0.46f, 0.56f); b.AmbientEquator = new Color(0.55f, 0.4f, 0.38f); b.AmbientGround = new Color(0.25f, 0.17f, 0.15f);
                b.Fog = new Color(0.56f, 0.42f, 0.5f); b.FogStart = 18f; b.FogEnd = 115f;
                b.RockCount = 40; b.RockColor = new Color(0.34f, 0.3f, 0.29f); b.TreeCount = 140; b.TreeColor = new Color(0.24f, 0.2f, 0.19f);
            });
            var coast = LoadOrCreate<BiomeDefinition>(BiomesDir + "/DrownedCoast.asset", b =>
            {
                b.Id = "biome.drowned_coast"; b.DisplayName = "The Drowned Coast";
                b.Terrain = new TerrainProfile { HillHeight = 16f, HillScale = 60f, CliffStep = 4f, CliffAmount = 0.45f, WaterLevel = 6.5f };
                b.Palette = new GroundPalette { Low = new Color(0.8f, 0.72f, 0.54f), High = new Color(0.88f, 0.82f, 0.66f), Cliff = new Color(0.44f, 0.5f, 0.54f), Rim = new Color(0.38f, 0.42f, 0.47f) };
                b.Timeline = DefaultTimelines.Standard("enemy.drowned", "enemy.brine_runner", "enemy.shell_brute", "enemy.tide_champion");
                b.Boss = mother;
                b.Sun = new Color(1f, 0.97f, 0.9f); b.SunIntensity = 1.3f; b.SunEuler = new Vector3(40f, 20f, 0f);
                b.AmbientSky = new Color(0.55f, 0.74f, 0.86f); b.AmbientEquator = new Color(0.5f, 0.62f, 0.64f); b.AmbientGround = new Color(0.26f, 0.3f, 0.3f);
                b.Fog = new Color(0.6f, 0.8f, 0.86f); b.FogStart = 25f; b.FogEnd = 150f;
                b.RockCount = 80; b.RockColor = new Color(0.45f, 0.47f, 0.5f); b.TreeCount = 10; b.TreeColor = new Color(0.3f, 0.4f, 0.32f);
                b.WaterColor = new Color(0.15f, 0.5f, 0.6f, 0.65f);
            });
            var arena = LoadOrCreate<BiomeDefinition>(BiomesDir + "/LastTestArena.asset", b =>
            {
                b.Id = "biome.last_test"; b.DisplayName = "The Last Test";
                b.Terrain = new TerrainProfile { Cells = 64, CellSize = 2f, HillHeight = 2f, HillScale = 40f, SpawnFlatRadius = 30f, RimHeight = 20f, RimWidth = 14f };
                b.Palette = new GroundPalette { Low = new Color(0.75f, 0.73f, 0.7f), High = new Color(0.8f, 0.78f, 0.74f), Cliff = new Color(0.6f, 0.58f, 0.55f), Rim = new Color(0.3f, 0.28f, 0.27f) };
                b.Timeline = DefaultTimelines.FinalArena();
                b.Boss = construct;
                b.Sun = new Color(1f, 0.85f, 0.6f); b.SunIntensity = 1.3f; b.SunEuler = new Vector3(20f, 160f, 0f);
                b.AmbientSky = new Color(0.6f, 0.55f, 0.5f); b.Fog = new Color(0.6f, 0.55f, 0.5f); b.FogStart = 50f; b.FogEnd = 160f;
                b.RockCount = 0; b.TreeCount = 0;
            });
            if (assets.Stages.Count == 0) assets.Stages.AddRange(new[] { steppe, wood, coast });
            if (assets.FinalArena == null) assets.FinalArena = arena;
        }

        public static BiomeDefinition GreyboxBiome()
        {
            var boss = LoadOrCreate<BossDefinition>(BossesDir + "/StoneWarden.asset", b =>
            {
                b.Id = "boss.stone_warden"; b.DisplayName = "The Stone Warden"; b.Epithet = "It remembers the first wall";
                b.MaxHealth = 3000f; b.MoveSpeed = 2.6f; b.Scale = 3.2f; b.Radius = 1.6f; b.ContactDamage = 10f; b.Rest = 1.3f;
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

        /// <summary>
        /// One-time changes to content that already exists, keyed by GameAssets.ContentVersion,
        /// so Inspector tuning after a migration is never overwritten.
        /// </summary>
        public static void Migrate(GameAssets assets, ContentLibrary l)
        {
            if (assets.ContentVersion < 1)
            {
                // Milestone 7: content that must be bought at the Shrine of Embers.
                var costs = new Dictionary<string, int>
                {
                    { "weapon.frost_shards", 100 }, { "weapon.bone_ring", 100 }, { "weapon.grave_pull", 150 },
                    { "weapon.ember_rain", 150 }, { "weapon.thunder_cloud", 200 },
                    { "passive.magnetism", 80 }, { "passive.fortune", 100 }, { "passive.multitude", 200 },
                    { "item.bloodstone", 150 }, { "item.quiver_of_dawn", 200 }, { "item.winged_crown", 250 },
                };
                foreach (var w in l.Weapons) if (w != null && costs.TryGetValue(w.Id, out int c)) { w.UnlockId = w.Id; w.UnlockCost = c; EditorUtility.SetDirty(w); }
                foreach (var p in l.Passives) if (p != null && costs.TryGetValue(p.Id, out int c)) { p.UnlockId = p.Id; p.UnlockCost = c; EditorUtility.SetDirty(p); }
                foreach (var i in l.Items) if (i != null && costs.TryGetValue(i.Id, out int c)) { i.UnlockId = i.Id; i.UnlockCost = c; EditorUtility.SetDirty(i); }
                assets.ContentVersion = 1;
                EditorUtility.SetDirty(assets);
            }
            if (assets.ContentVersion < 2)
            {
                // Milestone 9: each enemy gets its own model; biomes get their own prop kits.
                var models = new Dictionary<string, EnemyModel>
                {
                    { "enemy.husk", EnemyModel.Husk }, { "enemy.runner", EnemyModel.Runner }, { "enemy.brute", EnemyModel.Brute },
                    { "enemy.ghost", EnemyModel.Ghost }, { "enemy.ashling", EnemyModel.Ashling }, { "enemy.cinder", EnemyModel.Cinder },
                    { "enemy.hulk", EnemyModel.Hulk }, { "enemy.drowned", EnemyModel.Drowned }, { "enemy.brine_runner", EnemyModel.BrineRunner },
                    { "enemy.shell_brute", EnemyModel.ShellBrute }, { "enemy.champion", EnemyModel.Champion },
                    { "enemy.ash_champion", EnemyModel.AshChampion }, { "enemy.tide_champion", EnemyModel.TideChampion },
                };
                foreach (var e in l.Enemies)
                {
                    if (e == null || !models.TryGetValue(e.Id, out var model)) continue;
                    e.Model = model;
                    e.WalkSwing = model == EnemyModel.Ghost ? 0.04f : model == EnemyModel.Hulk || model == EnemyModel.ShellBrute ? 0.16f : 0.22f;
                    EditorUtility.SetDirty(e);
                }
                foreach (var b in assets.Stages)
                {
                    if (b == null) continue;
                    if (b.Id == "biome.grey_steppe") { b.RockModel = PropModel.StandingStone; b.TreeModel = PropModel.Pine; }
                    if (b.Id == "biome.ash_wood") { b.RockModel = PropModel.Boulder; b.TreeModel = PropModel.DeadTree; }
                    if (b.Id == "biome.drowned_coast") { b.RockModel = PropModel.SeaStack; b.TreeModel = PropModel.Driftwood; }
                    EditorUtility.SetDirty(b);
                }
                assets.ContentVersion = 2;
                EditorUtility.SetDirty(assets);
            }
            if (assets.ContentVersion < 3)
            {
                // Play-test 2026-10-09: gold was too rare to ever open a chest.
                foreach (var e in l.Enemies)
                {
                    if (e == null || e.IsElite || e.GoldChance <= 0f) continue;
                    e.GoldChance = e.Id == "enemy.brute" ? 0.5f : e.Id == "enemy.hulk" || e.Id == "enemy.shell_brute" ? 0.6f : 0.25f;
                    EditorUtility.SetDirty(e);
                }
                assets.ContentVersion = 3;
                EditorUtility.SetDirty(assets);
            }
            if (assets.ContentVersion < 4)
            {
                // Movement pass 2026-10-09: steadier air control; momentum, fall gravity and
                // short hops come from new fields with their defaults.
                if (assets.Motor.AirControl < 0.6f) assets.Motor.AirControl = 0.6f;
                assets.ContentVersion = 4;
                EditorUtility.SetDirty(assets);
            }
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
