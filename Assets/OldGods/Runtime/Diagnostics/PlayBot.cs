using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OldGods.Rules;
using UnityEngine;

namespace OldGods.Runtime
{
    /// <summary>
    /// Started by -playbot (or the older -autoplay, which stops after stage 1). Plays a whole run
    /// the way a careful player would: spends Embers and picks a god at the menu, explores,
    /// kites the horde, collects gems and gold, opens chests, uses shrines, wakes and fights each
    /// boss, dodges telegraphs, jumps and slides over the ground, and finishes The Last Test.
    /// Writes a PlayBotReport as JSON to -botOut and quits. Tools/playbot.py drives it.
    ///
    /// It runs faster than real time by stepping the game a fixed -botStep seconds per frame
    /// (default 1/30) without waiting for the clock, so the speed-up is the frame rate over 30.
    /// It never touches the real save: -botSave names a save folder, otherwise a fresh one is used.
    /// </summary>
    [DefaultExecutionOrder(-500)]
    public sealed class PlayBot : MonoBehaviour
    {
        enum Goal { Explore, Gem, Feature, Charge, Gate, Boss, Portal, Arena }

        // Options from the command line.
        string godId, outPath;
        BotPicks policy = BotPicks.Smart;
        float step = 1f / 30f, speed = 1f, bossAtFraction = 0.7f, bossAtSeconds = -1f, runLimit = 7200f;
        int stageLimit = int.MaxValue;
        bool unlockAll, spend = true, listOnly;

        readonly PlayBotReport report = new PlayBotReport();
        RunController run;
        Rng draftRng, moveRng;
        readonly int[] near = new int[64];
        readonly StuckWatch stuck = new StuckWatch();
        readonly List<float> frameMs = new List<float>(65536);
        double realStart, lastFrame;
        float simMsSum, nextLog = 30f;
        int simSamples;
        string lastSource = "";
        Vector3 lastPos;

        // The stage being played.
        PlayBotReport.Stage cur;
        int killsAtStart, chestsAtStart, shrinesAtStart, clearedAtStart;
        readonly List<Vector3> waypoints = new List<Vector3>();
        readonly HashSet<Interactable> skipped = new HashSet<Interactable>();
        readonly Dictionary<Interactable, int> stuckOn = new Dictionary<Interactable, int>();

        // What the bot is doing now.
        Goal goal = Goal.Explore;
        Interactable target;
        Vector3 targetPos;
        float goalSince, rethinkAt, atTargetSince = -1f;
        float jumpHeld, slideHeld, nextJumpAt, detourUntil;
        Vector3 detour;
        bool dodging, interacting;
        float orbitSide = 1f, turnSide;
        FlowField paths;
        Vector3 pathTarget = new Vector3(float.NaN, 0f, 0f);

        public static bool Requested => CommandLine.Has("-playbot") || CommandLine.Has("-autoplay");

        // SubsystemRegistration runs before any BeforeSceneLoad hook, some of which read the save.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Early()
        {
            if (!Requested) return;
            string folder = CommandLine.Value("-botSave");
            if (string.IsNullOrEmpty(folder))
            {
                folder = Path.Combine(Application.temporaryCachePath, "playbot-save");
                try { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
                catch (IOException e) { Debug.LogWarning(e.Message); }
            }
            SaveStore.FolderOverride = folder;
            SaveStore.Load(out _); // never keep a copy of the real save that something read first
            // Keep running when the window loses focus, so scripted runs do not stall.
            Application.runInBackground = true;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (!Requested || FindAnyObjectByType<PlayBot>() != null) return;
            var go = new GameObject("Play Bot");
            DontDestroyOnLoad(go);
            go.AddComponent<PlayBot>().ReadOptions();
        }

        static float Float(string flag, float fallback) =>
            float.TryParse(CommandLine.Value(flag), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float v) ? v : fallback;

        void ReadOptions()
        {
            godId = CommandLine.Value("-botGod");
            outPath = CommandLine.Value("-botOut") ?? CommandLine.Value("-autoplayOut");
            policy = PlayBotRules.ParsePicks(CommandLine.Value("-botPicks") ?? (CommandLine.Has("-autoplay") ? "first" : null));
            step = Float("-botStep", step);
            speed = Float("-botSpeed", Float("-speed", speed));
            bossAtFraction = Float("-botBossAt", bossAtFraction);
            bossAtSeconds = Float("-bossAt", -1f);
            runLimit = Float("-botLimit", runLimit);
            stageLimit = CommandLine.Has("-autoplay") ? 1 : (int)Float("-botStages", 99f);
            unlockAll = CommandLine.Has("-botUnlockAll");
            spend = !CommandLine.Has("-botNoSpend");
            listOnly = CommandLine.Has("-botList");
        }

        IEnumerator Start()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1;
            if (step > 0f) Time.captureDeltaTime = step;
            DamageNumbers.Enabled = false;
            ReadScreen.AutoClose = true;
            LevelUpScreen.Picker = PickCard;
            LevelUpScreen.Taken += OnTaken;
            ChoiceScreen.Picker = PickChoice;
            report.picks = policy.ToString().ToLowerInvariant();
            report.build = Application.version;

            // Let the menu build its pages first.
            yield return null;
            yield return null;
            var menu = MenuController.Instance;
            if (menu != null)
            {
                Catalogue(menu.Content);
                if (listOnly) { Finish(null, "listed the content", 0); yield break; }
                if (!Prepare(menu)) { Finish(null, report.note, 3); yield break; }
                menu.ShowSelect();
                yield return null;
                menu.Select(report.god);
                yield return null;
                menu.Play();
            }
            while (RunController.Instance == null || RunController.Instance.Combat == null || RunController.Instance.Director == null || RunController.Instance.Director.Timeline == null)
                yield return null;

            run = RunController.Instance;
            if (string.IsNullOrEmpty(report.god)) { report.god = run.GodId ?? ""; report.godName = run.God != null ? run.God.Name : ""; }
            if (report.content.gods.Count == 0) Catalogue(run.Content);
            report.seed = run.Seed.ToString();
            draftRng = run.Seed.Stream("playbot.draft");
            moveRng = run.Seed.Stream("playbot.move");
            run.PlayerHealth.Hurt += OnHurt;
            run.Player.Rescued += OnRescued;
            run.StageStarted += OnStageStarted;
            BeginStage(run.StageIndex);
            realStart = lastFrame = Time.realtimeSinceStartupAsDouble;
            lastPos = run.Player.transform.position;
            playing = true;
        }

        bool playing;

        /// <summary>
        /// Plays one frame. Runs before every other script (see the execution order above), so a
        /// scripted jump press lands on the same frame the motor reads it.
        /// </summary>
        void Update()
        {
            if (!playing) return;
            if (run == null) { playing = false; Finish("error", "the run ended unexpectedly", 1); return; }
            if (run.IsOver) { playing = false; Finish(run.Won ? "won" : "died", "", 0); return; }
            if (Time.timeScale > 0f && Time.timeScale != speed) Time.timeScale = speed;
            Measure();
            string stop = run.Elapsed > runLimit ? "the run time limit passed"
                : run.Director.Elapsed > StageTimeLimit() ? $"stuck in stage {run.StageIndex + 1}" : null;
            if (stop != null) { playing = false; Finish("timeout", stop, 0); return; }
            if (run.StageIndex >= stageLimit) { playing = false; Finish("stage limit", $"stopped after {stageLimit} stage(s)", 0); return; }
            if (Time.deltaTime > 0f) Think();
        }

        float StageTimeLimit() => run.IsFinal ? 900f : run.Director.Timeline.Duration + 240f;

        // ---------- Menu ----------

        void Catalogue(ContentSet content)
        {
            var save = SaveStore.Current;
            var c = report.content;
            c.gods.Clear();
            c.weapons.Clear();
            c.passives.Clear();
            foreach (var g in GodRules.Ordered(content.Gods))
                c.gods.Add(new PlayBotReport.Named { id = g.Id, name = g.Name, weapon = g.StartingWeapon ?? "", unlocked = GodRules.IsUnlocked(g, save.IsUnlocked), cost = g.Cost });
            foreach (var w in content.Weapons)
                c.weapons.Add(new PlayBotReport.Named { id = w.Id, name = w.Name, unlocked = string.IsNullOrEmpty(w.UnlockId) || save.IsUnlocked(w.UnlockId) });
            foreach (var p in content.Passives)
                c.passives.Add(new PlayBotReport.Named { id = p.Id, name = p.Name, unlocked = string.IsNullOrEmpty(p.UnlockId) || save.IsUnlocked(p.UnlockId) });
        }

        /// <summary>Spends Embers (or unlocks everything), then checks the chosen god can be played.</summary>
        bool Prepare(MenuController menu)
        {
            var save = SaveStore.Current;
            var content = menu.Content;
            report.embersBefore = save.currency;
            var tree = MetaRules.Tree(content, menu.Assets.Content.UnlockCost);
            if (unlockAll)
            {
                foreach (var e in tree) save.Unlock(e.Id);
            }
            else if (spend)
            {
                foreach (var p in PlayBotRules.Spend(save, tree, content.Gods)) report.purchases.Add($"{p.Name} ({p.Cost})");
            }
            try { SaveStore.Save(save); }
            catch (System.Exception e) { Debug.LogWarning(e.Message); }
            Catalogue(content);

            // "newest" plays the latest god the save has unlocked, for campaigns.
            var god = string.IsNullOrEmpty(godId) ? GodRules.Default(content.Gods, save.IsUnlocked)
                : godId == "newest" ? GodRules.Ordered(content.Gods).LastOrDefault(g => GodRules.IsUnlocked(g, save.IsUnlocked))
                : content.God(godId);
            if (god == null)
            {
                report.outcome = "error";
                report.note = $"no god with id {godId}";
                return false;
            }
            report.god = god.Id;
            report.godName = god.Name;
            if (!GodRules.IsUnlocked(god, save.IsUnlocked))
            {
                report.outcome = "locked";
                report.note = $"{god.Name} is locked in this save";
                return false;
            }
            return true;
        }

        // ---------- Stages ----------

        void OnStageStarted(int stage)
        {
            EndStage();
            BeginStage(stage);
        }

        void BeginStage(int stage)
        {
            cur = new PlayBotReport.Stage
            {
                index = stage,
                final = run.IsFinal,
                biome = run.Biome != null ? run.Biome.Id : "",
                biomeName = run.Biome != null ? run.Biome.DisplayName : "",
                boss = run.Biome != null && run.Biome.Boss != null ? run.Biome.Boss.DisplayName : "",
                startedAt = run.Elapsed,
            };
            report.stages.Add(cur);
            killsAtStart = run.Kills;
            chestsAtStart = run.Economy.ChestsOpened;
            shrinesAtStart = run.Economy.ShrinesUsed;
            clearedAtStart = run.Summary.StagesCleared;
            skipped.Clear();
            stuckOn.Clear();
            stuck.Reset();
            paths = null;
            pathTarget = new Vector3(float.NaN, 0f, 0f);
            SetGoal(Goal.Explore, null, Vector3.zero);
            BuildWaypoints();
            Line($"stage {stage + 1}: {cur.biomeName}");
        }

        void EndStage()
        {
            if (cur == null || run == null) return;
            cur.seconds = run.Elapsed - cur.startedAt;
            cur.levelAtEnd = run.Combat.Xp.Level;
            cur.kills = run.Kills - killsAtStart;
            cur.chestsOpened = run.Economy.ChestsOpened - chestsAtStart;
            cur.shrinesUsed = run.Economy.ShrinesUsed - shrinesAtStart;
            cur.goldAtEnd = run.Economy.Wallet.Gold;
            cur.cleared = cur.final ? run.Won : run.Summary.StagesCleared > clearedAtStart;
            cur = null;
        }

        /// <summary>A grid of points to walk through when there is nothing better to do; walking finds chests and shrines.</summary>
        void BuildWaypoints()
        {
            waypoints.Clear();
            var f = Ground.Field;
            if (f == null || run.IsFinal) return;
            float m = Ground.RimWidth + 14f;
            for (int i = 0; i < 5; i++)
                for (int j = 0; j < 5; j++)
                {
                    float x = Mathf.Lerp(f.MinX + m, f.MaxX - m, i / 4f);
                    float z = Mathf.Lerp(f.MinZ + m, f.MaxZ - m, j / 4f);
                    if (Ground.Flow != null && Ground.Flow.ClassAt(x, z) == CellClass.Blocked) continue;
                    waypoints.Add(new Vector3(x, 0f, z));
                }
        }

        // ---------- Choices ----------

        int PickCard(IReadOnlyList<DraftOption> options)
        {
            if (run == null || draftRng == null) return 0;
            var h = run.PlayerHealth.Health;
            return PlayBotRules.PickDraft(options, run.Combat.Loadout, h.Current / Mathf.Max(1f, h.Max), policy, draftRng);
        }

        void OnTaken(IReadOnlyList<DraftOption> dealt, DraftOption o)
        {
            if (run == null) return;
            var pick = new PlayBotReport.Pick
            {
                t = run.Elapsed, stage = run.StageIndex, level = run.Combat.Xp.Level,
                taken = o.Id ?? "", kind = o.Kind.ToString(), name = o.Title ?? "", rarity = o.Rarity.ToString(),
            };
            foreach (var d in dealt) pick.offered.Add($"{d.Kind} {d.Id}");
            report.draft.Add(pick);
        }

        /// <summary>Shrine offers: the rarest option.</summary>
        static int PickChoice(IList<ChoiceScreen.Option> options)
        {
            int best = 0;
            for (int i = 1; i < options.Count; i++)
                if (options[i].Rarity > options[best].Rarity) best = i;
            return best;
        }

        // ---------- Damage, falls and measurements ----------

        void OnHurt(float dealt, Vector3 from, string source)
        {
            string name = source ?? Attribute(from, out _);
            bool boss = source == null && BossController.All.Count > 0 && name.StartsWith(BossName());
            var s = report.SourceFor(name);
            s.amount += dealt;
            s.hits++;
            lastSource = name;
            if (cur == null) return;
            cur.damageTaken += dealt;
            if (boss) cur.bossDamageTaken += dealt;
        }

        string BossName() => BossController.Active != null ? BossController.Active.DisplayName : "\u0000";

        /// <summary>Names what hit the player at a point: a boss and its attack, or the enemy standing there.</summary>
        string Attribute(Vector3 from, out bool isBoss)
        {
            isBoss = false;
            foreach (var b in BossController.All)
            {
                if (b == null) continue;
                Vector3 d = b.transform.position - from;
                d.y = 0f;
                bool onMark = false;
                foreach (var m in b.Marks)
                    if (Flat(m - from).sqrMagnitude < 0.01f) onMark = true;
                if (d.sqrMagnitude > 0.01f && !onMark) continue;
                isBoss = true;
                var a = b.Current;
                bool attack = onMark || (a.HasValue && (a.Value.Attack == BossAttack.Charge || a.Value.Attack == BossAttack.Shockwave));
                return b.DisplayName + ": " + (attack && a.HasValue ? a.Value.Attack.ToString() : "contact");
            }
            int n = run.Horde.QueryCircle(from, 0.4f, near);
            int best = -1;
            float bestD = float.MaxValue;
            for (int k = 0; k < n; k++)
            {
                float d = Flat(run.Horde.Position(near[k]) - from).sqrMagnitude;
                if (d < bestD) { bestD = d; best = near[k]; }
            }
            if (best >= 0)
            {
                var def = run.Horde.Def(best);
                return string.IsNullOrEmpty(def.DisplayName) ? def.Id : def.DisplayName;
            }
            return "unknown";
        }

        void OnRescued(Vector3 at)
        {
            report.fellOut.Add(Spot(at, goal.ToString()));
            if (cur != null) cur.fellOut++;
            Line($"fell out of the world at ({at.x:0}, {at.z:0})");
        }

        PlayBotReport.Spot Spot(Vector3 at, string doing) =>
            new PlayBotReport.Spot { stage = run.StageIndex, t = run.Director.Elapsed, x = at.x, z = at.z, doing = doing };

        void Measure()
        {
            double now = Time.realtimeSinceStartupAsDouble;
            float ms = (float)((now - lastFrame) * 1000.0);
            lastFrame = now;
            if (ms > 0f && ms < 5000f) frameMs.Add(ms);
            int alive = run.Horde.AliveCount;
            report.perf.peakAlive = Mathf.Max(report.perf.peakAlive, alive);
            if (cur != null) cur.peakAlive = Mathf.Max(cur.peakAlive, alive);
            float sim = run.Horde.LastSimulateMs;
            simMsSum += sim;
            simSamples++;
            report.perf.simMsMax = Mathf.Max(report.perf.simMsMax, sim);
            var p = run.Player.transform.position;
            float moved = Flat(p - lastPos).magnitude;
            if (moved < 5f) report.moves.metres += moved;
            lastPos = p;
            if (cur != null)
            {
                var h = run.PlayerHealth.Health;
                cur.lowestHealth = Mathf.Min(cur.lowestHealth, h.Current / Mathf.Max(1f, h.Max));
                if (!run.IsFinal && run.Director.InFinalSwarm) cur.reachedSwarm = true;
                if (BossController.Active != null && cur.bossWokeAt < 0f) cur.bossWokeAt = run.Director.Elapsed;
                if (run.BossDefeated && cur.bossWokeAt >= 0f && cur.bossSeconds < 0f) cur.bossSeconds = run.Director.Elapsed - cur.bossWokeAt;
            }
            if (run.Elapsed >= nextLog)
            {
                nextLog += 30f;
                Line(goal.ToString().ToLowerInvariant());
            }
        }

        // ---------- Playing ----------

        void Think()
        {
            var p = run.Player.transform.position;
            float dt = Time.deltaTime;
            var h = run.PlayerHealth.Health;
            float hp = h.Current / Mathf.Max(1f, h.Max);
            float now = run.Elapsed;

            if (now >= rethinkAt || GoalDone())
            {
                rethinkAt = now + 0.4f;
                ChooseGoal(p, hp);
            }

            Vector3 avoid = Avoid(p, out int close);
            Vector3 dodge = Dodge(p, out bool jumpForWave);
            Vector3 seek = Vector3.zero;
            interacting = false;
            bool hold = false;
            float avoidWeight = 1f;

            switch (goal)
            {
                case Goal.Boss:
                case Goal.Arena:
                    seek = FightBoss(p);
                    avoidWeight = 0.7f;
                    break;
                case Goal.Charge:
                {
                    Vector3 to = Flat(targetPos - p);
                    float r = Shrine.ChargeRadius;
                    // Inside the ring: drift with the horde but stay in; outside: walk in.
                    seek = to.magnitude > r * 0.55f ? PathTo(p, targetPos) * (to.magnitude > r * 0.8f ? 1.5f : 0.6f) : Vector3.zero;
                    avoidWeight = to.magnitude < r * 0.7f ? 0.35f : 1f;
                    break;
                }
                case Goal.Feature:
                case Goal.Gate:
                case Goal.Portal:
                {
                    float range = target != null ? target.Range : 3f;
                    float d = Flat(targetPos - p).magnitude;
                    if (d < range * 0.75f)
                    {
                        hold = true;
                        interacting = true;
                        avoidWeight = 0.25f;
                        if (atTargetSince < 0f) atTargetSince = now;
                        // A feature that does not respond is skipped rather than waited on forever.
                        if (now - atTargetSince > 4f && target != null) { skipped.Add(target); rethinkAt = 0f; }
                    }
                    else
                    {
                        atTargetSince = -1f;
                        seek = PathTo(p, targetPos) * 1.6f;
                        avoidWeight = goal == Goal.Feature ? 0.8f : 0.5f;
                    }
                    break;
                }
                case Goal.Gem:
                case Goal.Explore:
                    seek = PathTo(p, targetPos) * (goal == Goal.Gem ? 1.2f : 1f);
                    break;
            }

            // Hurt, the bot puts staying alive ahead of whatever it was walking to.
            float danger = Mathf.Clamp01((0.6f - hp) / 0.4f);
            if (!interacting) seek *= 1f - 0.5f * danger;
            avoidWeight *= 1f + 1.5f * danger;
            Vector3 dir = seek + avoid * avoidWeight + dodge;
            if (now < detourUntil) dir = detour * 2f + avoid * 0.5f;
            dir = StayInside(p, dir);
            // Standing in blocked ground (the rim, a cliff top) every probe looks blocked, so just walk out.
            bool inBlocked = Ground.Flow != null && Ground.Flow.ClassAt(p.x, p.z) == CellClass.Blocked;
            if (!interacting && !inBlocked && Flat(targetPos - p).magnitude > 4f) dir = AroundDressing(p, dir);

            // Stuck checks: a stall tries a jump, a long one is reported and walked round.
            bool wantsMove = dir.sqrMagnitude > 0.09f && !interacting && goal != Goal.Charge;
            stuck.Step(p.x, p.z, wantsMove, dt);
            if (stuck.JustStalled) Jump(now);
            if (stuck.FirstStuck)
            {
                report.stuck.Add(Spot(p, goal.ToString()));
                if (cur != null) cur.stuck++;
            }
            if (stuck.JustStuck)
            {
                if (target != null)
                {
                    stuckOn.TryGetValue(target, out int times);
                    stuckOn[target] = ++times;
                    if (times >= 2) skipped.Add(target);
                }
                float a = moveRng.Range(0f, Mathf.PI * 2f);
                detour = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                detourUntil = now + 1.5f;
                Jump(now);
                rethinkAt = 0f;
            }

            // Jump over a shockwave, and up steps the motor cannot walk.
            if (jumpForWave) Jump(now, true);
            if (dir.sqrMagnitude > 0.01f && run.Player.Grounded)
            {
                Vector3 d = Flat(dir).normalized;
                float rise = Ground.Height(p.x + d.x * 1.6f, p.z + d.z * 1.6f) - Ground.Height(p.x, p.z);
                if (rise > 0.7f && rise < 3.5f) Jump(now);
            }

            // Slide out of a crowd, and down long slopes on the way somewhere.
            if (slideHeld <= 0f && run.Player.Grounded && !run.Player.IsSliding && run.Player.HorizontalSpeed > 2.5f && !interacting)
            {
                bool crowd = close >= 3;
                bool downhill = false;
                if (goal == Goal.Gate || goal == Goal.Portal || goal == Goal.Explore || goal == Goal.Feature)
                {
                    var n = Ground.Normal(p.x, p.z);
                    Vector3 d = Flat(dir).normalized;
                    downhill = Flat(targetPos - p).magnitude > 20f && n.x * d.x + n.z * d.z > 0.18f;
                }
                if (crowd || downhill)
                {
                    slideHeld = crowd ? 0.5f : 1.2f;
                    report.moves.slides++;
                }
            }

            jumpHeld -= dt;
            slideHeld -= dt;
            GameInput.Script(GameInput.Jump, jumpHeld > 0f);
            GameInput.Script(GameInput.Slide, slideHeld > 0f);
            GameInput.Script(GameInput.Interact, hold);
            if (hold && !wasHolding) report.moves.interactions++;
            wasHolding = hold;

            // Convert the world direction into camera-relative input.
            var cam = run.Player.ViewYaw;
            Vector3 f = cam != null ? cam.forward : Vector3.forward;
            f.y = 0f;
            f.Normalize();
            Vector3 right = new Vector3(f.z, 0f, -f.x);
            Vector3 want = Vector3.ClampMagnitude(dir, 1f);
            GameInput.MoveOverride = new Vector2(Vector3.Dot(want, right), Vector3.Dot(want, f));
        }

        bool wasHolding;

        void Jump(float now, bool urgent = false)
        {
            if (!urgent && now < nextJumpAt) return;
            if (jumpHeld > 0f) return;
            jumpHeld = 0.35f;
            nextJumpAt = now + 0.8f;
            report.moves.jumps++;
        }

        void SetGoal(Goal g, Interactable it, Vector3 at)
        {
            if (g != goal || it != target) { goalSince = run != null ? run.Elapsed : 0f; atTargetSince = -1f; }
            goal = g;
            target = it;
            targetPos = at;
        }

        /// <summary>True when the current goal has been met, so the bot rethinks at once.</summary>
        bool GoalDone()
        {
            switch (goal)
            {
                case Goal.Feature:
                case Goal.Gate:
                    return target == null || !target.isActiveAndEnabled || !Wanted(target);
                case Goal.Charge:
                    return target == null || target.MapLabel == null;
                default:
                    return false;
            }
        }

        void ChooseGoal(Vector3 p, float hp)
        {
            float now = run.Elapsed;
            // Anything chased for too long is given up on.
            if (target != null && (goal == Goal.Feature || goal == Goal.Charge) && now - goalSince > 45f) skipped.Add(target);

            if (run.IsFinal)
            {
                SetGoal(Goal.Arena, null, BossController.Active != null ? BossController.Active.transform.position : Vector3.zero);
                return;
            }
            if (BossController.Active != null)
            {
                SetGoal(Goal.Boss, null, BossController.Active.transform.position);
                return;
            }
            if (run.BossDefeated)
            {
                // The guardian's free chest, then onward.
                var chest = Best(p, hp, 45f, freeChestsOnly: true);
                if (chest != null) { SetGoal(Goal.Feature, chest, chest.transform.position); return; }
                var portal = FindAnyObjectByType<NextPortal>();
                if (portal != null) { SetGoal(Goal.Portal, portal, portal.transform.position); return; }
            }

            float stageT = run.Director.Elapsed;
            bool bossTime = bossAtSeconds > 0f ? stageT >= bossAtSeconds : stageT >= run.Director.Timeline.Duration * bossAtFraction;
            // Hurt, it gathers itself first, unless the final swarm is close.
            if (hp < 0.5f && stageT < run.Director.Timeline.Duration * 0.92f) bossTime = false;
            if (bossTime && !run.BossDefeated)
            {
                var gate = FindAnyObjectByType<BossGate>();
                if (gate != null && gate.CanUse) { SetGoal(Goal.Gate, gate, gate.transform.position); return; }
            }

            var best = hp > 0.3f ? Best(p, hp, 90f, false) : null;
            if (best != null)
            {
                bool charge = best is Shrine s && s.Kind == ShrineKind.Charge;
                SetGoal(charge ? Goal.Charge : Goal.Feature, best, best.transform.position);
                return;
            }

            bool threatened = run.Horde.QueryCircle(p, 4f, near) > 0;
            if (!threatened && run.Pickups.Nearest(p, 18f, out var gem))
            {
                SetGoal(Goal.Gem, null, gem);
                return;
            }
            SetGoal(Goal.Explore, null, NextWaypoint(p));
        }

        Vector3 NextWaypoint(Vector3 p)
        {
            if (waypoints.Count == 0) BuildWaypoints();
            if (waypoints.Count == 0) return Vector3.zero;
            // Reached, or chased for a minute: on to the nearest other one.
            for (int i = waypoints.Count - 1; i >= 0; i--)
                if (Flat(waypoints[i] - p).magnitude < 12f || (goal == Goal.Explore && targetPos == waypoints[i] && run.Elapsed - goalSince > 60f))
                    waypoints.RemoveAt(i);
            if (waypoints.Count == 0) BuildWaypoints();
            if (goal == Goal.Explore && waypoints.Contains(targetPos)) return targetPos;
            return waypoints.OrderBy(w => Flat(w - p).sqrMagnitude).First();
        }

        /// <summary>Whether the bot still wants to use this feature, before distance is weighed.</summary>
        bool Wanted(Interactable it) => Value(it, run.PlayerHealth.Health.Current / Mathf.Max(1f, run.PlayerHealth.Health.Max)) > 0f;

        float Value(Interactable it, float hp)
        {
            switch (it)
            {
                case Chest c: return c.CanUse ? (c.Free ? 6f : 4f) : 0f;
                case Merchant m: return m.CanUse ? 2.5f : 0f;
                case Duplicator d: return d.CanUse ? 2f : 0f;
                case LoreStone l: return l.CanUse ? 1f : 0f;
                case Shrine s:
                    if (s.MapLabel == null) return 0f; // used
                    switch (s.Kind)
                    {
                        case ShrineKind.Charge: return run.Horde.QueryCircle(s.transform.position, 6f, near) < 6 ? 3.5f : 0f;
                        case ShrineKind.Item: return s.CanUse && hp > 0.85f ? 2.5f : 0f;
                        case ShrineKind.Magnet: return s.CanUse && run.Pickups.Count > 40 ? 1.5f : 0f;
                        default: return 0f; // Greed, the Curse and Challenge are left alone
                    }
                default: return 0f;
            }
        }

        Interactable Best(Vector3 p, float hp, float maxDistance, bool freeChestsOnly)
        {
            Interactable best = null;
            float bestScore = 0f;
            foreach (var it in Interactable.All)
            {
                if (it == null || !it.Discovered || skipped.Contains(it)) continue;
                if (freeChestsOnly && !(it is Chest c && c.Free)) continue;
                float d = Flat(it.transform.position - p).magnitude;
                if (d > maxDistance) continue;
                // Out on the rim, past where the player can walk: not worth getting wedged for.
                Vector3 at = it.transform.position;
                if (Flat(Ground.ClampToPlayable(at, 1f) - at).sqrMagnitude > 1f) continue;
                float v = Value(it, hp);
                if (v <= 0f) continue;
                float score = v / (d + 15f);
                if (score > bestScore) { bestScore = score; best = it; }
            }
            return best;
        }

        /// <summary>Kite away from nearby enemies, harder the closer they are.</summary>
        Vector3 Avoid(Vector3 p, out int close)
        {
            Vector3 dir = Vector3.zero;
            close = 0;
            int n = run.Horde.QueryCircle(p, 10f, near);
            for (int k = 0; k < n; k++)
            {
                if (run.Horde.Hidden[near[k]]) continue; // a boss's own slot
                Vector3 away = Flat(p - run.Horde.Position(near[k]));
                float d = Mathf.Max(0.5f, away.magnitude);
                if (d < 3f) close++;
                // Champions and elites hit much harder, so they push much harder.
                dir += away / (d * d) * (run.Horde.Def(near[k]).IsElite ? 4f : 1f);
            }
            // Pushes harder the more enemies are in touching distance.
            return dir.sqrMagnitude > 0f ? dir.normalized * Mathf.Min(3f, 1.2f + close * 0.4f) : Vector3.zero;
        }

        /// <summary>Steps out of telegraphed circles and charge lines; asks for a jump over shockwaves.</summary>
        Vector3 Dodge(Vector3 p, out bool jump)
        {
            jump = false;
            Vector3 push = Vector3.zero;
            foreach (var b in BossController.All)
            {
                if (b == null || !b.Current.HasValue) continue;
                var a = b.Current.Value;
                Vector3 bp = b.transform.position;
                switch (a.Attack)
                {
                    case BossAttack.Slam:
                    case BossAttack.Volley:
                        foreach (var m in b.Marks)
                        {
                            Vector3 d = Flat(p - m);
                            float clear = a.Size + 1.5f;
                            if (d.magnitude >= clear) continue;
                            Vector3 outward = d.sqrMagnitude > 0.01f ? d.normalized : Vector3.Cross(Vector3.up, Flat(bp - p)).normalized;
                            push += outward * (clear - d.magnitude) * 1.5f;
                        }
                        break;
                    case BossAttack.Charge:
                    {
                        Vector3 cd = b.ChargeDir;
                        Vector3 rel = Flat(p - bp);
                        float along = Vector3.Dot(rel, cd);
                        if (along < -2f || along > a.Size + 3f) break;
                        Vector3 side = rel - cd * along;
                        float width = b.Def.Radius + 2.5f;
                        if (side.magnitude >= width) break;
                        Vector3 outward = side.sqrMagnitude > 0.01f ? side.normalized : Vector3.Cross(Vector3.up, cd);
                        push += outward * (width - side.magnitude) * 2f;
                        break;
                    }
                    case BossAttack.Shockwave:
                    {
                        float dist = Flat(p - bp).magnitude;
                        float r = b.ShockRadius;
                        if (r >= 0f && r < dist && dist - r < 3f && dist < a.Size + 1f) jump = true;
                        break;
                    }
                }
            }
            bool now = push.sqrMagnitude > 0.01f;
            if (now && !dodging) report.moves.dodges++;
            dodging = now;
            return push;
        }

        /// <summary>Circles a boss at the reach of the bot's weapons, closer for short-range kits.</summary>
        Vector3 FightBoss(Vector3 p)
        {
            var boss = BossController.Active;
            if (boss == null) return PathTo(p, run.IsFinal ? Vector3.zero : targetPos) * 0.3f;
            Vector3 to = Flat(boss.transform.position - p);
            float d = to.magnitude;
            float reach = 0f;
            foreach (var w in run.Combat.Loadout.Weapons) reach = Mathf.Max(reach, w.Def.Base.Range);
            float want = Mathf.Clamp(reach * 0.75f, boss.Def.Radius + 2.5f, 12f);
            Vector3 radial = d > 0.01f ? to / d * Mathf.Clamp((d - want) * 0.4f, -1.5f, 1.5f) : Vector3.zero;
            Vector3 tangent = d > 0.01f ? Vector3.Cross(Vector3.up, to / d) * orbitSide : Vector3.zero;
            // Switch direction now and then so the bot does not run into the same wall.
            if (moveRng.Chance(Time.deltaTime * 0.08f)) orbitSide = -orbitSide;
            if (d > 25f) return PathTo(p, boss.transform.position) * 1.3f;
            return radial + tangent * 0.9f;
        }

        /// <summary>The way to a point round walls and cliffs, as a player who knows the map would walk it.</summary>
        Vector3 PathTo(Vector3 p, Vector3 to)
        {
            Vector3 straight = Flat(to - p);
            if (straight.sqrMagnitude < 0.01f) return Vector3.zero;
            if (Ground.Field == null || straight.magnitude < 6f) return straight.normalized;
            if (paths == null || paths.Cells != Ground.Field.Cells || Flat(pathTarget - to).sqrMagnitude > 4f * 4f || float.IsNaN(pathTarget.x))
            {
                if (paths == null || paths.Cells != Ground.Field.Cells) paths = FlowField.FromTerrain(Ground.Field, Ground.Obstacles, Ground.RimWidth);
                paths.Solve(to.x, to.z);
                pathTarget = to;
            }
            return paths.Direction(p.x, p.z, out float dx, out float dz) ? new Vector3(dx, 0f, dz) : straight.normalized;
        }

        static Vector3 StayInside(Vector3 p, Vector3 dir)
        {
            var inside = Ground.ClampToPlayable(p, 6f);
            Vector3 back = Flat(inside - p);
            return back.sqrMagnitude > 0.01f ? dir + back.normalized * 1.5f : dir;
        }

        /// <summary>
        /// Turns the wanted direction aside when walls, columns or a cliff lie just ahead, trying
        /// small turns before large ones and keeping to one side for a while so it does not dither.
        /// </summary>
        Vector3 AroundDressing(Vector3 p, Vector3 dir)
        {
            float m = dir.magnitude;
            if (m < 1e-3f) return dir;
            Vector3 d = dir / m;
            if (Clear(p, d)) { turnSide = 0f; return dir; }
            float first = turnSide != 0f ? turnSide : 1f;
            foreach (float step in new[] { 30f, 60f, 90f, 120f, 150f })
                foreach (float side in new[] { first, -first })
                {
                    var t = Quaternion.Euler(0f, step * side, 0f) * d;
                    if (!Clear(p, t)) continue;
                    turnSide = side;
                    return t * m;
                }
            return -dir;
        }

        static bool Clear(Vector3 p, Vector3 d)
        {
            for (float s = 1f; s <= 3.5f; s += 1.25f)
            {
                Vector3 q = p + d * s;
                if (Ground.Obstacles != null && Ground.Obstacles.Overlaps(q.x, q.z, 0.6f)) return false;
                if (Ground.Flow != null && Ground.Flow.ClassAt(q.x, q.z) == CellClass.Blocked) return false;
            }
            return true;
        }

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

        // ---------- Report ----------

        void Line(string note)
        {
            if (run == null) return;
            var h = run.PlayerHealth.Health;
            var p = run.Player.transform.position;
            var b = BossController.Active;
            string boss = b != null ? System.FormattableString.Invariant($" | {b.DisplayName} {b.Health:0}/{b.MaxHealth:0} at ({b.transform.position.x:0}, {b.transform.position.z:0})") : "";
            report.log.Add(System.FormattableString.Invariant(
                $"{run.Elapsed:0}s stage {run.StageIndex + 1} t {run.Director.Elapsed:0} hp {h.Current:0}/{h.Max:0} lv {run.Combat.Xp.Level} kills {run.Kills} alive {run.Horde.AliveCount} gold {run.Economy.Wallet.Gold} at ({p.x:0}, {p.z:0}) {note}{boss}"));
        }

        void Finish(string outcome, string note, int exitCode)
        {
            GameInput.MoveOverride = null;
            GameInput.ClearScripted();
            if (outcome != null) report.outcome = outcome;
            if (!string.IsNullOrEmpty(note)) report.note = note;
            if (run != null)
            {
                EndStage();
                var s = run.Summary;
                var c = run.Combat;
                report.seconds = run.Elapsed;
                report.realSeconds = (float)(Time.realtimeSinceStartupAsDouble - realStart);
                report.speedup = report.realSeconds > 0f ? report.seconds / report.realSeconds : 0f;
                report.stagesCleared = s.StagesCleared;
                report.bossesKilled = s.BossesKilled;
                report.reachedThrone = s.ReachedThrone;
                report.won = run.Won;
                report.level = c.Xp.Level;
                report.kills = run.Kills;
                report.chestsOpened = run.Economy.ChestsOpened;
                report.shrinesUsed = run.Economy.ShrinesUsed;
                report.itemsFound = c.Items.Total;
                report.goldEarned = run.Economy.Wallet.Earned;
                report.goldLeft = run.Economy.Wallet.Gold;
                report.loreRead = SaveStore.Current.lore.Count;
                report.embersEarned = run.EmbersEarned;
                report.embersAfter = SaveStore.Current.currency;
                foreach (var q in run.QuestsCompleted) report.questsCompleted.Add(q.Name);
                if (report.outcome == "died")
                {
                    report.killedBy = lastSource;
                    report.killedInStage = run.StageIndex;
                }
                foreach (var w in c.Loadout.Weapons)
                {
                    c.DamageByWeapon.TryGetValue(w.Def.Id, out float dmg);
                    report.weapons.Add(new PlayBotReport.Held { id = w.Def.Id, name = w.Def.Name, level = w.Level, damage = dmg });
                }
                foreach (var ps in c.Loadout.Passives) report.passives.Add(new PlayBotReport.Held { id = ps.Def.Id, name = ps.Def.Name, level = ps.Level });
                foreach (var (item, count) in c.Items.Items) report.items.Add(count > 1 ? $"{item.Name} x{count}" : item.Name);
                report.damage.Sort((a, b) => b.amount.CompareTo(a.amount));
                Line(report.outcome);
            }
            Perf();
            Write();
            Time.captureDeltaTime = 0f;
            Time.timeScale = 1f;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit(exitCode);
#endif
        }

        void Perf()
        {
            var perf = report.perf;
            perf.frames = frameMs.Count;
            if (frameMs.Count == 0) return;
            var sorted = frameMs.OrderBy(x => x).ToList();
            float mean = frameMs.Average();
            perf.fpsMean = 1000f / mean;
            // 1% low: the average frame rate of the slowest 1% of frames.
            int worst = Mathf.Max(1, sorted.Count / 100);
            perf.fpsLow1 = 1000f / sorted.Skip(sorted.Count - worst).Average();
            perf.fpsMin = 1000f / sorted[sorted.Count - 1];
            perf.simMsMean = simSamples > 0 ? simMsSum / simSamples : 0f;
        }

        void Write()
        {
            string json = JsonUtility.ToJson(report, true);
            Debug.Log($"OldGods playbot: {report.godName} {report.seed} {report.outcome}, {report.stagesCleared} stages, level {report.level}, {report.seconds:0}s in {report.realSeconds:0}s");
            if (string.IsNullOrEmpty(outPath)) return;
            try
            {
                var dir = Path.GetDirectoryName(outPath);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                File.WriteAllText(outPath, json);
            }
            catch (System.Exception e) { Debug.LogWarning(e.Message); }
        }
    }
}
