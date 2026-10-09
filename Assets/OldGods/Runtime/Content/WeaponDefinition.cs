using System.Collections.Generic;
using OldGods.Rules;
using UnityEngine;

namespace OldGods.Runtime
{
    /// <summary>Authoring asset for an automatic weapon. Converts to the engine-free WeaponDef.</summary>
    [CreateAssetMenu(menuName = "Old Gods/Weapon", fileName = "Weapon")]
    public sealed class WeaponDefinition : ScriptableObject
    {
        public string Id = "weapon.new";
        public string DisplayName = "New Weapon";
        [TextArea] public string Description = "";
        public WeaponShape Shape;
        public WeaponStats Base = new WeaponStats { Damage = 10f, Cooldown = 1f, Count = 1f, Size = 1f, Speed = 14f, Duration = 1f, Range = 18f, Knockback = 1f };
        public List<WeaponUpgrade> Upgrades = new List<WeaponUpgrade>();
        public float SlowSeconds;
        [Tooltip("Unlock id that must be owned before the weapon is drafted; empty if always available.")]
        public string UnlockId = "";
        [Tooltip("Embers to unlock at the Shrine of Embers when UnlockId is set.")]
        public int UnlockCost;
        [Header("Look")]
        public Color Color = Color.white;
        [ColorUsage(false, true)] public Color Glow = Color.white;
        [Tooltip("Scale of the projectile or orbiter model before the weapon's Size applies.")]
        public Vector3 VisualScale = Vector3.one;

        public WeaponDef ToDef() => new WeaponDef
        {
            Id = Id,
            Name = DisplayName,
            Description = Description,
            Shape = Shape,
            Base = Base,
            UpgradePool = new List<WeaponUpgrade>(Upgrades),
            SlowSeconds = SlowSeconds,
            UnlockId = string.IsNullOrEmpty(UnlockId) ? null : UnlockId,
        };
    }
}
