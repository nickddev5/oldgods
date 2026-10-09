using UnityEngine;

namespace OldGods.Runtime
{
    /// <summary>The hidden boss gate. Using it wakes the stage boss beside it.</summary>
    public sealed class BossGate : Interactable
    {
        public bool Used;
        Transform glow;

        public override string Prompt => "Wake the guardian";
        public override bool CanUse => !Used && RunController.Instance != null && !RunController.Instance.BossActive;
        public override string MapLabel => "Gate";
        public override Color MapColor => new Color(1f, 0.45f, 0.3f);

        public static BossGate Create(Vector3 at, float yaw, GameAssets assets, Color accent, Transform parent)
        {
            var go = WorldBuilder.CreateProp("Boss Gate", PlaceholderMeshes.Monolith(), WorldBuilder.Tinted(assets.LowPoly, new Color(0.45f, 0.43f, 0.42f)),
                Ground.Snap(at), Quaternion.Euler(0f, yaw, 0f), Vector3.one * 1.3f, parent, true);
            var gate = go.AddComponent<BossGate>();
            gate.Range = 4.5f;
            gate.HoldSeconds = 1f;
            var g = new GameObject("Glow");
            g.transform.SetParent(go.transform, false);
            g.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            g.transform.localScale = new Vector3(2.1f, 3f, 0.15f);
            g.AddComponent<MeshFilter>().sharedMesh = Fx.Cube();
            g.AddComponent<MeshRenderer>().sharedMaterial = Fx.Glow(accent);
            gate.glow = g.transform;
            return gate;
        }

        public override void Use(PlayerCombat player)
        {
            Used = true;
            if (glow != null) glow.gameObject.SetActive(false);
            RunController.Instance.StartBoss(transform.position + transform.forward * 9f);
        }
    }

    /// <summary>The portal that opens when the stage boss falls. Using it starts the next stage.</summary>
    public sealed class NextPortal : Interactable
    {
        Transform ring;
        public override string Prompt => RunController.Instance != null && RunController.Instance.IsLastStage ? "Go to the throne" : "Enter the portal";
        public override string MapLabel => "Portal";
        public override Color MapColor => new Color(0.5f, 0.85f, 1f);

        public static NextPortal Create(Vector3 at, Transform parent)
        {
            var go = new GameObject("Next Portal");
            go.transform.SetParent(parent, false);
            go.transform.position = Ground.Snap(at);
            var portal = go.AddComponent<NextPortal>();
            portal.Range = 3.5f;
            portal.HoldSeconds = 0.4f;
            portal.Discovered = true;
            var r = new GameObject("Ring");
            r.transform.SetParent(go.transform, false);
            r.AddComponent<MeshFilter>().sharedMesh = PlaceholderMeshes.Portal();
            r.AddComponent<MeshRenderer>().sharedMaterial = Fx.Glow(new Color(0.6f, 1.2f, 2.2f));
            portal.ring = r.transform;
            var core = new GameObject("Core");
            core.transform.SetParent(go.transform, false);
            core.transform.localPosition = new Vector3(0f, 2f, 0f);
            core.transform.localScale = new Vector3(1.5f, 1.9f, 1f);
            core.AddComponent<MeshFilter>().sharedMesh = Fx.Disc(24);
            core.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            core.AddComponent<MeshRenderer>().sharedMaterial = Fx.Fade(new Color(0.5f, 0.85f, 1f, 0.45f));
            Effects.Burst(Fx.Ring(0.6f, 40), go.transform.position + Vector3.up * 0.2f, Quaternion.identity, Vector3.one, Vector3.one * 8f, new Color(0.6f, 1f, 1.6f, 0.8f), 1f);
            return portal;
        }

        void Update()
        {
            if (ring != null) ring.localRotation = Quaternion.Euler(0f, Time.time * 40f, 0f);
        }

        public override void Use(PlayerCombat player)
        {
            RunController.Instance.LeaveStage();
        }
    }
}
