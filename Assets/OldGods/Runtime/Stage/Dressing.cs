using System.Collections.Generic;
using OldGods.Rules;
using UnityEngine;
using UnityEngine.Rendering;

namespace OldGods.Runtime
{
    /// <summary>
    /// Builds a layout's pieces into the world: one mesh and collider per landmark (and one
    /// for the road and wall), plus a glowing mesh for embers and fire.
    /// </summary>
    public static class Dressing
    {
        public static void Build(HeightField field, LevelLayout layout, DressingColors colors, Material solid, Color glowColor, Transform parent)
        {
            var groups = new SortedDictionary<int, List<Piece>>();
            foreach (var p in layout.Pieces)
            {
                if (!groups.TryGetValue(p.Landmark, out var list)) groups[p.Landmark] = list = new List<Piece>();
                list.Add(p);
            }
            foreach (var kv in groups)
            {
                string name = kv.Key < 0 ? "Old Road" : "Landmark " + layout.Landmarks[kv.Key].Kind;
                var k = new MeshKit();
                var glow = new MeshKit();
                foreach (var p in kv.Value)
                {
                    float y = BaseHeight(field, p) + p.Lift;
                    var m = Matrix4x4.TRS(new Vector3(p.X, y, p.Z), Quaternion.Euler(0f, p.Yaw, 0f) * Quaternion.Euler(p.Lean, 0f, 0f), Vector3.one);
                    k.Placement = m;
                    glow.Placement = m;
                    LandmarkModels.Draw(k, glow, p, colors);
                }
                k.Placement = Matrix4x4.identity;
                glow.Placement = Matrix4x4.identity;

                var go = new GameObject(name);
                go.transform.SetParent(parent, false);
                go.layer = Layers.Ground;
                if (k.VertexCount > 0)
                {
                    var mesh = k.Build(name);
                    go.AddComponent<MeshFilter>().sharedMesh = mesh;
                    var mr = go.AddComponent<MeshRenderer>();
                    mr.sharedMaterial = solid;
                    mr.shadowCastingMode = ShadowCastingMode.On;
                    go.AddComponent<MeshCollider>().sharedMesh = mesh;
                }
                if (glow.VertexCount > 0)
                {
                    var g = new GameObject("Embers");
                    g.transform.SetParent(go.transform, false);
                    g.AddComponent<MeshFilter>().sharedMesh = glow.Build(name + " Embers");
                    var gr = g.AddComponent<MeshRenderer>();
                    gr.sharedMaterial = Fx.Glow(glowColor);
                    gr.shadowCastingMode = ShadowCastingMode.Off;
                }
            }
        }

        /// <summary>The lowest ground under a piece's footprint, a little sunk, so nothing floats on a slope.</summary>
        static float BaseHeight(HeightField field, Piece p)
        {
            float yaw = p.Yaw * Mathf.Deg2Rad;
            float hx = p.Width * 0.45f, hz = p.Depth * 0.45f;
            var right = new Vector2(Mathf.Cos(yaw), -Mathf.Sin(yaw));
            var fwd = new Vector2(Mathf.Sin(yaw), Mathf.Cos(yaw));
            float low = field.Sample(p.X, p.Z);
            for (int i = 0; i < 4; i++)
            {
                var o = right * ((i & 1) == 0 ? -hx : hx) + fwd * ((i & 2) == 0 ? -hz : hz);
                low = Mathf.Min(low, field.Sample(p.X + o.x, p.Z + o.y));
            }
            return low - 0.12f;
        }
    }
}
