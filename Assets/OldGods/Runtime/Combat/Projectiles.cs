using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace OldGods.Runtime
{
    /// <summary>What a projectile does when it hits, fixed when it is fired.</summary>
    public struct HitInfo
    {
        public float Damage;
        public float CritChance;
        public float CritMultiplier;
        public float Knockback;
        public float SlowSeconds;
        public int Source; // index into the stats list, for damage-by-weapon tracking
    }

    /// <summary>All player projectiles as arrays; hits are found through the horde's spatial hash.</summary>
    public sealed class Projectiles : MonoBehaviour
    {
        public static Projectiles Instance { get; private set; }

        const int Capacity = 1024;
        const int HitMemory = 12;

        readonly Vector3[] pos = new Vector3[Capacity];
        readonly Vector3[] dir = new Vector3[Capacity];
        readonly float[] speed = new float[Capacity];
        readonly float[] life = new float[Capacity];
        readonly float[] radius = new float[Capacity];
        readonly Vector3[] scale = new Vector3[Capacity];
        readonly int[] pierce = new int[Capacity];
        readonly int[] look = new int[Capacity];
        readonly HitInfo[] hit = new HitInfo[Capacity];
        readonly bool[] alive = new bool[Capacity];
        readonly int[] hits = new int[Capacity * HitMemory];
        readonly int[] hitCount = new int[Capacity];
        readonly Stack<int> free = new Stack<int>();
        readonly List<(Mesh mesh, Material mat)> looks = new List<(Mesh, Material)>();
        readonly int[] query = new int[64];
        Matrix4x4[] batch = new Matrix4x4[1023];
        int highWater;

        public System.Func<int, HitInfo, Vector3, bool> OnHit;

        void Awake()
        {
            Instance = this;
            for (int i = Capacity - 1; i >= 0; i--) free.Push(i);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public int RegisterLook(Mesh mesh, Material mat)
        {
            looks.Add((mesh, mat));
            return looks.Count - 1;
        }

        public void Fire(Vector3 from, Vector3 direction, float spd, float lifetime, float hitRadius, Vector3 visualScale, int pierceCount, int lookIndex, HitInfo info)
        {
            if (free.Count == 0) return;
            int i = free.Pop();
            pos[i] = from;
            direction.y = 0f;
            dir[i] = direction.sqrMagnitude > 1e-6f ? direction.normalized : Vector3.forward;
            speed[i] = spd;
            life[i] = lifetime;
            radius[i] = hitRadius;
            scale[i] = visualScale;
            pierce[i] = pierceCount;
            look[i] = lookIndex;
            hit[i] = info;
            hitCount[i] = 0;
            alive[i] = true;
            if (i + 1 > highWater) highWater = i + 1;
        }

        public void Clear()
        {
            for (int i = 0; i < highWater; i++)
                if (alive[i]) Kill(i);
        }

        void Kill(int i)
        {
            alive[i] = false;
            free.Push(i);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            var horde = HordeManager.Instance;
            for (int i = 0; i < highWater; i++)
            {
                if (!alive[i]) continue;
                life[i] -= dt;
                if (life[i] <= 0f) { Kill(i); continue; }
                pos[i] += dir[i] * speed[i] * dt;
                // Ride over hills: keep at least a little above the ground.
                float g = Ground.Height(pos[i].x, pos[i].z) + 0.9f;
                if (pos[i].y < g) pos[i].y = Mathf.Lerp(pos[i].y, g, 1f - Mathf.Exp(-12f * dt));
                if (horde == null) continue;

                int n = horde.QueryCircle(pos[i], radius[i], query);
                for (int k = 0; k < n && alive[i]; k++)
                {
                    int e = query[k];
                    if (AlreadyHit(i, e)) continue;
                    Remember(i, e);
                    OnHit?.Invoke(e, hit[i], dir[i]);
                    if (pierce[i]-- <= 0) Kill(i);
                }
            }
            Draw();
        }

        bool AlreadyHit(int i, int enemy)
        {
            int n = Mathf.Min(hitCount[i], HitMemory);
            int b = i * HitMemory;
            for (int k = 0; k < n; k++) if (hits[b + k] == enemy) return true;
            return false;
        }

        void Remember(int i, int enemy)
        {
            hits[i * HitMemory + hitCount[i] % HitMemory] = enemy;
            hitCount[i]++;
        }

        void Draw()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) return;
            for (int l = 0; l < looks.Count; l++)
            {
                int n = 0;
                var rp = new RenderParams(looks[l].mat) { shadowCastingMode = ShadowCastingMode.Off };
                for (int i = 0; i < highWater; i++)
                {
                    if (!alive[i] || look[i] != l) continue;
                    batch[n++] = Matrix4x4.TRS(pos[i], Quaternion.LookRotation(dir[i]), scale[i]);
                    if (n == batch.Length) { Graphics.RenderMeshInstanced(rp, looks[l].mesh, 0, batch, n); n = 0; }
                }
                if (n > 0) Graphics.RenderMeshInstanced(rp, looks[l].mesh, 0, batch, n);
            }
        }
    }
}
