using OldGods.Rules;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering;

namespace OldGods.Runtime
{
    /// <summary>Builds the pieces of a run in code: light, ground, player, camera, horde.</summary>
    public static class WorldBuilder
    {
        public static Light CreateSun(Transform parent, Color color, float intensity, Vector3 euler)
        {
            var go = new GameObject("Sun");
            go.transform.SetParent(parent, false);
            go.transform.rotation = Quaternion.Euler(euler);
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = color;
            light.intensity = intensity;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.75f;
            RenderSettings.sun = light;
            return light;
        }

        public static void SetAtmosphere(Color sky, Color equator, Color ground, Color fog, float fogStart, float fogEnd)
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = sky;
            RenderSettings.ambientEquatorColor = equator;
            RenderSettings.ambientGroundColor = ground;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = fog;
            RenderSettings.fogStartDistance = fogStart;
            RenderSettings.fogEndDistance = fogEnd;
            var cam = Camera.main;
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = fog;
                SkyDome.For(cam)?.SetColors(sky, fog, ground);
            }
            SceneLook.Ensure();
        }

        public static Material Tinted(Material baseMat, Color color, Color emission = default)
        {
            var m = new Material(baseMat) { name = baseMat.name + "_" + ColorUtility.ToHtmlStringRGB(color) };
            m.SetColor("_BaseColor", color);
            if (emission != default) m.SetColor("_EmissionColor", emission);
            return m;
        }

        /// <summary>
        /// The same material with a dark outline, for characters: the player, gods, bosses.
        /// Outlined surfaces take the pixel texture's fine grain at a smaller texel, as a
        /// character texture would, and skip the ground's broad patches.
        /// </summary>
        public static Material Outlined(Material m, float pixels = CharacterOutline)
        {
            m.SetFloat("_OutlineWidth", pixels);
            m.SetFloat("_PixelAmount", 0.2f);
            m.SetFloat("_TexelsPerMeter", 14f);
            return m;
        }

        /// <summary>Outline width of the player, gods and bosses, in pixels at 1080p-ish sizes.</summary>
        public const float CharacterOutline = 3f;

        public static GameObject CreateProp(string name, Mesh mesh, Material mat, Vector3 position, Quaternion rotation, Vector3 scale, Transform parent, bool collider)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(position, rotation);
            go.transform.localScale = scale;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            if (collider) go.AddComponent<MeshCollider>().sharedMesh = mesh;
            return go;
        }

        /// <summary>Instantiates an imported model under a parent, scaled, with shadows on and colliders removed.</summary>
        public static GameObject AttachModel(GameObject prefab, Transform parent, float scale)
        {
            var go = Object.Instantiate(prefab, parent, false);
            go.name = "Imported Model";
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one * scale;
            foreach (var c in go.GetComponentsInChildren<Collider>()) Object.Destroy(c);
            foreach (var r in go.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = ShadowCastingMode.On;
            return go;
        }

        public static PlayerMotor CreatePlayer(GameAssets assets, Vector3 position, Color robe, Transform parent, Mesh body = null, Color mark = default, GameObject prefab = null, float prefabScale = 1f, DodgeStyle dodge = DodgeStyle.Slide)
        {
            var go = new GameObject("Player");
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.layer = Layers.Player;
            go.tag = "Player";
            var cc = go.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.4f;
            cc.center = new Vector3(0f, 0.9f, 0f);
            cc.slopeLimit = 55f;
            cc.stepOffset = 0.45f;
            cc.skinWidth = 0.05f;
            go.AddComponent<PlayerHealth>();
            var motor = go.AddComponent<PlayerMotor>();
            motor.Tuning = assets.Motor;

            var visual = new GameObject("Visual");
            visual.transform.SetParent(go.transform, false);
            visual.layer = Layers.Player;
            motor.Visual = visual.transform;
            if (prefab != null)
            {
                AttachModel(prefab, visual.transform, prefabScale);
                return motor;
            }
            visual.AddComponent<MeshFilter>().sharedMesh = body != null ? body : GodModels.Get(GodLook.Storm);
            var mr = visual.AddComponent<MeshRenderer>();
            // Built-in god models carry their own colours; an untinted material shows them as authored.
            mr.sharedMaterial = Outlined(Tinted(assets.LowPoly, body != null ? Color.white : robe));
            var walk = visual.AddComponent<WalkAnimator>();
            walk.Tracked = go.transform;
            walk.Motor = motor;
            visual.AddComponent<CapeSway>();
            motor.Dodge = Dodges.Pose(dodge);
            var look = visual.AddComponent<DodgeLook>();
            look.Style = dodge;
            look.Motor = motor;
            if (mark != default)
            {
                var gem = new GameObject("Mark");
                gem.transform.SetParent(visual.transform, false);
                gem.transform.localPosition = new Vector3(0f, 1.28f, 0.2f);
                gem.transform.localScale = Vector3.one * 0.35f;
                gem.AddComponent<MeshFilter>().sharedMesh = PlaceholderMeshes.XpGem();
                gem.AddComponent<MeshRenderer>().sharedMaterial = Fx.Glow(mark);
            }
            return motor;
        }

        public static ChaseCamera CreateCamera(PlayerMotor player, Transform parent)
        {
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            camGo.transform.SetParent(parent, false);
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 60f;
            cam.nearClipPlane = 0.2f;
            cam.farClipPlane = 400f;
            camGo.AddComponent<AudioListener>();
            var brain = camGo.AddComponent<CinemachineBrain>();
            brain.UpdateMethod = CinemachineBrain.UpdateMethods.LateUpdate;
            var urp = camGo.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            urp.renderPostProcessing = true;

            var rig = new GameObject("Chase Camera");
            rig.transform.SetParent(parent, false);
            // Add every Cinemachine component before the camera enables, so its pipeline sees them.
            rig.SetActive(false);
            var vcam = rig.AddComponent<CinemachineCamera>();
            vcam.Lens = new LensSettings { FieldOfView = 60f, NearClipPlane = 0.2f, FarClipPlane = 400f };

            // Follow and look at a point at chest height.
            // The anchor is not parented to the player: it follows x and z exactly and smooths
            // height, so jumps, steps and slopes do not bounce the view.
            var anchor = new GameObject("Camera Anchor");
            anchor.transform.SetParent(parent, false);
            var follow = anchor.AddComponent<CameraAnchor>();
            follow.Target = player.transform;
            follow.Height = 1.3f;
            follow.Snap();
            vcam.Follow = anchor.transform;
            vcam.LookAt = anchor.transform;

            var orbit = rig.AddComponent<CinemachineOrbitalFollow>();
            orbit.OrbitStyle = CinemachineOrbitalFollow.OrbitStyles.Sphere;
            orbit.Radius = 9f;
            var tracker = orbit.TrackerSettings;
            tracker.BindingMode = Unity.Cinemachine.TargetTracking.BindingMode.WorldSpace;
            tracker.PositionDamping = Vector3.zero;
            orbit.TrackerSettings = tracker;
            orbit.HorizontalAxis.Range = new Vector2(-180f, 180f);
            orbit.HorizontalAxis.Wrap = true;
            orbit.HorizontalAxis.Value = 0f;
            orbit.VerticalAxis.Range = new Vector2(2f, 80f);
            orbit.VerticalAxis.Center = 24f;
            orbit.VerticalAxis.Value = 24f;
            orbit.RadialAxis.Range = new Vector2(1f, 1f);
            orbit.RadialAxis.Value = 1f;

            var composer = rig.AddComponent<CinemachineRotationComposer>();
            var comp = composer.Composition;
            // The player sits low and centred in frame.
            comp.ScreenPosition = new Vector2(0f, -0.12f);
            composer.Composition = comp;
            composer.Damping = Vector2.zero;

            var deoccluder = rig.AddComponent<CinemachineDeoccluder>();
            deoccluder.CollideAgainst = 1 << Layers.Ground;
            deoccluder.MinimumDistanceFromTarget = 1f;
            var avoid = deoccluder.AvoidObstacles;
            avoid.Enabled = true;
            avoid.CameraRadius = 0.3f;
            avoid.Strategy = CinemachineDeoccluder.ObstacleAvoidance.ResolutionStrategy.PullCameraForward;
            avoid.Damping = 0.3f;
            avoid.DampingWhenOccluded = 0.05f;
            deoccluder.AvoidObstacles = avoid;

            var chase = rig.AddComponent<ChaseCamera>();
            chase.Orbit = orbit;
            chase.Player = player;
            chase.Anchor = follow;
            player.ViewYaw = camGo.transform;
            rig.AddComponent<CinemachineCameraOffset>();
            CameraShake.Attach(rig);
            rig.SetActive(true);
            return chase;
        }

        public static HordeManager CreateHorde(GameAssets assets, PlayerMotor player, Transform parent)
        {
            var go = new GameObject("Horde");
            go.transform.SetParent(parent, false);
            go.SetActive(false);
            var renderer = go.AddComponent<HordeRenderer>();
            renderer.HordeMaterial = assets.Horde;
            var horde = go.AddComponent<HordeManager>();
            if (player != null)
            {
                horde.Target = player.transform;
                horde.TargetHealth = player.GetComponent<PlayerHealth>();
            }
            go.SetActive(true);
            return horde;
        }
    }
}
