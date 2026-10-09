using OldGods.Rules;
using UnityEngine;

namespace OldGods.Runtime
{
    /// <summary>Everything that makes one stage's world: ground, light, enemies over time, boss.</summary>
    [CreateAssetMenu(menuName = "Old Gods/Biome", fileName = "Biome")]
    public sealed class BiomeDefinition : ScriptableObject
    {
        public string Id = "biome.new";
        public string DisplayName = "New Biome";
        public TerrainProfile Terrain = new TerrainProfile();
        public GroundPalette Palette = new GroundPalette();
        public StageTimelineDef Timeline = new StageTimelineDef();
        public BossDefinition Boss;

        [Header("Atmosphere")]
        public Color Sun = new Color(1f, 0.95f, 0.85f);
        public float SunIntensity = 1.3f;
        public Vector3 SunEuler = new Vector3(50f, -35f, 0f);
        public Color AmbientSky = new Color(0.62f, 0.68f, 0.78f);
        public Color AmbientEquator = new Color(0.5f, 0.5f, 0.48f);
        public Color AmbientGround = new Color(0.25f, 0.24f, 0.22f);
        public Color Fog = new Color(0.66f, 0.70f, 0.74f);
        public float FogStart = 40f;
        public float FogEnd = 170f;
        public Color WaterColor = new Color(0.25f, 0.45f, 0.55f, 0.55f);

        [Header("Props")]
        public PropModel RockModel = PropModel.Boulder;
        public PropModel TreeModel = PropModel.Pine;
        public Color RockColor = new Color(0.55f, 0.53f, 0.5f);
        public int RockCount = 70;
        public Color TreeColor = new Color(0.3f, 0.42f, 0.25f);
        public int TreeCount;
        [Tooltip("Imported prefabs used instead of the built-in rocks and trees, picked at random.")]
        public System.Collections.Generic.List<GameObject> RockPrefabs = new System.Collections.Generic.List<GameObject>();
        public System.Collections.Generic.List<GameObject> TreePrefabs = new System.Collections.Generic.List<GameObject>();
    }
}
