using TMPro;
using UnityEngine;

namespace OldGods.Runtime
{
    /// <summary>Pooled floating numbers above hit enemies. Oldest is reused when the pool is full.</summary>
    public sealed class DamageNumbers : MonoBehaviour
    {
        const int PoolSize = 72;
        const float Life = 0.7f;

        static DamageNumbers instance;
        public static bool Enabled = true;

        TextMeshPro[] pool;
        float[] age;
        Vector3[] origin;
        int next;
        Camera cam;

        void Awake()
        {
            instance = this;
            pool = new TextMeshPro[PoolSize];
            age = new float[PoolSize];
            origin = new Vector3[PoolSize];
            for (int i = 0; i < PoolSize; i++)
            {
                var go = new GameObject("Num");
                go.transform.SetParent(transform, false);
                var t = go.AddComponent<TextMeshPro>();
                t.alignment = TextAlignmentOptions.Center;
                t.fontSize = 3f;
                t.fontStyle = FontStyles.Bold;
                t.outlineWidth = 0.25f;
                t.outlineColor = new Color32(0, 0, 0, 200);
                t.textWrappingMode = TextWrappingModes.NoWrap;
                go.SetActive(false);
                pool[i] = t;
                age[i] = Life;
            }
        }

        void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        public static void Show(Vector3 position, float amount)
        {
            if (!Enabled || instance == null) return;
            int v = Mathf.Max(1, Mathf.RoundToInt(amount));
            Color c = amount >= 50f ? new Color(1f, 0.55f, 0.2f) : (amount >= 20f ? new Color(1f, 0.85f, 0.3f) : Color.white);
            instance.Spawn(position, v.ToString(), c);
        }

        public static void ShowText(Vector3 position, string text, Color color)
        {
            if (!Enabled || instance == null) return;
            instance.Spawn(position, text, color);
        }

        void Spawn(Vector3 position, string text, Color color)
        {
            int i = next;
            next = (next + 1) % PoolSize;
            var t = pool[i];
            t.text = text;
            t.color = color;
            origin[i] = position + new Vector3(Random.Range(-0.3f, 0.3f), 0f, Random.Range(-0.3f, 0.3f));
            age[i] = 0f;
            t.transform.position = origin[i];
            t.gameObject.SetActive(true);
        }

        void LateUpdate()
        {
            if (cam == null) cam = Camera.main;
            float dt = Time.deltaTime;
            Quaternion face = cam != null ? cam.transform.rotation : Quaternion.identity;
            for (int i = 0; i < PoolSize; i++)
            {
                if (age[i] >= Life) continue;
                age[i] += dt;
                var t = pool[i];
                if (age[i] >= Life)
                {
                    t.gameObject.SetActive(false);
                    continue;
                }
                float k = age[i] / Life;
                t.transform.position = origin[i] + Vector3.up * (k * 1.2f);
                t.transform.rotation = face;
                float s = k < 0.15f ? Mathf.Lerp(1.4f, 1f, k / 0.15f) : 1f;
                t.transform.localScale = Vector3.one * s;
                var c = t.color;
                c.a = 1f - Mathf.Max(0f, (k - 0.6f) / 0.4f);
                t.color = c;
            }
        }
    }
}
