using System.Collections.Generic;
using OldGods.Rules;
using UnityEngine;

namespace OldGods.Runtime
{
    public enum GodLook { Storm, Forge, Tide, Hunt, Ember, Earth, Elias }

    /// <summary>Authoring asset for a playable god.</summary>
    [CreateAssetMenu(menuName = "Old Gods/God", fileName = "God")]
    public sealed class GodDefinition : ScriptableObject
    {
        public string Id = "god.new";
        [Tooltip("Working domain name until canon names are decided.")]
        public string DisplayName = "New God";
        [TextArea] public string Lore = "";
        public string StartingWeapon = "";
        public string StartingPassive = "";
        public List<StatMod> Kit = new List<StatMod>();
        public int Order;
        public int Cost;
        public bool IsLast;
        public GodLook Look;
        public Color Robe = Color.white;
        [ColorUsage(false, true)] public Color Mark = Color.white;
        [Header("Imported model (optional)")]
        [Tooltip("A model prefab (FBX or prefab) shown instead of the built-in body. Face +Z, feet at the origin.")]
        public GameObject ModelPrefab;
        [Tooltip("Uniform scale applied to the imported model.")]
        public float ModelScale = 1f;

        public GodDef ToDef() => new GodDef
        {
            Id = Id,
            Name = DisplayName,
            Lore = Lore,
            StartingWeapon = StartingWeapon,
            StartingPassive = StartingPassive,
            Kit = new List<StatMod>(Kit),
            Order = Order,
            Cost = Cost,
            IsLast = IsLast,
        };
    }
}
