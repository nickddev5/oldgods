using System.Collections;
using System.IO;
using NUnit.Framework;
using OldGods.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace OldGods.Tests.PlayMode
{
    /// <summary>Loads the real run scene and plays it the way the smoke test does.</summary>
    public class RunFlowProbe
    {
        [SetUp]
        public void SetUp()
        {
            SaveStore.FolderOverride = Path.Combine(Application.temporaryCachePath, "playmode-save");
            SaveStore.Forget();
            DamageNumbers.Enabled = true;
            RunSetup.GodId = null;
        }

        [TearDown]
        public void TearDown()
        {
            GameInput.MoveOverride = null;
            SaveStore.FolderOverride = null;
        }

        static IEnumerator WaitFor(System.Func<bool> condition, float seconds, string what)
        {
            float until = Time.realtimeSinceStartup + seconds;
            while (!condition())
            {
                if (Time.realtimeSinceStartup > until) Assert.Fail("Timed out waiting for " + what);
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator RunStartsSpawnsDiesAndRestarts()
        {
            SceneManager.LoadScene("Run");
            yield return WaitFor(() => RunController.Instance != null && RunController.Instance.Player != null, 20f, "run start");
            var first = RunController.Instance;
            Assert.IsNotNull(Ground.Field, "ground was generated");

            yield return WaitFor(() => first.Horde.AliveCount > 0, 10f, "horde spawn");

            // The player moves when input says so.
            Vector3 start = first.Player.transform.position;
            GameInput.MoveOverride = Vector2.up;
            yield return new WaitForSeconds(0.6f);
            GameInput.MoveOverride = null;
            Assert.Greater(Vector3.Distance(start, first.Player.transform.position), 1f, "player moved");

            first.PlayerHealth.Kill();
            yield return null;
            Assert.IsTrue(first.IsOver);

            first.Restart();
            yield return WaitFor(() => RunController.Instance != null && RunController.Instance != first && RunController.Instance.Player != null, 20f, "restart");
        }

        [UnityTest]
        public IEnumerator BossGateBossPortalAndNextStage()
        {
            LevelUpScreen.AutoPick = true;
            try
            {
                SceneManager.LoadScene("Run");
                yield return WaitFor(() => RunController.Instance != null && RunController.Instance.Combat != null, 20f, "run start");
                var run = RunController.Instance;
                run.PlayerHealth.Invincible = true;
                Assert.AreEqual(0, run.StageIndex);

                var gate = Object.FindAnyObjectByType<BossGate>();
                Assert.IsNotNull(gate, "the map has a boss gate");
                Assert.Greater(new Vector2(gate.transform.position.x, gate.transform.position.z).magnitude, 60f, "the gate is away from the start");

                gate.Use(run.Combat);
                yield return WaitFor(() => BossController.Active != null, 5f, "boss spawn");
                var boss = BossController.Active;
                Assert.Greater(boss.MaxHealth, 0f);
                yield return new WaitForSeconds(1.5f);

                // Weapons can hit the boss through its horde slot.
                run.Horde.Damage(boss.Slot, boss.MaxHealth * 2f);
                yield return WaitFor(() => run.BossDefeated, 5f, "boss defeat");
                var portal = Object.FindAnyObjectByType<NextPortal>();
                Assert.IsNotNull(portal, "the portal opened");

                portal.Use(run.Combat);
                yield return null;
                Assert.AreEqual(1, run.StageIndex, "moved to stage 2");
                Assert.IsNotNull(Object.FindAnyObjectByType<BossGate>(), "stage 2 has its own gate");
                yield return new WaitForSeconds(1f);
                Assert.Greater(run.Horde.AliveCount, 0, "stage 2 spawns enemies");
            }
            finally
            {
                LevelUpScreen.AutoPick = false;
            }
        }

        static IEnumerator KillBossesAndLeave(RunController run, bool viaGate)
        {
            if (viaGate)
            {
                var gate = Object.FindAnyObjectByType<BossGate>();
                Assert.IsNotNull(gate, $"stage {run.StageIndex + 1} has a gate");
                gate.Use(run.Combat);
            }
            yield return WaitFor(() => BossController.Active != null, 10f, "boss spawn");
            while (BossController.Active != null)
            {
                var b = BossController.Active;
                run.Horde.Damage(b.Slot, b.MaxHealth * 2f);
                yield return null;
            }
        }

        [UnityTest, Timeout(240000)]
        public IEnumerator FullRunAsEliasTakesTheThrone() => FullRun("god.elias", true);

        [UnityTest, Timeout(240000)]
        public IEnumerator FullRunAsAnotherGodIsRefused() => FullRun("god.storm", false);

        IEnumerator FullRun(string god, bool takesThrone)
        {
            RunSetup.GodId = god;
            SaveStore.Current.Unlock(god);
            LevelUpScreen.AutoPick = true;
            try
            {
                SceneManager.LoadScene("Run");
                yield return WaitFor(() => RunController.Instance != null && RunController.Instance.Combat != null, 20f, "run start");
                var run = RunController.Instance;
                run.PlayerHealth.Invincible = true;
                var biomes = new System.Collections.Generic.List<string>();
                for (int stage = 0; stage < run.StageCount; stage++)
                {
                    Assert.AreEqual(stage, run.StageIndex);
                    biomes.Add(run.Biome.Id);
                    yield return KillBossesAndLeave(run, true);
                    yield return WaitFor(() => run.BossDefeated, 5f, "boss defeat");
                    Object.FindAnyObjectByType<NextPortal>().Use(run.Combat);
                    yield return null;
                }
                Assert.AreEqual(3, new System.Collections.Generic.HashSet<string>(biomes).Count, "three different biomes");
                Assert.IsTrue(run.IsFinal, "reached The Last Test");
                Assert.IsNotNull(FinalArena.Instance);

                yield return KillBossesAndLeave(run, false);
                yield return WaitFor(() => run.IsOver, 60f, "ending");
                Assert.IsTrue(run.Won);
                Assert.AreEqual(takesThrone, OldGods.Rules.LastTest.TakesTheThrone(run.GodId));
                Assert.AreEqual(3, run.Summary.StagesCleared);
                Assert.AreEqual(4, run.Summary.BossesKilled);
                Assert.Greater(run.EmbersEarned, 0);
                Assert.IsNotNull(Object.FindAnyObjectByType<ResultsScreen>(), "results shown");
                if (takesThrone)
                    foreach (var statue in FinalArena.Instance.Statues) Assert.IsFalse(statue.gameObject.activeSelf, "the old gods are gone");
            }
            finally
            {
                LevelUpScreen.AutoPick = false;
                RunSetup.GodId = null;
            }
        }

        [UnityTest, Timeout(180000)]
        public IEnumerator EachGodStartsWithTheirKit()
        {
            var content = GameAssets.Load().Content.Load();
            Assert.AreEqual(7, content.Gods.Count, "seven gods");
            foreach (var g in content.Gods) SaveStore.Current.Unlock(OldGods.Rules.GodRules.UnlockId(g));
            var weapons = new System.Collections.Generic.HashSet<string>();
            foreach (var g in OldGods.Rules.GodRules.Ordered(content.Gods))
            {
                RunSetup.GodId = g.Id;
                SceneManager.LoadScene("Run");
                yield return null;
                yield return WaitFor(() => RunController.Instance != null && RunController.Instance.Combat != null && RunController.Instance.GodId == g.Id, 20f, "run as " + g.Id);
                var combat = RunController.Instance.Combat;
                Assert.AreEqual(g.StartingWeapon, combat.Loadout.Weapons[0].Def.Id, g.Id + " weapon");
                Assert.AreEqual(g.StartingPassive, combat.Loadout.Passives[0].Def.Id, g.Id + " passive");
                weapons.Add(g.StartingWeapon);
            }
            Assert.AreEqual(7, weapons.Count, "every god has a different weapon");
        }

        /// <summary>
        /// Milestone 7 gate: a fresh save earns Embers from a real run, the save survives a
        /// reload, and repeating that payout buys every god in order up to Elias.
        /// </summary>
        [UnityTest, Timeout(240000)]
        public IEnumerator FreshSaveEarnsSavesAndReachesElias()
        {
            SaveStore.Reset();
            LevelUpScreen.AutoPick = true;
            try
            {
                SceneManager.LoadScene("Run");
                yield return WaitFor(() => RunController.Instance != null && RunController.Instance.Combat != null, 20f, "run start");
                var run = RunController.Instance;
                run.PlayerHealth.Invincible = true;
                for (int stage = 0; stage < run.StageCount; stage++)
                {
                    yield return KillBossesAndLeave(run, true);
                    yield return WaitFor(() => run.BossDefeated, 5f, "boss defeat");
                    Object.FindAnyObjectByType<NextPortal>().Use(run.Combat);
                    yield return null;
                }
                yield return KillBossesAndLeave(run, false);
                yield return WaitFor(() => run.IsOver, 60f, "ending");
                int earned = run.EmbersEarned;
                Assert.Greater(earned, 0);

                SaveStore.Forget();
                var save = SaveStore.Load(out var result);
                Assert.AreEqual(SaveStore.LoadResult.Loaded, result, "the save was written and reads back");
                Assert.GreaterOrEqual(save.currency, earned);
                Assert.AreEqual(1, save.runsWon);
                Assert.IsTrue(save.lastRun.won);

                var content = GameAssets.Load().Content.Load();
                var tree = OldGods.Rules.MetaRules.Tree(content, GameAssets.Load().Content.UnlockCost);
                int runs = 1;
                while (!save.IsUnlocked(OldGods.Rules.LastTest.EliasId) && runs < 60)
                {
                    foreach (var e in tree)
                        if (e.Kind == OldGods.Rules.UnlockKind.God) OldGods.Rules.MetaRules.TryUnlock(save, e, content.Gods);
                    if (save.IsUnlocked(OldGods.Rules.LastTest.EliasId)) break;
                    OldGods.Rules.MetaRules.PayRun(save, run.Summary, earned);
                    runs++;
                }
                Assert.IsTrue(save.IsUnlocked(OldGods.Rules.LastTest.EliasId), $"Elias after {runs} winning runs");
                SaveStore.Save(save);

                MenuController.StartRun(OldGods.Rules.LastTest.EliasId);
                yield return WaitFor(() => RunController.Instance != null && RunController.Instance != run && RunController.Instance.Combat != null, 20f, "run as Elias");
                Assert.AreEqual(OldGods.Rules.LastTest.EliasId, RunController.Instance.GodId);
            }
            finally
            {
                LevelUpScreen.AutoPick = false;
            }
        }

        [UnityTest]
        public IEnumerator LoreStoneIsReadAndRemembered()
        {
            SaveStore.Reset();
            SceneManager.LoadScene("Run");
            yield return WaitFor(() => RunController.Instance != null && Object.FindAnyObjectByType<LoreStone>() != null, 20f, "a lore stone on the map");
            var run = RunController.Instance;
            var stone = Object.FindAnyObjectByType<LoreStone>();
            Assert.AreEqual(run.Biome.Id, stone.Fragment.BiomeId, "the stone belongs to this biome");
            stone.Use(run.Combat);
            Assert.IsTrue(ReadScreen.IsOpen);
            Assert.AreEqual(0f, Time.timeScale, "reading pauses the run");
            SaveStore.Forget();
            Assert.Contains(stone.Fragment.Id, SaveStore.Load(out _).lore, "saved");
            LevelUpScreen.AutoPick = true; // closes the page
            yield return null;
            yield return null;
            LevelUpScreen.AutoPick = false;
            Assert.IsFalse(ReadScreen.IsOpen);
            Assert.AreEqual(1f, Time.timeScale);
        }

        [UnityTest]
        public IEnumerator PauseStopsTheRunAndSettingsApply()
        {
            SaveStore.Reset();
            SceneManager.LoadScene("Run");
            yield return WaitFor(() => RunController.Instance != null && RunController.Instance.Combat != null, 20f, "run start");
            var run = RunController.Instance;
            var pause = Object.FindAnyObjectByType<PauseMenu>();
            Assert.IsNotNull(pause);
            pause.Open();
            Assert.IsTrue(pause.IsOpen);
            Assert.AreEqual(0f, Time.timeScale);
            yield return null; // the frame already under way still carries its delta time
            float clock = run.Director.Elapsed;
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.AreEqual(clock, run.Director.Elapsed, 1e-4f, $"the stage clock stops ({clock} then {run.Director.Elapsed}, dt {Time.deltaTime}, frame {Time.frameCount})");
            pause.Close();
            Assert.AreEqual(1f, Time.timeScale);

            var s = SaveStore.Current.settings;
            s.cameraSensitivity = 2.5f;
            s.invertY = true;
            SettingsPanel.Apply(s);
            Assert.AreEqual(2.5f, run.Camera.Sensitivity);
            Assert.IsTrue(run.Camera.InvertY);
        }

        [UnityTest]
        public IEnumerator CorruptSaveIsKeptAsideAndAFreshOneStarts()
        {
            SaveStore.Reset();
            System.IO.File.WriteAllText(SaveStore.SavePath, "{ this is not json");
            SaveStore.Forget();
            var save = SaveStore.Load(out var result);
            Assert.AreEqual(SaveStore.LoadResult.RecoveredFromCorrupt, result);
            Assert.AreEqual(0, save.currency);
            Assert.IsNotEmpty(System.IO.Directory.GetFiles(SaveStore.Folder, "*.corrupt"));
            yield return null;
        }

        [UnityTest]
        public IEnumerator MenuStartsARunWithTheChosenGod()
        {
            SceneManager.LoadScene("Menu");
            yield return WaitFor(() => MenuController.Instance != null, 10f, "menu");
            yield return null;
            MenuController.StartRun(null);
            yield return WaitFor(() => RunController.Instance != null && RunController.Instance.Combat != null, 20f, "run from menu");
            Assert.AreEqual("god.storm", RunController.Instance.GodId, "the first god by default");
        }

        [UnityTest]
        public IEnumerator EveryEconomyFeatureWorks()
        {
            LevelUpScreen.AutoPick = true;
            try
            {
                SceneManager.LoadScene("Run");
                yield return WaitFor(() => RunController.Instance != null && RunController.Instance.Economy != null && Object.FindAnyObjectByType<Chest>() != null, 20f, "run start");
                var run = RunController.Instance;
                var eco = run.Economy;
                run.PlayerHealth.Invincible = true;
                run.Director.Paused = true;

                // Gold pickups land in the wallet with Gold Gain applied.
                run.Pickups.Spawn(PickupKind.Gold, run.Player.transform.position, 500f);
                yield return WaitFor(() => eco.Wallet.Gold > 0, 5f, "gold pickup");
                eco.Wallet.Add(5000);
                int gold = eco.Wallet.Gold;

                var chest = Object.FindObjectsByType<Chest>(FindObjectsSortMode.None)[0];
                int price = eco.ChestPrice;
                chest.Use(run.Combat);
                Assert.AreEqual(gold - price, eco.Wallet.Gold, "chest cost its price");
                Assert.AreEqual(1, run.Combat.Items.Total, "chest gave an item");
                Assert.Greater(eco.ChestPrice, price, "next chest costs more");

                var merchant = Object.FindAnyObjectByType<Merchant>();
                Assert.IsNotNull(merchant);
                merchant.Use(run.Combat);
                Assert.AreEqual(2, run.Combat.Items.Total, "merchant sold an item");

                var dup = Object.FindAnyObjectByType<Duplicator>();
                Assert.IsNotNull(dup);
                dup.Use(run.Combat);
                Assert.AreEqual(3, run.Combat.Items.Total, "duplicator copied an item");

                var shrines = Object.FindObjectsByType<Shrine>(FindObjectsSortMode.None);
                var kinds = new System.Collections.Generic.HashSet<OldGods.Rules.ShrineKind>();
                foreach (var s in shrines) kinds.Add(s.Kind);
                Assert.AreEqual(6, kinds.Count, "every shrine kind is on the map");

                foreach (var s in shrines)
                    if (s.Kind != OldGods.Rules.ShrineKind.Charge && s.CanUse) s.Use(run.Combat);
                Assert.Greater(run.Director.DifficultyBonus, 0f, "greed raised difficulty");
                Assert.AreEqual(1, run.BossCurses, "curse taken");

                // Charge: stand in the ring until it fills.
                Shrine charge = null;
                foreach (var s in shrines) if (s.Kind == OldGods.Rules.ShrineKind.Charge) { charge = s; break; }
                run.Player.Teleport(charge.transform.position + Vector3.right * 2f + Vector3.up);
                int modsBefore = run.Combat.ExternalMods.Count;
                yield return WaitFor(() => run.Combat.ExternalMods.Count > modsBefore, 10f, "charge shrine boost");
                Assert.AreEqual(6, eco.ShrineKindsUsed.Count, "every shrine kind used");
            }
            finally
            {
                LevelUpScreen.AutoPick = false;
            }
        }

        [UnityTest]
        public IEnumerator FinalSwarmStartsWhenTheClockRunsOut()
        {
            SceneManager.LoadScene("Run");
            yield return WaitFor(() => RunController.Instance != null && RunController.Instance.Director != null && RunController.Instance.Director.Timeline != null, 20f, "run start");
            var run = RunController.Instance;
            run.PlayerHealth.Invincible = true;
            run.Director.DebugSkip(run.Director.Timeline.Duration - 0.5f);
            yield return WaitFor(() => run.Director.InFinalSwarm, 3f, "final swarm");
            yield return new WaitForSeconds(1f);
            Assert.Greater(run.Horde.AliveCount, 30, "ghosts pour in");
        }

        [UnityTest]
        public IEnumerator WeaponsKillDropXpAndTheDraftLevelsUp()
        {
            LevelUpScreen.AutoPick = true;
            try
            {
                SceneManager.LoadScene("Run");
                yield return WaitFor(() => RunController.Instance != null && RunController.Instance.Combat != null, 20f, "run start");
                var run = RunController.Instance;
                run.PlayerHealth.Invincible = true;
                Assert.AreEqual(1, run.Combat.Loadout.Weapons.Count, "starts with one weapon");

                yield return WaitFor(() => run.Kills > 0, 30f, "first kill");
                yield return WaitFor(() => run.Combat.Xp.Level >= 3, 60f, "two level-ups");
                yield return null;
                int progress = 0;
                foreach (var w in run.Combat.Loadout.Weapons) progress += w.Level;
                foreach (var p in run.Combat.Loadout.Passives) progress += p.Level;
                Assert.GreaterOrEqual(progress, 3, "each draft pick added or levelled something");
                Assert.AreEqual(0, run.Combat.PendingLevelUps);
                Assert.AreEqual(1f, Time.timeScale, "draft closed and the run resumed");
            }
            finally
            {
                LevelUpScreen.AutoPick = false;
            }
        }
    }
}
