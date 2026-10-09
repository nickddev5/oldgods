using System;
using System.Collections;
using OldGods.Rules;
using UnityEngine;

namespace OldGods.Runtime
{
    /// <summary>
    /// A boss with a full model and telegraphed attacks. Its health lives in a controlled
    /// horde slot, so every weapon hits it without special cases.
    /// </summary>
    public sealed class BossController : MonoBehaviour
    {
        /// <summary>Every boss alive now (a Boss Curse can wake several).</summary>
        public static readonly System.Collections.Generic.List<BossController> All = new System.Collections.Generic.List<BossController>();
        /// <summary>The boss the HUD and camera follow: the first one alive.</summary>
        public static BossController Active => All.Count > 0 ? All[0] : null;

        public BossDefinition Asset { get; private set; }
        public BossDef Def { get; private set; }
        public int Slot { get; private set; } = -1;
        public float MaxHealth { get; private set; }
        public float Health => Slot >= 0 && horde.Alive[Slot] ? Mathf.Max(0f, horde.Hp[Slot]) : 0f;
        public bool Dead { get; private set; }
        public string DisplayName => Asset != null ? Asset.DisplayName : "Boss";

        public event Action<BossController> Defeated;

        HordeManager horde;
        PlayerMotor player;
        PlayerHealth playerHealth;
        BossPattern pattern;
        Rng rng;
        int stage;
        bool attacking;
        Transform model;
        MeshRenderer modelRenderer;
        MaterialPropertyBlock props;
        Color accent;
        Vector3 velocity;
        float damageScale;

        public static BossController Spawn(BossDefinition asset, Vector3 at, int stageIndex, int curses, GameAssets assets,
            HordeManager horde, PlayerMotor player, Rng rng)
        {
            var go = new GameObject("Boss " + asset.DisplayName);
            go.transform.position = Ground.Snap(at);
            var boss = go.AddComponent<BossController>();
            boss.Init(asset, stageIndex, curses, assets, horde, player, rng);
            return boss;
        }

        void Init(BossDefinition asset, int stageIndex, int curses, GameAssets assets, HordeManager h, PlayerMotor p, Rng r)
        {
            Asset = asset;
            Def = asset.ToDef();
            horde = h;
            player = p;
            playerHealth = p.GetComponent<PlayerHealth>();
            rng = r;
            stage = stageIndex;
            pattern = new BossPattern(Def);
            damageScale = BossScaling.Damage(1f, stageIndex);
            accent = asset.Accent;
            props = new MaterialPropertyBlock();

            var enemyDef = new EnemyDef { Id = Def.Id, DisplayName = Def.Name, MaxHealth = Def.MaxHealth, MoveSpeed = 0f, Radius = Def.Radius / Def.Scale, Scale = Def.Scale, XpValue = 0, IsElite = true };
            int type = h.TypeOf(enemyDef);
            if (type < 0) type = h.RegisterType(enemyDef, EnemyModels.Get(EnemyModel.Husk), Color.white);
            MaxHealth = BossScaling.Health(Def, stageIndex, curses);
            Slot = h.Spawn(type, transform.position, MaxHealth / Def.MaxHealth);
            if (Slot < 0)
            {
                Debug.LogError("OldGods: no horde slot for the boss");
                Destroy(gameObject);
                return;
            }
            h.Controlled[Slot] = true;
            h.Hidden[Slot] = true;
            h.SlotKilled += OnSlotKilled;

            var m = new GameObject("Model");
            m.transform.SetParent(transform, false);
            if (asset.ModelPrefab != null)
            {
                WorldBuilder.AttachModel(asset.ModelPrefab, m.transform, asset.ModelScale);
            }
            else
            {
                m.transform.localScale = Vector3.one * Def.Scale;
                m.AddComponent<MeshFilter>().sharedMesh = BossModels.Get(asset.Model);
                modelRenderer = m.AddComponent<MeshRenderer>();
                // Built-in models carry their colours in the mesh.
                modelRenderer.sharedMaterial = WorldBuilder.Tinted(assets.LowPoly, Color.white);
                var walk = m.AddComponent<WalkAnimator>();
                walk.Tracked = transform;
                walk.MaxSwing = 0.22f;
            }
            model = m.transform;
            All.Add(this);

            // Rise out of the ground.
            StartCoroutine(Rise());
        }

        IEnumerator Rise()
        {
            attacking = true;
            float t = 0f;
            Effects.Burst(Fx.Ring(0.7f, 40), transform.position + Vector3.up * 0.2f, Quaternion.identity, Vector3.one * 2f, Vector3.one * Def.Scale * 3f, accent, 1.2f);
            Audio.Play(Sfx.BossWake, 1f, 0f);
            while (t < 1.2f)
            {
                t += Time.deltaTime;
                model.localPosition = Vector3.up * Mathf.Lerp(-Def.Scale * 1.8f, 0f, t / 1.2f);
                yield return null;
            }
            model.localPosition = Vector3.zero;
            pattern.Finished(Time.time, 1f);
            attacking = false;
        }

        void OnDestroy()
        {
            if (horde != null) horde.SlotKilled -= OnSlotKilled;
            All.Remove(this);
        }

        void OnSlotKilled(int slot)
        {
            if (slot != Slot || Dead) return;
            Dead = true;
            StopAllCoroutines();
            Vector3 at = transform.position;
            Effects.Burst(Fx.Column(), at, Quaternion.identity, new Vector3(2f, 1f, 2f), new Vector3(Def.Scale * 2f, 25f, Def.Scale * 2f), accent, 1.2f);
            Effects.Burst(Fx.Ring(0.6f, 48), at + Vector3.up * 0.3f, Quaternion.identity, Vector3.one, Vector3.one * 20f, accent, 1f);
            Audio.Play(Sfx.Death, 1f, 0f);
            All.Remove(this);
            Defeated?.Invoke(this);
            Destroy(gameObject, 0.05f);
        }

        float HealthFraction => MaxHealth > 0f ? Health / MaxHealth : 0f;

        void Update()
        {
            if (Dead || Slot < 0 || player == null) return;
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            Vector3 toPlayer = player.transform.position - transform.position;
            toPlayer.y = 0f;
            float dist = toPlayer.magnitude;

            if (!attacking)
            {
                Vector3 want = dist > Def.Radius + 3.5f ? toPlayer / Mathf.Max(0.01f, dist) * Def.MoveSpeed : Vector3.zero;
                velocity = Vector3.MoveTowards(velocity, want, 10f * dt);
                Move(velocity * dt);
                if (dist > 0.1f) Face(toPlayer, dt, 180f);

                int attack = pattern.Next(Time.time, HealthFraction, rng);
                if (attack >= 0) StartCoroutine(Perform(Def.Attacks[attack]));
            }

            // Contact damage.
            if (dist < Def.Radius + 0.7f && Mathf.Abs(player.transform.position.y - transform.position.y) < Def.Scale * 1.5f)
                playerHealth.TakeHit(Def.ContactDamage * damageScale, transform.position);

            Sync();
        }

        void Move(Vector3 delta)
        {
            Vector3 p = Ground.ClampToPlayable(transform.position + delta, 2f);
            transform.position = Ground.Snap(p);
        }

        void Face(Vector3 dir, float dt, float degPerSec)
        {
            if (dir.sqrMagnitude < 1e-4f) return;
            var target = Quaternion.LookRotation(dir.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, target, degPerSec * dt);
        }

        void Sync()
        {
            var p = transform.position;
            horde.X[Slot] = p.x;
            horde.Z[Slot] = p.z;
            horde.Y[Slot] = p.y;
            float flash = horde.Flash[Slot];
            if (modelRenderer != null)
            {
                modelRenderer.GetPropertyBlock(props);
                props.SetColor("_EmissionColor", Color.white * flash * 0.6f);
                modelRenderer.SetPropertyBlock(props);
            }
        }

        bool PlayerGrounded => player.transform.position.y - Ground.Height(player.transform.position.x, player.transform.position.z) < 0.6f;

        void HitPlayerIfInside(Vector3 centre, float radius, float damage)
        {
            Vector3 d = player.transform.position - centre;
            d.y = 0f;
            if (d.magnitude <= radius) playerHealth.TakeHit(damage * damageScale, centre);
        }

        static readonly Color Warning = new Color(1f, 0.25f, 0.15f, 0.45f);

        IEnumerator Perform(BossAttackDef a)
        {
            attacking = true;
            velocity = Vector3.zero;
            switch (a.Attack)
            {
                case BossAttack.Slam:
                {
                    Vector3 at = Ground.Snap(player.transform.position);
                    Effects.Burst(Fx.Disc(), at + Vector3.up * 0.1f, Quaternion.identity, Vector3.one * a.Size * 0.2f, Vector3.one * a.Size, Warning, a.Telegraph);
                    yield return Lean(a.Telegraph, -12f);
                    HitPlayerIfInside(at, a.Size, a.Damage);
                    Audio.Play(Sfx.Slam, 0.9f);
                    CameraShake.Kick(0.7f);
                    Effects.Burst(Fx.Column(), at, Quaternion.identity, new Vector3(a.Size, 0.5f, a.Size), new Vector3(a.Size * 1.2f, 4f, a.Size * 1.2f), accent, 0.35f);
                    break;
                }
                case BossAttack.Charge:
                {
                    Vector3 dir = player.transform.position - transform.position;
                    dir.y = 0f;
                    dir = dir.sqrMagnitude > 0.01f ? dir.normalized : transform.forward;
                    transform.rotation = Quaternion.LookRotation(dir);
                    Vector3 mid = transform.position + dir * a.Size * 0.5f + Vector3.up * 0.1f;
                    Effects.Burst(Fx.Cube(), mid, Quaternion.LookRotation(dir), new Vector3(Def.Radius * 2f, 0.05f, a.Size * 0.2f),
                        new Vector3(Def.Radius * 2f, 0.05f, a.Size), Warning, a.Telegraph);
                    yield return Lean(a.Telegraph, 15f);
                    float travelled = 0f, speed = a.Size / 0.55f;
                    bool hit = false;
                    while (travelled < a.Size)
                    {
                        float step = speed * Time.deltaTime;
                        travelled += step;
                        Move(dir * step);
                        Sync();
                        if (!hit)
                        {
                            Vector3 d = player.transform.position - transform.position;
                            d.y = 0f;
                            if (d.magnitude < Def.Radius + 1.2f) { playerHealth.TakeHit(a.Damage * damageScale, transform.position); hit = true; }
                        }
                        yield return null;
                    }
                    break;
                }
                case BossAttack.Shockwave:
                {
                    yield return Lean(a.Telegraph, -20f);
                    Audio.Play(Sfx.Slam, 0.8f);
                    CameraShake.Kick(0.5f);
                    Vector3 c = transform.position;
                    float r = 0f, speed = 14f;
                    bool hit = false;
                    while (r < a.Size)
                    {
                        r += speed * Time.deltaTime;
                        if (Time.frameCount % 3 == 0)
                            Effects.Burst(Fx.Ring(0.9f, 48), c + Vector3.up * 0.25f, Quaternion.identity, Vector3.one * r, Vector3.one * (r + 0.6f), accent, 0.15f);
                        Vector3 d = player.transform.position - c;
                        d.y = 0f;
                        if (!hit && Mathf.Abs(d.magnitude - r) < 0.9f && PlayerGrounded)
                        {
                            playerHealth.TakeHit(a.Damage * damageScale, c);
                            hit = true;
                        }
                        yield return null;
                    }
                    break;
                }
                case BossAttack.Summon:
                {
                    yield return Lean(a.Telegraph, -8f);
                    var stageDirector = StageDirector.Instance;
                    int count = Mathf.Max(1, Mathf.RoundToInt(a.Size));
                    for (int i = 0; i < count; i++)
                    {
                        float ang = i * Mathf.PI * 2f / count;
                        Vector3 at = transform.position + new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)) * (Def.Radius + 2.5f);
                        if (stageDirector != null) stageDirector.SpawnEnemy(Def.MinionId, at);
                        Effects.Burst(Fx.Ring(0.6f, 24), Ground.Snap(at) + Vector3.up * 0.2f, Quaternion.identity, Vector3.one * 0.3f, Vector3.one * 1.6f, accent, 0.5f);
                    }
                    break;
                }
                case BossAttack.Volley:
                {
                    var points = new Vector3[5];
                    for (int i = 0; i < points.Length; i++)
                    {
                        Vector2 off = i == 0 ? Vector2.zero : new Vector2(rng.Range(-6f, 6f), rng.Range(-6f, 6f));
                        points[i] = Ground.Snap(player.transform.position + new Vector3(off.x, 0f, off.y));
                        Effects.Burst(Fx.Disc(), points[i] + Vector3.up * 0.1f, Quaternion.identity, Vector3.one * a.Size * 0.2f, Vector3.one * a.Size, Warning, a.Telegraph);
                    }
                    yield return Lean(a.Telegraph, -10f);
                    Audio.Play(Sfx.Slam, 0.7f);
                    foreach (var p in points)
                    {
                        HitPlayerIfInside(p, a.Size, a.Damage);
                        Effects.Burst(Fx.Column(), p, Quaternion.identity, new Vector3(a.Size * 0.4f, 8f, a.Size * 0.4f), new Vector3(a.Size, 0.5f, a.Size), accent, 0.3f);
                    }
                    break;
                }
            }
            pattern.Finished(Time.time, HealthFraction);
            attacking = false;
        }

        /// <summary>Winds up by leaning the model, which reads as a telegraph on the body too.</summary>
        IEnumerator Lean(float seconds, float degrees)
        {
            float t = 0f;
            while (t < seconds)
            {
                t += Time.deltaTime;
                float k = Mathf.Sin(Mathf.Clamp01(t / seconds) * Mathf.PI * 0.5f);
                model.localRotation = Quaternion.Euler(degrees * k, 0f, 0f);
                Sync();
                yield return null;
            }
            model.localRotation = Quaternion.identity;
        }
    }
}
