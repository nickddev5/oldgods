using OldGods.Rules;
using UnityEngine;

namespace OldGods.Runtime
{
    /// <summary>
    /// Shared references every scene needs: materials, content and tuning. Scenes hold
    /// one reference to this asset; everything else is built in code at run start.
    /// </summary>
    [CreateAssetMenu(menuName = "Old Gods/Game Assets", fileName = "GameAssets")]
    public sealed class GameAssets : ScriptableObject
    {
        public Material LowPoly;
        public Material Horde;
        public Material Glow;
        public Material UnlitGlow;
        public Material UnlitFade;
        public ContentLibrary Content;
        [Tooltip("Weapon the greybox run starts with until gods pick their own.")]
        public string StartingWeapon = "weapon.spear_volley";
        [Tooltip("The stages of a run, in order. Empty uses the greybox biome three times.")]
        public System.Collections.Generic.List<BiomeDefinition> Stages = new System.Collections.Generic.List<BiomeDefinition>();
        public BiomeDefinition GreyboxBiome;
        public MotorTuning Motor = new MotorTuning();
        public TerrainProfile GreyboxTerrain = new TerrainProfile();
        public GroundPalette GreyboxPalette = new GroundPalette();

        static GameAssets loaded;

        /// <summary>The asset at Resources/GameAssets, for code paths without a scene reference.</summary>
        public static GameAssets Load()
        {
            if (loaded == null) loaded = Resources.Load<GameAssets>("GameAssets");
            return loaded;
        }
    }
}
