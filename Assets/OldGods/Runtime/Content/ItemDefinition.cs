using System.Collections.Generic;
using OldGods.Rules;
using UnityEngine;

namespace OldGods.Runtime
{
    /// <summary>Authoring asset for a chest or merchant item.</summary>
    [CreateAssetMenu(menuName = "Old Gods/Item", fileName = "Item")]
    public sealed class ItemDefinition : ScriptableObject
    {
        public string Id = "item.new";
        public string DisplayName = "New Item";
        [TextArea] public string Description = "";
        public Rarity Rarity;
        public List<StatMod> Mods = new List<StatMod>();
        [Tooltip("Runtime effect name for items that are more than stats, e.g. heal_on_kill.")]
        public string Special = "";
        public float SpecialValue;
        public string UnlockId = "";
        public Color Color = Color.white;

        public ItemDef ToDef() => new ItemDef
        {
            Id = Id,
            Name = DisplayName,
            Description = Description,
            Rarity = Rarity,
            Mods = new List<StatMod>(Mods),
            Special = string.IsNullOrEmpty(Special) ? null : Special,
            SpecialValue = SpecialValue,
            UnlockId = string.IsNullOrEmpty(UnlockId) ? null : UnlockId,
        };
    }
}
