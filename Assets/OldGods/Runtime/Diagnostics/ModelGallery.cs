using System.Collections;
using System.IO;
using UnityEngine;

namespace OldGods.Runtime
{
    /// <summary>
    /// Started by -gallery DIR. Lines up every built-in model under even light and saves close
    /// screenshots of the gods, the enemies and the bosses, front and three-quarter, then quits.
    /// For reviewing art without playing.
    /// </summary>
    public sealed class ModelGallery : MonoBehaviour
    {
        string dir;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            string d = CommandLine.Value("-gallery");
            if (string.IsNullOrEmpty(d) || FindAnyObjectByType<ModelGallery>() != null) return;
            Application.runInBackground = true;
            var go = new GameObject("Model Gallery");
            DontDestroyOnLoad(go);
            go.AddComponent<ModelGallery>().dir = d;
        }

        IEnumerator Start()
        {
            Directory.CreateDirectory(dir);
            yield return null;
            // Clear whatever scene loaded and build a studio.
            foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
                if (root != gameObject) Destroy(root);
            yield return null;
            var assets = GameAssets.Load();
            Fx.Init(assets);
            var white = WorldBuilder.Tinted(assets.LowPoly, Color.white);
            var floorMat = WorldBuilder.Tinted(assets.LowPoly, new Color(0.42f, 0.42f, 0.44f));

            var camGo = new GameObject("Gallery Camera");
            var cam = camGo.AddComponent<Camera>();
            camGo.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            cam.fieldOfView = 30f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.62f, 0.66f, 0.7f);
            WorldBuilder.CreateSun(null, new Color(1f, 0.96f, 0.9f), 1.35f, new Vector3(40f, 150f, 0f));
            var fill = new GameObject("Fill").AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.intensity = 0.45f;
            fill.transform.rotation = Quaternion.Euler(20f, -30f, 0f);
            WorldBuilder.SetAtmosphere(new Color(0.75f, 0.78f, 0.85f), new Color(0.6f, 0.6f, 0.6f), new Color(0.35f, 0.33f, 0.3f), new Color(0.62f, 0.66f, 0.7f), 200f, 400f);
            WorldBuilder.CreateProp("Floor", Fx.Disc(32), floorMat, Vector3.zero, Quaternion.identity, new Vector3(60f, 1f, 60f), null, false);

            var gods = new GameObject("Gods").transform;
            for (int i = 0; i < 7; i++)
                WorldBuilder.CreateProp($"God {(GodLook)i}", GodModels.Get((GodLook)i), white, new Vector3((i - 3) * 1.1f, 0f, 0f), Quaternion.Euler(0f, 180f, 0f), Vector3.one, gods, false);
            yield return Shoot(cam, new Vector3(0f, 1.2f, -10.5f), new Vector3(0f, 0.95f, 0f), "gods_front");
            foreach (Transform t in gods) t.rotation = Quaternion.Euler(0f, 145f, 0f);
            yield return Shoot(cam, new Vector3(0f, 1.2f, -10.5f), new Vector3(0f, 0.95f, 0f), "gods_turned");
            for (int i = 0; i < 7; i++)
            {
                foreach (Transform t in gods) t.rotation = Quaternion.Euler(0f, 160f, 0f);
                var x = (i - 3) * 1.1f;
                yield return Shoot(cam, new Vector3(x, 1.25f, -3.2f), new Vector3(x, 1.0f, 0f), $"god_{(GodLook)i}");
            }
            Destroy(gods.gameObject);

            // Poses, side on: standing, two moments of the run, the jump and the slide.
            var poses = new GameObject("Poses").transform;
            var posed = new (float phase, float swing, float air, float slide)[] { (0f, 0.02f, 0f, 0f), (1.57f, 0.2f, 0f, 0f), (4.71f, 0.2f, 0f, 0f), (0f, 0.03f, 1f, 0f), (0f, 0.03f, 0f, 1f) };
            for (int i = 0; i < posed.Length; i++)
            {
                var go = WorldBuilder.CreateProp($"Pose {i}", GodModels.Get(GodLook.Storm), white, new Vector3((i - 2) * 1.3f, posed[i].slide > 0f ? -0.32f : 0f, 0f), Quaternion.Euler(posed[i].slide > 0f ? -16f : 0f, 90f, 0f), Vector3.one, poses, false);
                var block = new MaterialPropertyBlock();
                block.SetFloat("_AnimPhase", posed[i].phase);
                block.SetFloat("_WalkSwing", posed[i].swing);
                block.SetFloat("_AirPose", posed[i].air);
                block.SetFloat("_SlidePose", posed[i].slide);
                go.GetComponent<MeshRenderer>().SetPropertyBlock(block);
            }
            yield return Shoot(cam, new Vector3(0f, 1.1f, -9f), new Vector3(0f, 0.9f, 0f), "poses");
            Destroy(poses.gameObject);

            var enemies = new GameObject("Enemies").transform;
            int n = System.Enum.GetValues(typeof(EnemyModel)).Length;
            for (int i = 0; i < n; i++)
                WorldBuilder.CreateProp($"Enemy {(EnemyModel)i}", EnemyModels.Get((EnemyModel)i), white, new Vector3((i % 7 - 3) * 1.3f, 0f, (i / 7) * 2.2f), Quaternion.Euler(0f, 160f, 0f), Vector3.one, enemies, false);
            yield return Shoot(cam, new Vector3(0f, 2.4f, -11.5f), new Vector3(0f, 0.9f, 1f), "enemies");
            for (int i = 0; i < n; i++)
            {
                var at = new Vector3((i % 7 - 3) * 1.3f, 0f, (i / 7) * 2.2f);
                yield return Shoot(cam, at + new Vector3(0.4f, 1.3f, -3.1f), at + new Vector3(0f, 0.85f, 0f), $"enemy_{(EnemyModel)i}");
            }
            Destroy(enemies.gameObject);

            var bosses = new GameObject("Bosses").transform;
            for (int i = 0; i < 4; i++)
                WorldBuilder.CreateProp($"Boss {(BossModel)i}", BossModels.Get((BossModel)i), white, new Vector3((i - 1.5f) * 2.4f, 0f, 0f), Quaternion.Euler(0f, 160f, 0f), Vector3.one, bosses, false);
            yield return Shoot(cam, new Vector3(0f, 1.6f, -11f), new Vector3(0f, 1f, 0f), "bosses");
            foreach (Transform t in bosses) t.rotation = Quaternion.Euler(0f, 215f, 0f);
            yield return Shoot(cam, new Vector3(0f, 1.6f, -11f), new Vector3(0f, 1f, 0f), "bosses_turned");
            for (int i = 0; i < 4; i++)
            {
                foreach (Transform t in bosses) t.rotation = Quaternion.Euler(0f, 155f, 0f);
                var x = (i - 1.5f) * 2.4f;
                yield return Shoot(cam, new Vector3(x + 0.5f, 1.4f, -4.6f), new Vector3(x, 1.05f, 0f), $"boss_{(BossModel)i}");
            }

            yield return new WaitForSecondsRealtime(0.5f);
            Application.Quit(0);
        }

        IEnumerator Shoot(Camera cam, Vector3 from, Vector3 at, string name)
        {
            cam.transform.position = from;
            cam.transform.LookAt(at);
            yield return null;
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(dir, name + ".png"));
            yield return null;
            yield return null;
        }
    }
}
