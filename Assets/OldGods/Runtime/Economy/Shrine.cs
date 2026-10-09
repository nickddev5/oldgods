using System.Collections.Generic;
using OldGods.Rules;
using UnityEngine;

namespace OldGods.Runtime
{
    /// <summary>
    /// One shrine. Charge shrines fill while the player stands in their ring and then offer
    /// three stat boosts; the others are used with Interact.
    /// </summary>
    public sealed class Shrine : Interactable
    {
        public const float ChargeRadius = 5f;

        public ShrineKind Kind;
        public Transform Gem;
        public Transform Ring;
        bool used;
        float charge;
        MaterialPropertyBlock ringProps;

        public override string Prompt
        {
            get
            {
                switch (Kind)
                {
                    case ShrineKind.Item: return "Shrine of Gifts: give 20% of your health for an item";
                    case ShrineKind.Greed: return "Shrine of Greed: harder foes, richer gold";
                    case ShrineKind.BossCurse: return "Shrine of the Curse: another guardian, another chest";
                    case ShrineKind.Challenge: return "Shrine of Challenge: call a champion";
                    case ShrineKind.Magnet: return "Shrine of Drawing: pull in every gem";
                    default: return "";
                }
            }
        }

        public override bool CanUse => !used && Kind != ShrineKind.Charge && (Kind != ShrineKind.BossCurse || !RunController.Instance.BossActive);
        public override string MapLabel => used ? null : "Shrine";
        public override Color MapColor => Features.ShrineColor(Kind) * 0.6f;

        void Awake()
        {
            Range = 3f;
            HoldSeconds = 0.7f;
        }

        void Update()
        {
            if (Gem != null)
            {
                Gem.localRotation = Quaternion.Euler(0f, Time.time * 60f, 0f);
                Gem.localPosition = new Vector3(0f, 1.9f + Mathf.Sin(Time.time * 2f) * 0.12f, 0f);
            }
            if (Kind != ShrineKind.Charge || used) return;
            var run = RunController.Instance;
            if (run == null || run.Player == null || Time.timeScale <= 0f) return;
            bool inside = DistanceTo(run.Player.transform.position) < ChargeRadius;
            charge = ShrineRules.StepCharge(charge, inside, Time.deltaTime);
            if (Ring != null)
            {
                float s = ChargeRadius / 1.2f;
                Ring.localScale = new Vector3(s, 1f, s);
                ringProps ??= new MaterialPropertyBlock();
                ringProps.SetColor("_BaseColor", new Color(0.4f, 1f, 1.6f, 0.25f + 0.6f * charge));
                Ring.GetComponent<MeshRenderer>().SetPropertyBlock(ringProps);
            }
            if (inside && charge > 0f && charge < 1f && Time.frameCount % 10 == 0)
                DamageNumbers.ShowText(transform.position + Vector3.up * 3.2f, $"{charge * 100f:0}%", new Color(0.6f, 0.9f, 1f));
            if (charge >= 1f) Charged(run);
        }

        void Charged(RunController run)
        {
            used = true;
            if (Ring != null) Ring.gameObject.SetActive(false);
            var choices = ShrineRules.ChargeChoices(run.Seed.Stream(RunSeed.Shrines, Mathf.RoundToInt(transform.position.x * 31 + transform.position.z)), run.Combat.Stats.Value(StatId.Luck));
            var options = new List<ChoiceScreen.Option>();
            foreach (var (mod, rarity) in choices)
                options.Add(new ChoiceScreen.Option { Title = StatText.Name(mod.Stat), Description = StatText.Describe(mod), Rarity = rarity });
            RunEconomy.Instance.NoteShrine(ShrineKind.Charge);
            ChoiceScreen.Show("The shrine is charged", options, i =>
            {
                run.Combat.ExternalMods.Add(choices[i].mod);
                run.Combat.RecomputeStats();
            });
            Spent();
        }

        void Spent()
        {
            used = true;
            if (Gem != null) Gem.gameObject.SetActive(false);
            Effects.Burst(Fx.Column(), transform.position, Quaternion.identity, new Vector3(0.8f, 0.5f, 0.8f), new Vector3(1.5f, 8f, 1.5f),
                Features.ShrineColor(Kind), 0.6f);
        }

        public override void Use(PlayerCombat player)
        {
            var run = RunController.Instance;
            var eco = RunEconomy.Instance;
            switch (Kind)
            {
                case ShrineKind.Item:
                    run.PlayerHealth.TakeTrueDamage(run.PlayerHealth.Health.Max * ShrineRules.ItemShrineHealthCost);
                    if (run.IsOver) return;
                    eco.Grant(eco.RollItem(Rarity.Uncommon));
                    break;
                case ShrineKind.Greed:
                    run.Director.DifficultyBonus += ShrineRules.GreedDifficulty;
                    eco.GreedGold += ShrineRules.GreedGold;
                    run.Announce("Greed answers", $"Foes +{ShrineRules.GreedDifficulty * 100f:0}%, gold +{ShrineRules.GreedGold * 100f:0}%");
                    break;
                case ShrineKind.BossCurse:
                    run.BossCurses++;
                    run.Announce("The curse is taken", $"{ShrineRules.BossesForCurses(run.BossCurses)} guardians will wake");
                    break;
                case ShrineKind.Challenge:
                    if (run.Director.SpawnEnemy("enemy.champion", transform.position + transform.forward * 8f, 1.2f) >= 0)
                        run.Announce("A champion answers", "Defeat it for a chest");
                    break;
                case ShrineKind.Magnet:
                    run.Pickups.MagnetAll();
                    break;
            }
            eco.NoteShrine(Kind);
            Spent();
        }

    }
}
