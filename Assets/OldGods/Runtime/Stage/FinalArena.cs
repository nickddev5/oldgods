using System.Collections;
using System.Collections.Generic;
using OldGods.Rules;
using Unity.Cinemachine;
using UnityEngine;

namespace OldGods.Runtime
{
    /// <summary>
    /// The Last Test's arena: the empty throne on its dais, the six old gods in stone
    /// behind it, and the construct that guards it. Also plays the ending.
    /// </summary>
    public sealed class FinalArena : MonoBehaviour
    {
        public static FinalArena Instance { get; private set; }

        public Transform Throne { get; private set; }
        public Vector3 Seat => Throne.TransformPoint(new Vector3(0f, 2.05f, -0.6f));
        readonly List<Transform> statues = new List<Transform>();
        public IReadOnlyList<Transform> Statues => statues;

        static Mesh throneMesh;

        public static Mesh ThroneMesh()
        {
            if (throneMesh != null) return throneMesh;
            var k = new MeshKit();
            var stone = new Color(0.8f, 0.78f, 0.74f);
            var gold = new Color(1f, 0.82f, 0.45f);
            k.Box(new Vector3(0f, 0.25f, 0f), new Vector3(6f, 0.5f, 5f), stone);
            k.Box(new Vector3(0f, 0.75f, -0.3f), new Vector3(4.5f, 0.5f, 3.6f), stone);
            k.Box(new Vector3(0f, 1.25f, -0.6f), new Vector3(3f, 0.5f, 2.4f), stone);
            k.Box(new Vector3(0f, 1.75f, -0.7f), new Vector3(1.6f, 0.5f, 1.4f), gold);
            k.Box(new Vector3(0f, 3.1f, -1.3f), new Vector3(1.6f, 2.4f, 0.25f), gold, 0.8f);
            k.Box(new Vector3(-0.75f, 2.25f, -0.7f), new Vector3(0.15f, 0.5f, 1.3f), gold);
            k.Box(new Vector3(0.75f, 2.25f, -0.7f), new Vector3(0.15f, 0.5f, 1.3f), gold);
            k.Gem(new Vector3(0f, 4.55f, -1.3f), new Vector3(0.3f, 0.35f, 0.3f), new Color(1f, 0.95f, 0.8f));
            throneMesh = k.Build("Throne");
            return throneMesh;
        }

        public static FinalArena Build(RunController run, Transform parent)
        {
            var go = new GameObject("Final Arena");
            go.transform.SetParent(parent, false);
            var arena = go.AddComponent<FinalArena>();
            Instance = arena;
            var f = Ground.Field;
            float z = f.MaxZ - run.Biome.Terrain.RimWidth - 14f;
            var throne = WorldBuilder.CreateProp("Throne", ThroneMesh(), WorldBuilder.Tinted(run.Assets.LowPoly, Color.white),
                Ground.Snap(new Vector3(0f, 0f, z)), Quaternion.Euler(0f, 180f, 0f), Vector3.one * 1.6f, go.transform, true);
            arena.Throne = throne.transform;

            var stone = WorldBuilder.Tinted(run.Assets.LowPoly, new Color(0.62f, 0.6f, 0.58f));
            for (int i = 0; i < 6; i++)
            {
                float a = Mathf.Lerp(-70f, 70f, i / 5f) * Mathf.Deg2Rad;
                var at = Ground.Snap(throne.transform.position + new Vector3(Mathf.Sin(a) * 16f, 0f, Mathf.Cos(a) * 9f + 2f));
                WorldBuilder.CreateProp("Plinth", Fx.Column(), stone, at, Quaternion.identity, new Vector3(1.4f, 1.2f, 1.4f), go.transform, true);
                var statue = WorldBuilder.CreateProp($"Old God {i + 1}", GodModels.Statue((GodLook)i), stone, at + Vector3.up * 1.2f,
                    Quaternion.LookRotation(throne.transform.position - at), Vector3.one * 2.4f, go.transform, false);
                arena.statues.Add(statue.transform);
            }
            return arena;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Elias takes the throne: he climbs, sits, and the old gods' power passes into him.</summary>
        public IEnumerator EliasEnding(RunController run, IList<string> lines)
        {
            var cam = MakeEndingCamera(run);
            run.Player.InputEnabled = false;
            run.PlayerHealth.Invincible = true;
            yield return new WaitForSeconds(1f);

            Vector3 start = run.Player.transform.position;
            Vector3 seat = Seat;
            Vector3 foot = Ground.Snap(Throne.position + Throne.forward * 5f);
            yield return Walk(run.Player, start, foot, 2.5f);
            yield return Walk(run.Player, foot, seat, 1.5f);
            run.Player.Visual.rotation = Quaternion.LookRotation(Throne.forward);

            int line = 0;
            if (lines.Count > line) run.Announce(lines[line++], null);
            yield return new WaitForSeconds(2f);
            foreach (var statue in statues)
            {
                Effects.Bolt(statue.position + Vector3.up * 3f, seat + Vector3.up * 1.2f, new Color(1.8f, 1.5f, 0.8f), 0.35f, 1.2f, 6);
                Effects.Burst(PlaceholderMeshes.XpGem(), statue.position + Vector3.up * 3f, Quaternion.identity, Vector3.one, Vector3.one * 4f, new Color(1.8f, 1.5f, 0.8f, 0.9f), 1f);
                StartCoroutine(Vanish(statue, 1.2f));
                yield return new WaitForSeconds(0.9f);
            }
            if (lines.Count > line) run.Announce(lines[line++], null);
            Effects.Burst(Fx.Column(), seat, Quaternion.identity, new Vector3(1f, 1f, 1f), new Vector3(3f, 40f, 3f), new Color(2f, 1.8f, 1.2f, 0.9f), 2.5f);
            yield return new WaitForSeconds(3.5f);
            while (line < lines.Count)
            {
                run.Announce(lines[line++], null);
                yield return new WaitForSeconds(3.5f);
            }
            Destroy(cam.gameObject);
        }

        /// <summary>Any other god: the construct falls, but the throne is not theirs.</summary>
        public IEnumerator RefusedEnding(RunController run, IList<string> lines)
        {
            var cam = MakeEndingCamera(run);
            run.Player.InputEnabled = false;
            run.PlayerHealth.Invincible = true;
            yield return Walk(run.Player, run.Player.transform.position, Ground.Snap(Throne.position + Throne.forward * 7f), 2.5f);
            foreach (var l in lines)
            {
                run.Announce(l, null);
                yield return new WaitForSeconds(3.5f);
            }
            Destroy(cam.gameObject);
        }

        CinemachineCamera MakeEndingCamera(RunController run)
        {
            var go = new GameObject("Ending Camera");
            go.transform.SetParent(transform, false);
            go.transform.position = Throne.position + Throne.forward * 18f + Vector3.up * 8f;
            go.transform.LookAt(Throne.position + Vector3.up * 3f);
            var cam = go.AddComponent<CinemachineCamera>();
            cam.Priority = 100;
            cam.Lens = new LensSettings { FieldOfView = 50f, NearClipPlane = 0.2f, FarClipPlane = 400f };
            return cam;
        }

        static IEnumerator Walk(PlayerMotor player, Vector3 from, Vector3 to, float seconds)
        {
            float t = 0f;
            Vector3 dir = to - from;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.01f) player.Visual.rotation = Quaternion.LookRotation(dir);
            while (t < seconds)
            {
                t += Time.deltaTime;
                player.Teleport(Vector3.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / seconds)));
                yield return null;
            }
        }

        static IEnumerator Vanish(Transform statue, float seconds)
        {
            Vector3 s0 = statue.localScale;
            float t = 0f;
            while (t < seconds)
            {
                t += Time.deltaTime;
                statue.localScale = Vector3.Lerp(s0, new Vector3(0f, s0.y * 1.3f, 0f), t / seconds);
                yield return null;
            }
            statue.gameObject.SetActive(false);
        }
    }
}
