using System;
using System.Collections.Generic;
using OldGods.Rules;
using UnityEngine;

namespace OldGods.Runtime
{
    /// <summary>
    /// The player's side of combat: loadout, stats, XP and the weapons that fire on their own.
    /// Rules decide the numbers; the weapon drivers here turn them into hits on the horde.
    /// </summary>
    public sealed class PlayerCombat : MonoBehaviour
    {
        public static PlayerCombat Instance { get; private set; }

        public ContentLibrary Library { get; private set; }
        public ContentSet Content { get; private set; }
        public readonly Loadout Loadout = new Loadout();
        public readonly DraftCharges Charges = new DraftCharges();
        public readonly XpTracker Xp = new XpTracker();
        public readonly StatBlock Stats = StatBlock.Default();
        /// <summary>Stat changes from outside the draft: the god's kit, shrines, meta powerups.</summary>
        public readonly List<StatMod> ExternalMods = new List<StatMod>();

        public int PendingLevelUps { get; set; }
        public event Action LeveledUp;
        public event Action Changed;

        /// <summary>Damage dealt per weapon id this run, for the results screen.</summary>
        public readonly Dictionary<string, float> DamageByWeapon = new Dictionary<string, float>();

        readonly List<WeaponDriver> drivers = new List<WeaponDriver>();
        System.Random rng;
        PlayerMotor motor;
        PlayerHealth health;

        public PlayerMotor Motor => motor;
        public Vector3 Chest => transform.position + Vector3.up * 1.1f;

        void Awake()
        {
            Instance = this;
            motor = GetComponent<PlayerMotor>();
            health = GetComponent<PlayerHealth>();
        }

        void OnDestroy()
        {
            foreach (var d in drivers) d.Dispose();
            if (Instance == this) Instance = null;
        }

        public void Init(ContentLibrary library, ContentSet content, RunSeed seed)
        {
            Library = library;
            Content = content;
            rng = new System.Random((int)(seed.Stream("combat").NextULong() & 0x7FFFFFFF));
            if (Projectiles.Instance != null) Projectiles.Instance.OnHit = (e, info, dir) => { Hit(e, info, dir); return true; };
            RecomputeStats();
        }

        public void GiveWeapon(string id)
        {
            Loadout.AddWeapon(Content.Weapon(id));
            SyncDrivers();
            Changed?.Invoke();
        }

        public void GivePassive(string id)
        {
            Loadout.AddPassive(Content.Passive(id));
            RecomputeStats();
        }

        /// <summary>Makes sure every weapon in the loadout has a driver.</summary>
        public void SyncDrivers()
        {
            foreach (var w in Loadout.Weapons)
            {
                if (drivers.Exists(d => d.State == w)) continue;
                var asset = Library != null ? Library.WeaponAsset(w.Def.Id) : null;
                drivers.Add(WeaponDriver.Create(this, w, asset));
            }
        }

        public void RecomputeStats()
        {
            Stats.Clear();
            Stats.ApplyAll(ExternalMods);
            Stats.ApplyAll(Loadout.PassiveMods());
            if (motor != null)
            {
                motor.SpeedMultiplier = Stats.Value(StatId.MoveSpeed);
                motor.JumpMultiplier = Stats.Value(StatId.JumpHeight);
                motor.ExtraJumps = Stats.IntValue(StatId.ExtraJumps);
            }
            if (health != null)
            {
                health.SetMaxHealth(Stats.Value(StatId.MaxHealth));
                health.Armor = Stats.Value(StatId.Armor);
                health.Evasion = Stats.Value(StatId.Evasion);
                health.Regen = Stats.Value(StatId.Regen);
            }
            if (Pickups.Instance != null) Pickups.Instance.PickupRange = Stats.Value(StatId.PickupRange);
            Changed?.Invoke();
        }

        public void AddXp(float amount)
        {
            int gained = Xp.Add(amount * Stats.Value(StatId.XpGain));
            if (gained <= 0) return;
            PendingLevelUps += gained;
            LeveledUp?.Invoke();
        }

        public float Roll() => (float)rng.NextDouble();

        void Update()
        {
            if (health != null && health.IsDead) return;
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            foreach (var d in drivers) d.Tick(dt);
        }

        /// <summary>One weapon hit on one enemy. Returns true if it killed.</summary>
        public bool Hit(int enemy, HitInfo info, Vector3 direction)
        {
            var horde = HordeManager.Instance;
            if (horde == null || !horde.Alive[enemy]) return false;
            bool crit = Roll() < info.CritChance;
            float dmg = crit ? info.Damage * info.CritMultiplier : info.Damage;
            direction.y = 0f;
            if (direction.sqrMagnitude < 1e-4f) direction = horde.Position(enemy) - transform.position;
            direction.y = 0f;
            if (info.SlowSeconds > 0f) horde.Slow(enemy, info.SlowSeconds);
            if (info.Source >= 0 && info.Source < Loadout.Weapons.Count)
            {
                string id = Loadout.Weapons[info.Source].Def.Id;
                DamageByWeapon.TryGetValue(id, out float sum);
                DamageByWeapon[id] = sum + dmg;
            }
            return horde.Damage(enemy, dmg, direction.normalized, info.Knockback);
        }

        public int SlotOf(WeaponState w) => Loadout.Weapons.IndexOf(w);
    }

    /// <summary>Runs one weapon: counts down its cooldown and fires its shape.</summary>
    public abstract class WeaponDriver
    {
        public readonly WeaponState State;
        protected readonly PlayerCombat Owner;
        protected readonly WeaponDefinition Asset;
        protected float Timer;
        protected readonly int[] Found = new int[128];

        protected WeaponDriver(PlayerCombat owner, WeaponState state, WeaponDefinition asset)
        {
            Owner = owner;
            State = state;
            Asset = asset;
            Timer = 0.3f;
        }

        protected Color Tint => Asset != null ? Asset.Color : Color.white;
        protected Color GlowColor => Asset != null ? Asset.Glow : Color.white;
        protected HordeManager Horde => HordeManager.Instance;

        public static WeaponDriver Create(PlayerCombat owner, WeaponState state, WeaponDefinition asset)
        {
            switch (state.Def.Shape)
            {
                case WeaponShape.Projectile: return new ProjectileDriver(owner, state, asset);
                case WeaponShape.Aura: return new AuraDriver(owner, state, asset);
                case WeaponShape.Area: return new AreaDriver(owner, state, asset);
                case WeaponShape.Orbit: return new OrbitDriver(owner, state, asset);
                case WeaponShape.Pull: return new PullDriver(owner, state, asset);
                case WeaponShape.Chain: return new ChainDriver(owner, state, asset);
                default: throw new ArgumentOutOfRangeException();
            }
        }

        public virtual void Tick(float dt)
        {
            if (Horde == null) return;
            var e = State.Effective(Owner.Stats);
            Continuous(e, dt);
            Timer -= dt;
            if (Timer > 0f) return;
            Timer = Fire(e) ? e.Cooldown : 0.2f;
        }

        /// <summary>Fires once. Returns false when there was nothing to fire at, to retry soon.</summary>
        protected abstract bool Fire(in EffectiveWeapon e);

        protected virtual void Continuous(in EffectiveWeapon e, float dt) { }

        public virtual void Dispose() { }

        protected HitInfo Info(in EffectiveWeapon e) => new HitInfo
        {
            Damage = e.Damage,
            CritChance = e.CritChance,
            CritMultiplier = e.CritMultiplier,
            Knockback = e.Knockback,
            SlowSeconds = State.Def.SlowSeconds,
            Source = Owner.SlotOf(State),
        };

        protected int HitCircle(Vector3 centre, float radius, in EffectiveWeapon e, bool knockOutward)
        {
            int n = Horde.QueryCircle(centre, radius, Found);
            var info = Info(e);
            for (int k = 0; k < n; k++)
            {
                int i = Found[k];
                Vector3 dir = knockOutward ? Horde.Position(i) - centre : Vector3.zero;
                Owner.Hit(i, info, dir);
            }
            return n;
        }
    }

    sealed class ProjectileDriver : WeaponDriver
    {
        readonly int look;
        readonly Vector3 visualScale;
        int[] targets = new int[1];

        public ProjectileDriver(PlayerCombat o, WeaponState s, WeaponDefinition a) : base(o, s, a)
        {
            visualScale = a != null ? a.VisualScale : new Vector3(1f, 1f, 1.6f);
            look = Projectiles.Instance != null ? Projectiles.Instance.RegisterLook(PlaceholderMeshes.Shard(), Fx.Glow(GlowColor)) : -1;
        }

        protected override bool Fire(in EffectiveWeapon e)
        {
            var proj = Projectiles.Instance;
            if (proj == null) return true;
            Vector3 origin = Owner.Chest;
            if (targets.Length != Mathf.Max(1, e.Count)) targets = new int[Mathf.Max(1, e.Count)];
            int n = Horde.NearestN(origin, e.Range, targets);
            if (n == 0) return false;
            var info = Info(e);
            for (int k = 0; k < e.Count; k++)
            {
                int target = targets[k % n];
                Vector3 dir = Horde.Position(target) + Vector3.up * 0.9f - origin;
                dir.y = 0f;
                if (k >= n)
                {
                    float spread = 12f * ((k - n) / 2 + 1) * ((k - n) % 2 == 0 ? 1f : -1f);
                    dir = Quaternion.Euler(0f, spread, 0f) * dir;
                }
                proj.Fire(origin, dir, e.Speed, e.Duration, 0.45f * e.Size, visualScale * e.Size, e.Pierce, look, info);
            }
            return true;
        }
    }

    sealed class AuraDriver : WeaponDriver
    {
        readonly GameObject ring;

        public AuraDriver(PlayerCombat o, WeaponState s, WeaponDefinition a) : base(o, s, a)
        {
            ring = new GameObject("Aura " + s.Def.Name);
            ring.transform.SetParent(o.transform, false);
            ring.transform.localPosition = Vector3.up * 0.15f;
            ring.AddComponent<MeshFilter>().sharedMesh = Fx.Ring(0.92f, 48);
            var mr = ring.AddComponent<MeshRenderer>();
            var c = GlowColor;
            mr.sharedMaterial = Fx.Fade(new Color(c.r, c.g, c.b, 0.55f));
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        protected override void Continuous(in EffectiveWeapon e, float dt)
        {
            float pulse = 1f + Mathf.Sin(Time.time * 4f) * 0.03f;
            ring.transform.localScale = new Vector3(e.Size * pulse, 1f, e.Size * pulse);
            ring.transform.rotation = Quaternion.Euler(0f, Time.time * 30f, 0f);
        }

        protected override bool Fire(in EffectiveWeapon e)
        {
            HitCircle(Owner.transform.position, e.Size, e, true);
            return true;
        }

        public override void Dispose()
        {
            if (ring != null) UnityEngine.Object.Destroy(ring);
        }
    }

    sealed class AreaDriver : WeaponDriver
    {
        struct Pending { public Vector3 At; public float Left; }
        readonly List<Pending> pending = new List<Pending>();
        EffectiveWeapon last;

        public AreaDriver(PlayerCombat o, WeaponState s, WeaponDefinition a) : base(o, s, a) { }

        protected override bool Fire(in EffectiveWeapon e)
        {
            last = e;
            float delay = Mathf.Max(0.05f, e.Duration);
            if (e.Range <= 0f)
            {
                // Centred on the player (Quake); extra strikes ring around it.
                for (int k = 0; k < e.Count; k++)
                {
                    Vector3 at = Owner.transform.position;
                    if (k > 0)
                    {
                        float a = k * 2.399963f;
                        at += new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * e.Size * 1.2f;
                    }
                    Queue(Ground.Snap(at), delay, e);
                }
                return true;
            }
            int n = Horde.NearestN(Owner.transform.position, e.Range, Found);
            if (n == 0) return false;
            n = Mathf.Min(n, 24);
            for (int k = 0; k < e.Count; k++)
            {
                int target = Found[(int)(Owner.Roll() * n) % n];
                Queue(Horde.Position(target), delay, e);
            }
            return true;
        }

        void Queue(Vector3 at, float delay, in EffectiveWeapon e)
        {
            pending.Add(new Pending { At = at, Left = delay });
            var c = GlowColor;
            Effects.Burst(Fx.Disc(), at + Vector3.up * 0.08f, Quaternion.identity, Vector3.one * e.Size * 0.3f, Vector3.one * e.Size,
                new Color(c.r, c.g, c.b, 0.35f), delay);
        }

        protected override void Continuous(in EffectiveWeapon e, float dt)
        {
            for (int k = pending.Count - 1; k >= 0; k--)
            {
                var p = pending[k];
                p.Left -= dt;
                if (p.Left > 0f) { pending[k] = p; continue; }
                pending.RemoveAt(k);
                HitCircle(p.At, last.Size, last, true);
                var c = GlowColor;
                Effects.Burst(Fx.Column(), p.At, Quaternion.identity, new Vector3(last.Size * 0.5f, 6f, last.Size * 0.5f),
                    new Vector3(last.Size * 1.1f, 0.5f, last.Size * 1.1f), new Color(c.r, c.g, c.b, 0.85f), 0.3f);
            }
        }
    }

    sealed class OrbitDriver : WeaponDriver
    {
        readonly List<Transform> orbiters = new List<Transform>();
        readonly Transform root;
        float[] lastHit;
        float angle;
        readonly Mesh mesh;

        public OrbitDriver(PlayerCombat o, WeaponState s, WeaponDefinition a) : base(o, s, a)
        {
            root = new GameObject("Orbit " + s.Def.Name).transform;
            mesh = a != null && a.VisualScale.x > 1.2f ? Fx.Cube() : PlaceholderMeshes.XpGem();
        }

        public override void Tick(float dt)
        {
            if (Horde == null) return;
            if (lastHit == null || lastHit.Length < Horde.Capacity) lastHit = new float[Horde.Capacity];
            var e = State.Effective(Owner.Stats);
            while (orbiters.Count < e.Count)
            {
                var go = new GameObject("Orbiter");
                go.transform.SetParent(root, false);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                go.AddComponent<MeshRenderer>().sharedMaterial = Fx.Glow(GlowColor);
                orbiters.Add(go.transform);
            }
            angle += e.Speed * dt;
            float radius = e.Range;
            Vector3 centre = Owner.transform.position + Vector3.up * 1f;
            var info = Info(e);
            float now = Time.time;
            Vector3 vs = Asset != null ? Asset.VisualScale : Vector3.one;
            for (int k = 0; k < orbiters.Count; k++)
            {
                var t = orbiters[k];
                bool on = k < e.Count;
                t.gameObject.SetActive(on);
                if (!on) continue;
                float a = angle + k * Mathf.PI * 2f / e.Count;
                Vector3 p = centre + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * radius;
                t.SetPositionAndRotation(p, Quaternion.Euler(0f, -a * Mathf.Rad2Deg, 0f) * Quaternion.Euler(0f, 0f, now * 360f));
                t.localScale = vs * e.Size * 0.5f;
                int n = Horde.QueryCircle(p, 0.6f * e.Size, Found);
                for (int j = 0; j < n; j++)
                {
                    int i = Found[j];
                    if (now - lastHit[i] < e.Cooldown) continue;
                    lastHit[i] = now;
                    Owner.Hit(i, info, Horde.Position(i) - centre);
                }
            }
        }

        protected override bool Fire(in EffectiveWeapon e) => true;

        public override void Dispose()
        {
            if (root != null) UnityEngine.Object.Destroy(root.gameObject);
        }
    }

    sealed class PullDriver : WeaponDriver
    {
        struct Vortex { public Vector3 At; public float Left; public float Tick; public float Ring; }
        readonly List<Vortex> vortices = new List<Vortex>();
        EffectiveWeapon last;

        public PullDriver(PlayerCombat o, WeaponState s, WeaponDefinition a) : base(o, s, a) { }

        protected override bool Fire(in EffectiveWeapon e)
        {
            last = e;
            int n = Horde.NearestN(Owner.transform.position, e.Range, Found);
            if (n == 0) return false;
            for (int k = 0; k < e.Count; k++)
            {
                int target = Found[Mathf.Min(n - 1, k * 3)];
                vortices.Add(new Vortex { At = Horde.Position(target), Left = e.Duration, Tick = 0f });
            }
            return true;
        }

        protected override void Continuous(in EffectiveWeapon e, float dt)
        {
            var c = GlowColor;
            for (int v = vortices.Count - 1; v >= 0; v--)
            {
                var x = vortices[v];
                x.Left -= dt;
                x.Tick -= dt;
                x.Ring -= dt;
                int n = Horde.QueryCircle(x.At, last.Size, Found);
                for (int k = 0; k < n; k++)
                {
                    int i = Found[k];
                    float dx = x.At.x - Horde.X[i], dz = x.At.z - Horde.Z[i];
                    float d = Mathf.Sqrt(dx * dx + dz * dz);
                    if (d < 0.3f) continue;
                    Horde.KnockX[i] = dx / d * last.Knockback;
                    Horde.KnockZ[i] = dz / d * last.Knockback;
                }
                if (x.Tick <= 0f)
                {
                    x.Tick = 0.5f;
                    var info = Info(last);
                    for (int k = 0; k < n; k++) Owner.Hit(Found[k], info, Vector3.zero);
                }
                if (x.Ring <= 0f)
                {
                    x.Ring = 0.25f;
                    Effects.Burst(Fx.Ring(0.8f, 32), x.At + Vector3.up * 0.2f, Quaternion.Euler(0f, Time.time * 200f, 0f),
                        Vector3.one * last.Size, Vector3.one * 0.3f, new Color(c.r, c.g, c.b, 0.6f), 0.35f);
                }
                if (x.Left <= 0f) vortices.RemoveAt(v);
                else vortices[v] = x;
            }
        }
    }

    sealed class ChainDriver : WeaponDriver
    {
        readonly List<int> chain = new List<int>();

        public ChainDriver(PlayerCombat o, WeaponState s, WeaponDefinition a) : base(o, s, a) { }

        protected override bool Fire(in EffectiveWeapon e)
        {
            int first = Horde.Nearest(Owner.transform.position, e.Range);
            if (first < 0) return false;
            chain.Clear();
            chain.Add(first);
            int current = first;
            for (int k = 1; k < e.Count; k++)
            {
                Vector3 at = Horde.Position(current);
                int n = Horde.QueryCircle(at, e.Size, Found);
                int best = -1;
                float bestD = float.MaxValue;
                for (int j = 0; j < n; j++)
                {
                    int i = Found[j];
                    if (chain.Contains(i)) continue;
                    float d = (Horde.Position(i) - at).sqrMagnitude;
                    if (d < bestD) { bestD = d; best = i; }
                }
                if (best < 0) break;
                chain.Add(best);
                current = best;
            }

            var info = Info(e);
            var c = GlowColor;
            Vector3 from = Owner.Chest;
            foreach (int i in chain)
            {
                Vector3 to = Horde.Position(i) + Vector3.up * 1.1f;
                Effects.Bolt(from, to, c, 0.18f, 0.18f);
                Owner.Hit(i, info, to - from);
                from = to;
            }
            return true;
        }
    }
}
