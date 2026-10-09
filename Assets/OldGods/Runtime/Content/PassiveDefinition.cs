using OldGods.Rules;
using UnityEngine;

namespace OldGods.Runtime
{
    /// <summary>Authoring asset for a passive. Converts to the engine-free PassiveDef.</summary>
    [CreateAssetMenu(menuName = "Old Gods/Passive", fileName = "Passive")]
    public sealed class PassiveDefinition : ScriptableObject
    {
        public string Id = "passive.new";
        public string DisplayName = "New Passive";
        [TextArea] public string Description = "";
        public StatId Stat;
        public float AddPerLevel;
        public float PercentPerLevel;
        public string UnlockId = "";
        public Color Color = Color.white;

        public PassiveDef ToDef() => new PassiveDef
        {
            Id = Id,
            Name = DisplayName,
            Description = Description,
            PerLevel = new StatMod(Stat, AddPerLevel, PercentPerLevel),
            UnlockId = string.IsNullOrEmpty(UnlockId) ? null : UnlockId,
        };
    }
}
