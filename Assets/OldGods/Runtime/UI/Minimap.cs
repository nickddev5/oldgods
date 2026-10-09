using System.Collections.Generic;
using OldGods.Rules;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OldGods.Runtime
{
    /// <summary>
    /// Corner map: the ground drawn once from the height field, the player's position and
    /// heading, and markers for features once the player has been near them.
    /// </summary>
    public sealed class Minimap : MonoBehaviour
    {
        const float Size = 230f;

        RectTransform frame, playerDot;
        RawImage image;
        Texture2D texture;
        readonly Dictionary<Interactable, Image> markers = new Dictionary<Interactable, Image>();
        Image bossDot;
        HeightField field;

        public static Minimap Create(RectTransform hudRoot)
        {
            var bg = UiKit.Panel(hudRoot, "Minimap", new Color(0f, 0f, 0f, 0.5f));
            UiKit.Anchor(bg, new Vector2(1f, 1f), new Vector2(-20f, -60f), new Vector2(Size + 8f, Size + 8f));
            var map = bg.gameObject.AddComponent<Minimap>();
            map.frame = bg;
            var img = new GameObject("Ground", typeof(RectTransform), typeof(RawImage));
            img.transform.SetParent(bg, false);
            map.image = img.GetComponent<RawImage>();
            UiKit.Stretch(map.image.rectTransform, 4f);
            map.playerDot = UiKit.Panel(bg, "Player", Color.white);
            map.playerDot.sizeDelta = new Vector2(9f, 9f);
            map.bossDot = UiKit.Panel(bg, "Boss", new Color(1f, 0.3f, 0.2f)).GetComponent<Image>();
            map.bossDot.rectTransform.sizeDelta = new Vector2(12f, 12f);
            map.bossDot.gameObject.SetActive(false);
            return map;
        }

        /// <summary>Redraws the ground for a new map.</summary>
        public void SetGround(HeightField f, GroundPalette palette, float hillHeight)
        {
            field = f;
            int res = 128;
            if (texture == null) texture = new Texture2D(res, res, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var px = new Color[res * res];
            for (int y = 0; y < res; y++)
                for (int x = 0; x < res; x++)
                {
                    float wx = f.MinX + (x + 0.5f) / res * f.Size, wz = f.MinZ + (y + 0.5f) / res * f.Size;
                    float h = f.Sample(wx, wz);
                    Color c;
                    if (!TerrainGenerator.InPlayableArea(f, wx, wz, Ground.RimWidth * 0.7f)) c = palette.Rim;
                    else if (f.SlopeDegrees(wx, wz) > palette.CliffSlope) c = palette.Cliff;
                    else c = Color.Lerp(palette.Low, palette.High, Mathf.Clamp01(h / Mathf.Max(1f, hillHeight)));
                    px[y * res + x] = c * 0.85f;
                }
            texture.SetPixels(px);
            texture.Apply();
            image.texture = texture;
            foreach (var m in markers.Values) if (m != null) Destroy(m.gameObject);
            markers.Clear();
        }

        Vector2 ToMap(Vector3 world)
        {
            float u = (world.x - field.MinX) / field.Size - 0.5f;
            float v = (world.z - field.MinZ) / field.Size - 0.5f;
            return new Vector2(u * Size, v * Size);
        }

        void LateUpdate()
        {
            if (field == null) return;
            var run = RunController.Instance;
            if (run == null || run.Player == null) return;
            playerDot.anchoredPosition = ToMap(run.Player.transform.position);
            var f = run.Player.Facing;
            playerDot.localRotation = Quaternion.Euler(0f, 0f, -Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg + 45f);

            foreach (var it in Interactable.All)
            {
                if (it == null || !it.Discovered) continue;
                if (it.MapLabel == null)
                {
                    if (markers.TryGetValue(it, out var gone) && gone != null) gone.gameObject.SetActive(false);
                    continue;
                }
                if (!markers.TryGetValue(it, out var dot) || dot == null)
                {
                    dot = UiKit.Panel(frame, it.MapLabel, it.MapColor).GetComponent<Image>();
                    dot.rectTransform.sizeDelta = new Vector2(10f, 10f);
                    dot.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
                    markers[it] = dot;
                }
                dot.rectTransform.anchoredPosition = ToMap(it.transform.position);
                dot.color = it.CanUse ? it.MapColor : it.MapColor * 0.45f;
            }
            var stale = new List<Interactable>();
            foreach (var kv in markers) if (kv.Key == null) stale.Add(kv.Key);
            foreach (var k in stale) { if (markers[k] != null) Destroy(markers[k].gameObject); markers.Remove(k); }

            var boss = BossController.Active;
            bossDot.gameObject.SetActive(boss != null);
            if (boss != null) bossDot.rectTransform.anchoredPosition = ToMap(boss.transform.position);
        }
    }
}
