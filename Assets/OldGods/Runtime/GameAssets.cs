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
        public ContentLibrary Content;
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
