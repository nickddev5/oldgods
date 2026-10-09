using OldGods.Rules;
using UnityEngine;

namespace OldGods.Runtime
{
    /// <summary>Authoring asset for a horde enemy. Converts to the engine-free EnemyDef.</summary>
    [CreateAssetMenu(menuName = "Old Gods/Enemy", fileName = "Enemy")]
    public sealed class EnemyDefinition : ScriptableObject
    {
        public string Id = "enemy.new";
        public string DisplayName = "New Enemy";
        public float MaxHealth = 10f;
        public float MoveSpeed = 3.5f;
        public float Radius = 0.45f;
        public float ContactDamage = 5f;
        public int XpValue = 1;
        public float Scale = 1f;
        [Range(0f, 1f)] public float GoldChance = 0.02f;
        public bool IsElite;
        public Color Color = new Color(0.6f, 0.25f, 0.2f);
        [Tooltip("Leave empty to use the placeholder husk.")]
        public Mesh Mesh;

        public EnemyDef ToDef() => new EnemyDef
        {
            Id = Id,
            DisplayName = DisplayName,
            MaxHealth = MaxHealth,
            MoveSpeed = MoveSpeed,
            Radius = Radius,
            ContactDamage = ContactDamage,
            XpValue = XpValue,
            Scale = Scale,
            GoldChance = GoldChance,
            IsElite = IsElite,
        };

        public Mesh MeshOrPlaceholder => Mesh != null ? Mesh : PlaceholderMeshes.Husk();
    }
}
