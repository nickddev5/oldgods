using System;
using System.Collections.Generic;
using OldGods.Rules;
using UnityEngine;

namespace OldGods.Runtime
{
    /// <summary>
    /// Owns every common enemy as struct-of-arrays (ADR 0001). One loop per frame
    /// steers, separates, moves, snaps to the ground and applies contact damage.
    /// Weapons query and damage enemies by index through this API.
    /// </summary>
    public sealed class HordeManager : MonoBehaviour
    {
        public static HordeManager Instance { get; private set; }

        [Tooltip("Maximum enemies alive at once.")]
        public int Capacity = 2048;
        public float SeparationWeight = 1.1f;
        public float Acceleration = 14f;
        [Tooltip("Enemies farther than this from the player are moved back to the spawn ring.")]
        public float LeashDistance = 70f;
        public float PlayerRadius = 0.5f;
        /// <summary>Speed multiplier for every enemy (the Swift Horde modifier).</summary>
        public float GlobalSpeed = 1f;

        public Transform Target;
        public PlayerHealth TargetHealth;

        /// <summary>Called when an enemy dies: world position, definition, whether a player weapon killed it.</summary>
        public event Action<Vector3, EnemyDef, bool> EnemyKilled;
        /// <summary>Called with the slot index just before it is freed, for owners of controlled slots.</summary>
        public event Action<int> SlotKilled;

        // State arrays, indexed by enemy slot.
        public float[] X, Z, Y, VX, VZ, Yaw, Hp, MaxHp, Phase, Flash, SlowFor, KnockX, KnockZ;
        public int[] TypeIndex;
        public bool[] Alive;
        /// <summary>Moved by another script (a boss); the horde loop leaves position alone.</summary>
        public bool[] Controlled;
        /// <summary>Not drawn by the horde renderer (it has its own model).</summary>
        public bool[] Hidden;
        public float[] SpeedMul, DamageMul;

        readonly List<EnemyDef> types = new List<EnemyDef>();
        readonly List<float> legLengths = new List<float>(); // hip height in metres, per type
        readonly Stack<int> free = new Stack<int>();
        int highWater;
        SpatialHash hash;
        int[] scratch = new int[256];
        float maxRadius = 0.5f;

        public int AliveCount { get; private set; }
        public int HighWater => highWater;
        public IReadOnlyList<EnemyDef> Types => types;
        public SpatialHash Hash => hash;
        public float SpawnRingMin = 26f, SpawnRingMax = 34f;
        public Rng SpawnRng;

        public HordeRenderer Renderer { get; private set; }

        void Awake()
        {
            Instance = this;
            Allocate(Capacity);
            Renderer = GetComponent<HordeRenderer>();
            SpawnRng ??= new Rng((ulong)Environment.TickCount);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Allocate(int n)
        {
            X = new float[n]; Z = new float[n]; Y = new float[n];
            VX = new float[n]; VZ = new float[n]; Yaw = new float[n];
            Hp = new float[n]; MaxHp = new float[n]; Phase = new float[n]; Flash = new float[n];
            SlowFor = new float[n]; KnockX = new float[n]; KnockZ = new float[n];
            SpeedMul = new float[n]; DamageMul = new float[n];
            TypeIndex = new int[n];
            Alive = new bool[n];
            Controlled = new bool[n];
            Hidden = new bool[n];
            free.Clear();
            for (int i = n - 1; i >= 0; i--) free.Push(i);
            highWater = 0;
            AliveCount = 0;
            hash = new SpatialHash(1.2f, 13);
        }

        /// <summary>Registers an enemy type with its look. Returns the type index.</summary>
        public int RegisterType(EnemyDef def, Mesh mesh, Color color, Color emission = default, float walkSwing = 0.22f)
        {
            int existing = types.IndexOf(def);
            if (existing >= 0) return existing;
            types.Add(def);
            legLengths.Add(MeshKit.HipHeight(mesh) * def.Scale);
            maxRadius = Mathf.Max(maxRadius, def.Radius * def.Scale);
            hash = new SpatialHash(Mathf.Max(1.2f, maxRadius * 2.5f), 13);
            if (Renderer == null) Renderer = GetComponent<HordeRenderer>();
            if (Renderer != null) Renderer.AddType(mesh, color, def.Scale, emission, walkSwing);
            return types.Count - 1;
        }

        public int TypeOf(EnemyDef def) => types.IndexOf(def);

        /// <summary>Spawns one enemy. Returns its slot, or -1 when the horde is full.</summary>
        public int Spawn(int type, Vector3 position, float healthMultiplier = 1f, float speedMultiplier = 1f, float damageMultiplier = 1f)
        {
            if (free.Count == 0 || type < 0 || type >= types.Count) return -1;
            var def = types[type];
            int i = free.Pop();
            position = Ground.ClampToPlayable(position, 1f);
            X[i] = position.x; Z[i] = position.z; Y[i] = Ground.Height(position.x, position.z);
            VX[i] = 0f; VZ[i] = 0f;
            Hp[i] = MaxHp[i] = def.MaxHealth * healthMultiplier;
            Phase[i] = SpawnRng.Range(0f, 6.283f);
            Flash[i] = 0f; SlowFor[i] = 0f; KnockX[i] = 0f; KnockZ[i] = 0f;
            SpeedMul[i] = speedMultiplier;
            DamageMul[i] = damageMultiplier;
            TypeIndex[i] = type;
            Alive[i] = true;
            Controlled[i] = false;
            Hidden[i] = false;
            if (Target != null)
            {
                Vector3 d = Target.position - position;
                Yaw[i] = Mathf.Atan2(d.x, d.z);
            }
            AliveCount++;
            if (i + 1 > highWater) highWater = i + 1;
            return i;
        }

        /// <summary>Spawns on the ring around the target, off screen.</summary>
        public int SpawnOnRing(int type, float healthMultiplier = 1f, float speedMultiplier = 1f, float damageMultiplier = 1f)
        {
            Vector3 c = Target != null ? Target.position : Vector3.zero;
            var p = RingSpawnPoint(new Vec2(c.x, c.z));
            return Spawn(type, new Vector3(p.X, 0f, p.Z), healthMultiplier, speedMultiplier, damageMultiplier);
        }

        /// <summary>
        /// A point on the spawn ring the horde can walk to the player from: not inside a wall,
        /// on a cliff or in a pocket with no path. Falls back to any ring point.
        /// </summary>
        Vec2 RingSpawnPoint(Vec2 center)
        {
            var p = HordeSteering.RingPoint(center, SpawnRingMin, SpawnRingMax, SpawnRng);
            var flow = Ground.Flow;
            if (flow == null) return p;
            for (int attempt = 0; attempt < 8; attempt++)
            {
                var clamped = Ground.ClampToPlayable(new Vector3(p.X, 0f, p.Z), 1f);
                if (flow.ClassAt(clamped.x, clamped.z) == CellClass.Open && flow.Reachable(clamped.x, clamped.z)) return new Vec2(clamped.x, clamped.z);
                p = HordeSteering.RingPoint(center, SpawnRingMin, SpawnRingMax, SpawnRng);
            }
            return p;
        }

        public EnemyDef Def(int i) => types[TypeIndex[i]];
        public Vector3 Position(int i) => new Vector3(X[i], Y[i], Z[i]);
        /// <summary>How far an enemy's body reaches from its centre on the ground plane.</summary>
        public float BodyRadius(int i) => types[TypeIndex[i]].Radius * types[TypeIndex[i]].Scale;

        /// <summary>Deals damage. Returns true if this killed the enemy.</summary>
        public bool Damage(int i, float amount, Vector3 knockDirection = default, float knock = 0f)
        {
            if (i < 0 || i >= highWater || !Alive[i] || amount <= 0f) return false;
            Hp[i] -= amount;
            Flash[i] = 1f;
            if (knock > 0f)
            {
                KnockX[i] += knockDirection.x * knock;
                KnockZ[i] += knockDirection.z * knock;
            }
            DamageNumbers.Show(Position(i) + Vector3.up * 1.8f * Def(i).Scale, amount);
            Audio.Play(Sfx.Hit, 0.45f, 0.15f);
            if (Hp[i] <= 0f)
            {
                Kill(i, true);
                return true;
            }
            return false;
        }

        public void Slow(int i, float seconds)
        {
            if (i >= 0 && i < highWater && Alive[i]) SlowFor[i] = Mathf.Max(SlowFor[i], seconds);
        }

        public void Kill(int i, bool byPlayer)
        {
            if (!Alive[i]) return;
            SlotKilled?.Invoke(i);
            Alive[i] = false;
            AliveCount--;
            free.Push(i);
            var def = types[TypeIndex[i]];
            if (byPlayer)
            {
                Audio.Play(Sfx.Kill, 0.5f, 0.12f);
                DeathPuff(new Vector3(X[i], Y[i], Z[i]), def.Scale);
            }
            EnemyKilled?.Invoke(new Vector3(X[i], Y[i], Z[i]), def, byPlayer);
        }

        float puffBudget;

        /// <summary>A brief ring of dust where an enemy fell, capped so big waves stay cheap.</summary>
        void DeathPuff(Vector3 at, float scale)
        {
            if (puffBudget <= 0f) return;
            puffBudget -= 1f;
            Effects.Burst(Fx.Ring(0.55f, 16), at + Vector3.up * 0.15f, Quaternion.identity, Vector3.one * 0.3f * scale, Vector3.one * 1.4f * scale,
                new Color(0.85f, 0.8f, 0.7f, 0.5f), 0.35f);
        }

        public void KillAll(bool byPlayer)
        {
            for (int i = 0; i < highWater; i++)
                if (Alive[i] && !Controlled[i]) Kill(i, byPlayer);
        }

        /// <summary>Alive count excluding controlled slots such as bosses.</summary>
        public int CommonAlive
        {
            get
            {
                int n = AliveCount;
                for (int i = 0; i < highWater; i++) if (Alive[i] && Controlled[i]) n--;
                return n;
            }
        }

        /// <summary>Removes every enemy without death events, for restarts.</summary>
        public void Clear()
        {
            for (int i = 0; i < highWater; i++) Alive[i] = false;
            free.Clear();
            for (int i = X.Length - 1; i >= 0; i--) free.Push(i);
            highWater = 0;
            AliveCount = 0;
        }

        /// <summary>Alive enemies whose centre lies within radius of p (ground plane). Returns count written.</summary>
        public int QueryCircle(Vector3 p, float radius, int[] results)
        {
            float probe = radius + maxRadius;
            if (scratch.Length < results.Length * 4) scratch = new int[results.Length * 4];
            int n = hash.Query(p.x, p.z, probe, scratch);
            int found = 0;
            for (int k = 0; k < n && found < results.Length; k++)
            {
                int i = scratch[k];
                if (!Alive[i]) continue;
                float dx = X[i] - p.x, dz = Z[i] - p.z;
                float r = radius + types[TypeIndex[i]].Radius * types[TypeIndex[i]].Scale;
                if (dx * dx + dz * dz <= r * r) results[found++] = i;
            }
            return found;
        }

        /// <summary>Nearest alive enemy within maxRange, or -1.</summary>
        public int Nearest(Vector3 p, float maxRange, int skip = -1)
        {
            int best = -1;
            float bestD = maxRange * maxRange;
            for (int i = 0; i < highWater; i++)
            {
                if (!Alive[i] || i == skip) continue;
                float dx = X[i] - p.x, dz = Z[i] - p.z;
                float d = dx * dx + dz * dz;
                if (d < bestD) { bestD = d; best = i; }
            }
            return best;
        }

        /// <summary>The n nearest alive enemies within maxRange, nearest first.</summary>
        public int NearestN(Vector3 p, float maxRange, int[] results)
        {
            int count = 0;
            float max2 = maxRange * maxRange;
            var dists = nearestScratch;
            if (dists.Length < results.Length) dists = nearestScratch = new float[results.Length];
            for (int i = 0; i < highWater; i++)
            {
                if (!Alive[i]) continue;
                float dx = X[i] - p.x, dz = Z[i] - p.z;
                float d = dx * dx + dz * dz;
                if (d > max2) continue;
                if (count < results.Length)
                {
                    int k = count++;
                    while (k > 0 && dists[k - 1] > d) { dists[k] = dists[k - 1]; results[k] = results[k - 1]; k--; }
                    dists[k] = d; results[k] = i;
                }
                else if (d < dists[count - 1])
                {
                    int k = count - 1;
                    while (k > 0 && dists[k - 1] > d) { dists[k] = dists[k - 1]; results[k] = results[k - 1]; k--; }
                    dists[k] = d; results[k] = i;
                }
            }
            return count;
        }
        float[] nearestScratch = new float[16];

        /// <summary>Milliseconds the last Simulate call took, for the probe and the debug line.</summary>
        public float LastSimulateMs { get; private set; }
        readonly System.Diagnostics.Stopwatch simWatch = new System.Diagnostics.Stopwatch();

        /// <summary>
        /// An enemy follows the flow field only when the path is this much longer than the
        /// straight line, so on open ground the horde still runs straight at the player.
        /// </summary>
        public float DetourRatio = 1.25f;
        FlowField solvedFlow;
        int solvedCell = -1;

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            puffBudget = Mathf.Min(12f, puffBudget + dt * 30f);
            simWatch.Restart();
            Simulate(dt);
            LastSimulateMs = (float)simWatch.Elapsed.TotalMilliseconds;
        }

        /// <summary>One simulation step. Public so probes and tests can drive it.</summary>
        public void Simulate(float dt)
        {
            hash.Build(X, Z, Alive, highWater);
            Vector3 tp = Target != null ? Target.position : Vector3.zero;
            var target = new Vec2(tp.x, tp.z);
            bool hasTarget = Target != null;
            float leash2 = LeashDistance * LeashDistance;

            // Paths only change when the player moves to another cell.
            var flow = hasTarget ? Ground.Flow : null;
            var obstacles = Ground.Obstacles;
            if (flow != null)
            {
                int cell = flow.CellOf(tp.x, tp.z);
                if (flow != solvedFlow || cell != solvedCell)
                {
                    flow.Solve(tp.x, tp.z);
                    solvedFlow = flow;
                    solvedCell = cell;
                }
            }

            for (int i = 0; i < highWater; i++)
            {
                if (!Alive[i]) continue;
                if (Controlled[i])
                {
                    Flash[i] = Mathf.Max(0f, Flash[i] - dt * 6f);
                    continue;
                }
                var def = types[TypeIndex[i]];
                float speed = def.MoveSpeed * SpeedMul[i] * GlobalSpeed * (SlowFor[i] > 0f ? 0.45f : 1f);
                float radius = def.Radius * def.Scale;

                // Around walls and cliffs, head for the next step of the path instead of the player.
                Vec2 goal = target;
                CellClass here = CellClass.Open;
                if (flow != null)
                {
                    here = flow.ClassAt(X[i], Z[i]);
                    if (here == CellClass.Climb) speed *= 0.5f;
                    float ex = tp.x - X[i], ez = tp.z - Z[i];
                    float straight = Mathf.Sqrt(ex * ex + ez * ez);
                    if (straight > 3f)
                    {
                        float path = flow.Distance(X[i], Z[i]);
                        if (!float.IsInfinity(path) && path > straight * DetourRatio + 2f && flow.Direction(X[i], Z[i], out float fdx, out float fdz))
                            goal = new Vec2(X[i] + fdx * 8f, Z[i] + fdz * 8f);
                    }
                }

                Vec2 desired = hasTarget
                    ? HordeSteering.Desired(i, X, Z, goal, speed, radius * 2.2f, SeparationWeight, hash, scratch)
                    : default;
                var v = HordeSteering.Accelerate(new Vec2(VX[i], VZ[i]), desired, Acceleration, dt);
                VX[i] = v.X;
                VZ[i] = v.Z;

                float nx = X[i] + (v.X + KnockX[i]) * dt;
                float nz = Z[i] + (v.Z + KnockZ[i]) * dt;
                float decay = Mathf.Exp(-8f * dt);
                KnockX[i] *= decay;
                KnockZ[i] *= decay;

                if (hasTarget)
                {
                    // Keep out of the player's body so the horde surrounds instead of stacking inside.
                    float dx = nx - tp.x, dz = nz - tp.z;
                    float min = radius + PlayerRadius;
                    float d2 = dx * dx + dz * dz;
                    if (d2 < min * min && d2 > 1e-6f)
                    {
                        float d = Mathf.Sqrt(d2);
                        nx = tp.x + dx / d * min;
                        nz = tp.z + dz / d * min;
                    }

                    if (d2 <= (min + 0.15f) * (min + 0.15f) && Mathf.Abs(Y[i] - tp.y) < 2.2f && TargetHealth != null)
                        TargetHealth.TakeHit(def.ContactDamage * DamageMul[i], Position(i));

                    if (d2 > leash2)
                    {
                        var rp = RingSpawnPoint(target);
                        nx = rp.X;
                        nz = rp.Z;
                        here = CellClass.Blocked; // a fresh spot: skip the cliff check below
                    }
                }

                // Solid dressing pushes enemies out; cliffs stop them, sliding along the edge.
                if (obstacles != null) obstacles.PushOut(ref nx, ref nz, radius * 0.8f);
                if (flow != null && here != CellClass.Blocked && flow.ClassAt(nx, nz) == CellClass.Blocked)
                {
                    if (flow.ClassAt(nx, Z[i]) != CellClass.Blocked) nz = Z[i];
                    else if (flow.ClassAt(X[i], nz) != CellClass.Blocked) nx = X[i];
                    else { nx = X[i]; nz = Z[i]; }
                }

                var clamped = Ground.ClampToPlayable(new Vector3(nx, 0f, nz), 0.5f);
                X[i] = clamped.x;
                Z[i] = clamped.z;
                Y[i] = Ground.Height(X[i], Z[i]);

                float sp2 = v.X * v.X + v.Z * v.Z;
                if (sp2 > 0.01f)
                {
                    float targetYaw = Mathf.Atan2(v.X, v.Z);
                    Yaw[i] = Mathf.LerpAngle(Yaw[i] * Mathf.Rad2Deg, targetYaw * Mathf.Rad2Deg, 1f - Mathf.Exp(-10f * dt)) * Mathf.Deg2Rad;
                }
                Phase[i] += Gait.PhaseStep(Mathf.Sqrt(sp2), legLengths[TypeIndex[i]], dt);
                if (Phase[i] > 1000f) Phase[i] -= 6.2831853f * 159f;
                Flash[i] = Mathf.Max(0f, Flash[i] - dt * 6f);
                SlowFor[i] = Mathf.Max(0f, SlowFor[i] - dt);
            }
        }

        void LateUpdate()
        {
            if (Renderer != null) Renderer.Draw(this);
        }
    }
}
