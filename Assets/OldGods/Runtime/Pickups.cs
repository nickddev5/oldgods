using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace OldGods.Runtime
{
    public enum PickupKind { Xp, Gold, Heal, Magnet }

    /// <summary>
    /// Every loose pickup on the map (XP gems, gold, food, magnets) as arrays. Pickups
    /// inside the player's pickup range fly to the player and are collected on touch.
    /// When the pool is full, new XP merges into an existing gem so nothing is lost.
    /// </summary>
    public sealed class Pickups : MonoBehaviour
    {
        public static Pickups Instance { get; private set; }

        public int Capacity = 2000;
        public float PickupRange = 3.5f;
        public Transform Player;

        public event Action<PickupKind, float> Collected;

        Vector3[] pos;
        float[] value;
        PickupKind[] kind;
        bool[] alive, homing;
        float[] speed;
        int count;
        int highWater;
        readonly System.Collections.Generic.Stack<int> free = new System.Collections.Generic.Stack<int>();
        Matrix4x4[] batch = new Matrix4x4[1023];
        Material xpSmall, xpMid, xpBig, gold, heal, magnet;
        Mesh gem;

        public int Count => count;

        void Awake()
        {
            Instance = this;
            pos = new Vector3[Capacity];
            value = new float[Capacity];
            kind = new PickupKind[Capacity];
            alive = new bool[Capacity];
            homing = new bool[Capacity];
            speed = new float[Capacity];
            for (int i = Capacity - 1; i >= 0; i--) free.Push(i);
            gem = PlaceholderMeshes.XpGem();
            xpSmall = Fx.Glow(new Color(0.35f, 0.75f, 1.6f));
            xpMid = Fx.Glow(new Color(0.45f, 1.6f, 0.55f));
            xpBig = Fx.Glow(new Color(1.8f, 0.45f, 0.45f));
            gold = Fx.Glow(new Color(1.8f, 1.35f, 0.3f));
            heal = Fx.Glow(new Color(1.6f, 0.5f, 0.7f));
            magnet = Fx.Glow(new Color(1.4f, 1.4f, 1.6f));
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Spawn(PickupKind k, Vector3 at, float amount)
        {
            if (free.Count == 0)
            {
                if (k != PickupKind.Xp && k != PickupKind.Gold) return;
                // Merge into the first live pickup of the same kind.
                for (int j = 0; j < highWater; j++)
                    if (alive[j] && kind[j] == k) { value[j] += amount; return; }
                return;
            }
            int i = free.Pop();
            pos[i] = Ground.Snap(at) + Vector3.up * 0.5f;
            value[i] = amount;
            kind[i] = k;
            alive[i] = true;
            homing[i] = false;
            speed[i] = 0f;
            count++;
            if (i + 1 > highWater) highWater = i + 1;
        }

        /// <summary>Pulls every XP gem on the map to the player (the Magnet pickup and shrine).</summary>
        public void MagnetAll()
        {
            for (int i = 0; i < highWater; i++)
                if (alive[i] && (kind[i] == PickupKind.Xp || kind[i] == PickupKind.Gold)) homing[i] = true;
        }

        public void Clear()
        {
            for (int i = 0; i < highWater; i++) alive[i] = false;
            free.Clear();
            for (int i = Capacity - 1; i >= 0; i--) free.Push(i);
            count = 0;
            highWater = 0;
        }

        void Update()
        {
            if (Player == null) return;
            float dt = Time.deltaTime;
            Vector3 p = Player.position + Vector3.up * 1f;
            float range2 = PickupRange * PickupRange;
            for (int i = 0; i < highWater; i++)
            {
                if (!alive[i]) continue;
                Vector3 d = p - pos[i];
                float d2 = d.sqrMagnitude;
                if (!homing[i] && d2 < range2) homing[i] = true;
                if (!homing[i]) continue;
                speed[i] = Mathf.Min(40f, speed[i] + 45f * dt);
                float dist = Mathf.Sqrt(d2);
                if (dist < 0.9f || dist < speed[i] * dt)
                {
                    alive[i] = false;
                    free.Push(i);
                    count--;
                    Audio.Play(kind[i] == PickupKind.Gold ? Sfx.Gold : Sfx.Pickup, 0.6f, 0.2f);
                    Collected?.Invoke(kind[i], value[i]);
                    if (kind[i] == PickupKind.Magnet) MagnetAll();
                    continue;
                }
                pos[i] += d / dist * speed[i] * dt;
            }
            Draw();
        }

        void Draw()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) return;
            float t = Time.time;
            DrawKind(PickupKind.Xp, 0f, 2f, xpSmall, 0.8f, t);
            DrawKind(PickupKind.Xp, 2f, 10f, xpMid, 1.1f, t);
            DrawKind(PickupKind.Xp, 10f, float.MaxValue, xpBig, 1.5f, t);
            DrawKind(PickupKind.Gold, 0f, float.MaxValue, gold, 1f, t);
            DrawKind(PickupKind.Heal, 0f, float.MaxValue, heal, 1.6f, t);
            DrawKind(PickupKind.Magnet, 0f, float.MaxValue, magnet, 1.8f, t);
        }

        void DrawKind(PickupKind k, float min, float max, Material mat, float scale, float t)
        {
            int n = 0;
            var rp = new RenderParams(mat) { shadowCastingMode = ShadowCastingMode.Off, receiveShadows = false };
            for (int i = 0; i < highWater; i++)
            {
                if (!alive[i] || kind[i] != k || value[i] < min || value[i] >= max) continue;
                float bob = Mathf.Sin(t * 3f + i) * 0.12f;
                batch[n++] = Matrix4x4.TRS(pos[i] + Vector3.up * bob, Quaternion.Euler(0f, t * 90f + i * 37f, 0f), Vector3.one * scale);
                if (n == batch.Length)
                {
                    Graphics.RenderMeshInstanced(rp, gem, 0, batch, n);
                    n = 0;
                }
            }
            if (n > 0) Graphics.RenderMeshInstanced(rp, gem, 0, batch, n);
        }

        /// <summary>The nearest XP gem within range, for the autoplay bot.</summary>
        public bool Nearest(Vector3 p, float range, out Vector3 at)
        {
            at = default;
            float best = range * range;
            bool found = false;
            for (int i = 0; i < highWater; i++)
            {
                if (!alive[i] || homing[i] || kind[i] == PickupKind.Magnet) continue;
                Vector3 d = pos[i] - p;
                d.y = 0f;
                if (d.sqrMagnitude < best) { best = d.sqrMagnitude; at = pos[i]; found = true; }
            }
            return found;
        }

        /// <summary>Total XP lying on the map, for tests.</summary>
        public float TotalValue(PickupKind k)
        {
            float sum = 0f;
            for (int i = 0; i < highWater; i++) if (alive[i] && kind[i] == k) sum += value[i];
            return sum;
        }
    }
}
