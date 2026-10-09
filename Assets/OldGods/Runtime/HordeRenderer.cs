using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;

namespace OldGods.Runtime
{
    /// <summary>
    /// Draws the horde: one RenderMeshPrimitives call per enemy type, reading a
    /// structured buffer of instances (OldGods/HordeInstanced shader).
    /// </summary>
    public sealed class HordeRenderer : MonoBehaviour
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct HordeInstance
        {
            public Vector3 Position;
            public float Yaw;
            public float Scale;
            public float Phase;
            public float Flash;
            public float Tint;
        }

        sealed class TypeDraw
        {
            public Mesh Mesh;
            public Material Material;
            public MaterialPropertyBlock Props;
            public GraphicsBuffer Buffer;
            public HordeInstance[] Cpu;
            public int Count;
            public float Scale;
        }

        public Material HordeMaterial;
        public bool CastShadows = true;

        static readonly int InstancesId = Shader.PropertyToID("_Instances");
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        readonly List<TypeDraw> draws = new List<TypeDraw>();
        bool canDraw;

        public int DrawCalls { get; private set; }

        void Awake()
        {
            canDraw = SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null && SystemInfo.supportsComputeShaders;
            if (HordeMaterial == null)
            {
                var shader = Shader.Find("OldGods/HordeInstanced");
                if (shader != null) HordeMaterial = new Material(shader);
            }
        }

        public void AddType(Mesh mesh, Color color, float scale)
        {
            var d = new TypeDraw { Mesh = mesh, Scale = scale, Props = new MaterialPropertyBlock() };
            if (HordeMaterial != null)
            {
                d.Material = new Material(HordeMaterial) { name = $"Horde_{mesh.name}" };
                d.Material.SetColor(BaseColorId, color);
            }
            draws.Add(d);
        }

        void EnsureCapacity(TypeDraw d, int capacity)
        {
            if (d.Cpu != null && d.Cpu.Length >= capacity) return;
            int size = Mathf.NextPowerOfTwo(Mathf.Max(64, capacity));
            d.Cpu = new HordeInstance[size];
            if (canDraw)
            {
                d.Buffer?.Release();
                d.Buffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, size, Marshal.SizeOf<HordeInstance>());
                d.Props.SetBuffer(InstancesId, d.Buffer);
            }
        }

        public void Draw(HordeManager horde)
        {
            DrawCalls = 0;
            if (draws.Count == 0) return;
            foreach (var d in draws)
            {
                d.Count = 0;
                EnsureCapacity(d, horde.Capacity);
            }

            for (int i = 0; i < horde.HighWater; i++)
            {
                if (!horde.Alive[i]) continue;
                var d = draws[horde.TypeIndex[i]];
                d.Cpu[d.Count++] = new HordeInstance
                {
                    Position = new Vector3(horde.X[i], horde.Y[i], horde.Z[i]),
                    Yaw = horde.Yaw[i],
                    Scale = d.Scale,
                    Phase = horde.Phase[i],
                    Flash = horde.Flash[i],
                    Tint = 0.88f + 0.24f * Frac(i * 0.6180339f),
                };
            }

            if (!canDraw) return;
            foreach (var d in draws)
            {
                if (d.Count == 0 || d.Material == null) continue;
                d.Buffer.SetData(d.Cpu, 0, 0, d.Count);
                var rp = new RenderParams(d.Material)
                {
                    worldBounds = new Bounds(Vector3.zero, new Vector3(4000f, 1000f, 4000f)),
                    matProps = d.Props,
                    shadowCastingMode = CastShadows ? ShadowCastingMode.On : ShadowCastingMode.Off,
                    receiveShadows = true,
                    layer = gameObject.layer,
                };
                Graphics.RenderMeshPrimitives(rp, d.Mesh, 0, d.Count);
                DrawCalls++;
            }
        }

        static float Frac(float v) => v - Mathf.Floor(v);

        void OnDestroy()
        {
            foreach (var d in draws)
            {
                d.Buffer?.Release();
                d.Buffer = null;
                if (d.Material != null) Destroy(d.Material);
            }
            draws.Clear();
        }
    }
}
