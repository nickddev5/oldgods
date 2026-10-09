using System;
using OldGods.Rules;
using UnityEngine;

namespace OldGods.Runtime
{
    /// <summary>The player's health, armour and death. Wraps the pure Health rule.</summary>
    public sealed class PlayerHealth : MonoBehaviour
    {
        public float MaxHealth = 100f;
        public float HitInvulnerability = 0.6f;
        /// <summary>Flat damage reduction per hit.</summary>
        public float Armor;
        /// <summary>0..1 chance to ignore a hit.</summary>
        public float Evasion;
        /// <summary>Health regenerated per second.</summary>
        public float Regen;
        public bool Invincible;

        public Health Health { get; private set; }
        public bool IsDead => Health != null && Health.IsDead;

        public event Action<float> Damaged;
        public event Action Died;

        float regenBank;
        System.Random evadeRoll = new System.Random(7);

        void Awake()
        {
            Health = new Health(MaxHealth) { HitInvulnerability = HitInvulnerability };
        }

        void Update()
        {
            if (Health == null || IsDead) return;
            Health.Tick(Time.deltaTime);
            if (Regen > 0f)
            {
                regenBank += Regen * Time.deltaTime;
                if (regenBank >= 1f)
                {
                    Health.Heal(Mathf.Floor(regenBank));
                    regenBank -= Mathf.Floor(regenBank);
                }
            }
        }

        /// <summary>An enemy hit: armour, evasion and invulnerability apply.</summary>
        public void TakeHit(float amount, Vector3 from)
        {
            if (Health == null || IsDead || Invincible) return;
            if (Health.InvulnerableFor > 0f) return;
            if (Evasion > 0f && evadeRoll.NextDouble() < Evasion)
            {
                DamageNumbers.ShowText(transform.position + Vector3.up * 2.2f, "evade", new Color(0.7f, 0.9f, 1f));
                Health.Guard();
                return;
            }
            Apply(Mathf.Max(1f, amount - Armor), false);
        }

        /// <summary>Damage that ignores armour, evasion and invulnerability (falls, curses).</summary>
        public void TakeTrueDamage(float amount)
        {
            if (Health == null || IsDead || Invincible) return;
            Apply(amount, true);
        }

        void Apply(float amount, bool ignoreInvulnerability)
        {
            float dealt = Health.Damage(amount, ignoreInvulnerability);
            if (dealt <= 0f) return;
            Damaged?.Invoke(dealt);
            if (Health.IsDead) Died?.Invoke();
        }

        public void Kill()
        {
            if (Health == null || IsDead) return;
            Health.Damage(Health.Current + 1f, true);
            Died?.Invoke();
        }

        public void SetMaxHealth(float max)
        {
            MaxHealth = max;
            Health?.SetMax(max, true);
        }
    }
}
