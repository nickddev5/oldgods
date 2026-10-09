using System.Collections.Generic;
using OldGods.Rules;
using UnityEngine;

namespace OldGods.Runtime
{
    public enum BossModel { Warden, Stag, Mother, Construct }

    /// <summary>Authoring asset for a stage boss or The Last Test.</summary>
    [CreateAssetMenu(menuName = "Old Gods/Boss", fileName = "Boss")]
    public sealed class BossDefinition : ScriptableObject
    {
        public string Id = "boss.new";
        public string DisplayName = "New Boss";
        [Tooltip("Line shown when the boss wakes.")]
        public string Epithet = "";
        public float MaxHealth = 2500f;
        public float MoveSpeed = 3f;
        public float Scale = 3f;
        public float Radius = 1.4f;
        public float ContactDamage = 15f;
        public float Rest = 1.2f;
        public string MinionId = "enemy.husk";
        [Range(0f, 1f)] public float EnrageAt = 0.35f;
        public List<BossAttackDef> Attacks = new List<BossAttackDef>();
        public BossModel Model;
        [Tooltip("Not used by the built-in models, which carry their colours in the mesh.")]
        public Color Color = new Color(0.5f, 0.5f, 0.55f);
        [ColorUsage(false, true)] public Color Accent = new Color(1.5f, 0.6f, 0.2f);
        [Header("Imported model (optional)")]
        [Tooltip("A model prefab (FBX or prefab) shown instead of the built-in body. Face +Z, feet at the origin.")]
        public GameObject ModelPrefab;
        [Tooltip("Uniform scale applied to the imported model.")]
        public float ModelScale = 1f;

        public BossDef ToDef() => new BossDef
        {
            Id = Id,
            Name = DisplayName,
            MaxHealth = MaxHealth,
            MoveSpeed = MoveSpeed,
            Scale = Scale,
            Radius = Radius,
            ContactDamage = ContactDamage,
            Rest = Rest,
            MinionId = MinionId,
            EnrageAt = EnrageAt,
            Attacks = new List<BossAttackDef>(Attacks),
        };
    }
}
