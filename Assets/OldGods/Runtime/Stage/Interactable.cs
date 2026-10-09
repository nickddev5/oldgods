using System.Collections.Generic;
using UnityEngine;

namespace OldGods.Runtime
{
    /// <summary>
    /// Something the player uses by standing near it and holding Interact: the boss gate,
    /// chests, shrines, the merchant. The nearest one in range gets the prompt.
    /// </summary>
    public abstract class Interactable : MonoBehaviour
    {
        public static readonly List<Interactable> All = new List<Interactable>();

        public float Range = 3f;
        public float HoldSeconds = 0.6f;
        public bool Discovered;

        public abstract string Prompt { get; }
        public virtual bool CanUse => true;
        /// <summary>Whether the prompt shows. Paid things show while unaffordable, so the price is visible.</summary>
        public virtual bool Shown => CanUse;
        /// <summary>Label shown on the minimap once discovered; null for none.</summary>
        public virtual string MapLabel => null;
        public virtual Color MapColor => Color.white;

        protected virtual void OnEnable() => All.Add(this);
        protected virtual void OnDisable() => All.Remove(this);

        public abstract void Use(PlayerCombat player);

        public float DistanceTo(Vector3 p)
        {
            Vector3 d = transform.position - p;
            d.y = 0f;
            return d.magnitude;
        }
    }

    /// <summary>Finds the nearest usable interactable, shows its prompt and fires it after a hold.</summary>
    public sealed class InteractionDriver : MonoBehaviour
    {
        public PlayerCombat Player;
        public float DiscoverRadius = 30f;

        public Interactable Current { get; private set; }
        public float HoldProgress { get; private set; }

        void Update()
        {
            if (Player == null || Time.timeScale <= 0f) return;
            Vector3 p = Player.transform.position;
            Interactable best = null;
            float bestD = float.MaxValue;
            foreach (var it in Interactable.All)
            {
                if (it == null) continue;
                float d = it.DistanceTo(p);
                if (d < DiscoverRadius) it.Discovered = true;
                if (!it.Shown || d > it.Range || d >= bestD) continue;
                best = it;
                bestD = d;
            }
            if (best != Current) HoldProgress = 0f;
            Current = best;
            if (Current == null) return;

            if (Current.CanUse && GameInput.Held(GameInput.Interact))
            {
                HoldProgress += Time.deltaTime / Mathf.Max(0.01f, Current.HoldSeconds);
                if (HoldProgress >= 1f)
                {
                    HoldProgress = 0f;
                    var used = Current;
                    Current = null;
                    used.Use(Player);
                }
            }
            else
            {
                HoldProgress = 0f;
            }
        }
    }
}
