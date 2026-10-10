using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace OldGods.Runtime
{
    /// <summary>
    /// Started by -gallery DIR. Lines up every built-in model under even light and saves close
    /// screenshots of the gods, the enemies and the bosses, front and three-quarter, then quits.
    /// For reviewing art without playing. Add -galleryTurn NAME for one god's front, side and back.
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
            // The gallery only takes pictures: keep it silent.
            AudioListener.volume = 0f;
            AudioListener.pause = true;
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
            var white = WorldBuilder.Outlined(WorldBuilder.Tinted(assets.LowPoly, Color.white));
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
            // -galleryTurn NAME: that god alone, front, side and back.
            if (System.Enum.TryParse<GodLook>(CommandLine.Value("-galleryTurn"), out var turn))
            {
                foreach (Transform t in gods) t.gameObject.SetActive(t.name == $"God {turn}");
                var x = ((int)turn - 3) * 1.1f;
                var views = new (float yaw, string name)[] { (180f, "front"), (90f, "side"), (0f, "back"), (135f, "three_quarter") };
                foreach (var v in views)
                {
                    foreach (Transform t in gods) t.rotation = Quaternion.Euler(0f, v.yaw, 0f);
                    yield return Shoot(cam, new Vector3(x, 1.2f, -4.6f), new Vector3(x, 1.0f, 0f), $"turn_{turn}_{v.name}");
                }
            }
            Destroy(gods.gameObject);

            // Poses, side on: standing, two moments of the run, the jump and the slide.
            var posed = new (float phase, float swing, float air, float slide)[] { (0f, 0.02f, 0f, 0f), (1.57f, 0.2f, 0f, 0f), (4.71f, 0.2f, 0f, 0f), (0f, 0.03f, 1f, 0f), (0f, 0.03f, 0f, 1f) };
            yield return Poses(cam, white, GodLook.Storm, posed, 90f, "poses");
            if (System.Enum.TryParse<GodLook>(CommandLine.Value("-galleryTurn"), out var walker))
            {
                // A full stride of that god at run swing, side on and from the front quarter, then the jump and slide.
                var stride = new (float, float, float, float)[] { (0f, 0.2f, 0f, 0f), (1.05f, 0.2f, 0f, 0f), (2.1f, 0.2f, 0f, 0f), (3.14f, 0.2f, 0f, 0f), (4.19f, 0.2f, 0f, 0f) };
                yield return Poses(cam, white, walker, stride, 90f, $"walk_{walker}_side");
                yield return Poses(cam, white, walker, stride, 150f, $"walk_{walker}_front");
                yield return Poses(cam, white, walker, posed, 90f, $"poses_{walker}");
            }

            // Capes, side on: hanging, walking, running, and swung to one side, for each caped god.
            var swings = new[] { new Vector4(0f, 0f, 0f, 0f), new Vector4(0.3f, 0f, 0.4f, 0f), new Vector4(0.7f, 0f, 1f, 0f), new Vector4(0.3f, 0.5f, 0.5f, 0f) };
            foreach (var look in new[] { GodLook.Storm, GodLook.Elias, GodLook.Beast })
            {
                var capes = new GameObject("Capes").transform;
                for (int i = 0; i < swings.Length; i++)
                {
                    var go = WorldBuilder.CreateProp($"Cape {look} {i}", GodModels.Get(look), white, new Vector3((i - 1.5f) * 1.3f, 0f, 0f), Quaternion.Euler(0f, 90f, 0f), Vector3.one, capes, false);
                    var block = new MaterialPropertyBlock();
                    block.SetFloat("_WalkSwing", swings[i].x > 0.6f ? 0.2f : swings[i].x > 0f ? 0.1f : 0.02f);
                    block.SetFloat("_AnimPhase", 1.57f);
                    block.SetVector("_CapeSwing", swings[i]);
                    go.GetComponent<MeshRenderer>().SetPropertyBlock(block);
                }
                yield return Shoot(cam, new Vector3(0f, 1.1f, -8f), new Vector3(0f, 0.9f, 0f), $"capes_{look}");
                Destroy(capes.gameObject);
                yield return null;
            }

            // Each god's own slide: all seven side by side, then each one sliding past.
            if (Effects.Instance == null) new GameObject("Effects").AddComponent<Effects>();
            yield return Dodges(cam, white);

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

        /// <summary>
        /// The gods in their slide poses (DodgeLook) with what they ride on: a row of all seven, then
        /// each one sliding past the camera with its trail and cape moving, side on and from the front quarter.
        /// </summary>
        IEnumerator Dodges(Camera cam, Material white)
        {
            const float speed = 12f;
            var row = new GameObject("Dodges").transform;
            var gods = new List<(Transform root, Transform visual, DodgeLook look)>();
            for (int i = 0; i < 7; i++)
            {
                var look = (GodLook)i;
                var root = new GameObject($"Dodge {look}").transform;
                root.SetParent(row, false);
                root.position = new Vector3((i - 3) * 2.3f, 0f, 0f);
                var visual = WorldBuilder.CreateProp($"Dodge {look} model", GodModels.Get(look), white, root.position, Quaternion.identity, Vector3.one, root, false).transform;
                var dodge = visual.gameObject.AddComponent<DodgeLook>();
                dodge.Style = GodModels.Dodge(look);
                dodge.ManualBlend = 1f;
                dodge.ManualVelocity = new Vector3(speed, 0f, 0f);
                visual.gameObject.AddComponent<CapeSway>();
                gods.Add((root, visual, dodge));
            }
            yield return null;
            float phase = 0f;
            void Pose(float dt)
            {
                phase += dt * 9f;
                foreach (var g in gods)
                {
                    var p = g.look.Pose;
                    g.visual.localPosition = new Vector3(0f, p.Height, 0f);
                    g.visual.localRotation = Quaternion.Euler(p.Pitch, 90f + p.Yaw, p.Roll);
                    var r = g.visual.GetComponent<MeshRenderer>();
                    var block = new MaterialPropertyBlock();
                    r.GetPropertyBlock(block);
                    block.SetFloat("_AnimPhase", phase);
                    block.SetFloat("_WalkSwing", 0.03f);
                    r.SetPropertyBlock(block);
                }
            }
            Pose(0f);
            yield return null;
            Pose(0f);
            yield return Shoot(cam, new Vector3(0f, 2.2f, -22f), new Vector3(0f, 0.8f, 0f), "dodges");

            // One at a time, sliding along x past the camera; the shot is taken as it passes x = 0.
            foreach (var g in gods)
            {
                foreach (var o in gods) o.root.gameObject.SetActive(o.root == g.root);
                string name = g.root.name.Substring(6);
                for (int pass = 0; pass < 2; pass++)
                {
                    g.root.position = new Vector3(-speed * 0.9f, 0f, 0f);
                    while (g.root.position.x < 0f)
                    {
                        float dt = Mathf.Min(Time.deltaTime, 1f / 30f);
                        g.root.position += new Vector3(speed * dt, 0f, 0f);
                        Pose(dt);
                        yield return null;
                    }
                    var at = g.root.position;
                    if (pass == 0) yield return Shoot(cam, at + new Vector3(0f, 1.3f, -6f), at + new Vector3(-0.4f, 0.9f, 0f), $"dodge_{name}_side");
                    else yield return Shoot(cam, at + new Vector3(3.9f, 1.8f, -3.9f), at + new Vector3(-0.3f, 0.85f, 0f), $"dodge_{name}_front");
                }
                g.root.gameObject.SetActive(false);
            }
            Destroy(row.gameObject);
            yield return new WaitForSecondsRealtime(1f);
        }

        /// <summary>A row of one god in shader poses (phase, swing, air, slide), turned to yaw, then a screenshot.</summary>
        IEnumerator Poses(Camera cam, Material white, GodLook look, (float phase, float swing, float air, float slide)[] posed, float yaw, string name)
        {
            var poses = new GameObject("Poses").transform;
            for (int i = 0; i < posed.Length; i++)
            {
                var go = WorldBuilder.CreateProp($"Pose {i}", GodModels.Get(look), white, new Vector3((i - 2) * 1.3f, posed[i].slide > 0f ? -0.32f : 0f, 0f), Quaternion.Euler(posed[i].slide > 0f ? -16f : 0f, yaw, 0f), Vector3.one, poses, false);
                var block = new MaterialPropertyBlock();
                block.SetFloat("_AnimPhase", posed[i].phase);
                block.SetFloat("_WalkSwing", posed[i].swing);
                block.SetFloat("_AirPose", posed[i].air);
                block.SetFloat("_SlidePose", posed[i].slide);
                go.GetComponent<MeshRenderer>().SetPropertyBlock(block);
            }
            yield return Shoot(cam, new Vector3(0f, 1.1f, -9f), new Vector3(0f, 0.9f, 0f), name);
            Destroy(poses.gameObject);
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
