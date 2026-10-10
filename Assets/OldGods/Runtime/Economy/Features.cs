using System.Collections.Generic;
using OldGods.Rules;
using UnityEngine;

namespace OldGods.Runtime
{
    /// <summary>Builds the economy's map features. Greybox looks until milestone 9.</summary>
    public static class Features
    {
        public static Mesh ChestMesh() => Cached("chest", () =>
        {
            var k = new MeshKit();
            k.Box(new Vector3(0f, 0.35f, 0f), new Vector3(1.2f, 0.7f, 0.8f), new Color(0.7f, 0.55f, 0.35f));
            k.Box(new Vector3(0f, 0.8f, 0f), new Vector3(1.25f, 0.25f, 0.85f), new Color(0.55f, 0.42f, 0.28f), 0.85f);
            k.Box(new Vector3(0f, 0.55f, 0.42f), new Vector3(0.2f, 0.25f, 0.05f), new Color(1f, 0.85f, 0.4f));
            return k.Build("Chest");
        });

        public static Mesh PedestalMesh() => Cached("pedestal", () =>
        {
            var k = new MeshKit();
            k.Prism(Vector3.zero, 0.9f, 0.3f, 6, new Color(0.7f, 0.7f, 0.7f), 0.85f);
            k.Prism(new Vector3(0f, 0.3f, 0f), 0.45f, 1.1f, 6, new Color(0.85f, 0.85f, 0.85f), 0.7f);
            return k.Build("Pedestal");
        });

        public static Mesh StallMesh() => Cached("stall", () =>
        {
            var k = new MeshKit();
            k.Box(new Vector3(0f, 0.5f, 0f), new Vector3(2.4f, 1f, 1.2f), new Color(0.6f, 0.45f, 0.3f));
            k.Box(new Vector3(-1.1f, 1.4f, -0.5f), new Vector3(0.12f, 1.8f, 0.12f), new Color(0.5f, 0.38f, 0.26f));
            k.Box(new Vector3(1.1f, 1.4f, -0.5f), new Vector3(0.12f, 1.8f, 0.12f), new Color(0.5f, 0.38f, 0.26f));
            k.Box(new Vector3(0f, 2.35f, 0f), new Vector3(2.8f, 0.15f, 1.8f), new Color(0.75f, 0.3f, 0.25f), 1f, Quaternion.Euler(-12f, 0f, 0f));
            return k.Build("Stall");
        });

        public static Mesh TwinPillarsMesh() => Cached("twin", () =>
        {
            var k = new MeshKit();
            k.Prism(new Vector3(-0.8f, 0f, 0f), 0.35f, 2.2f, 5, new Color(0.8f, 0.8f, 0.85f), 0.6f);
            k.Prism(new Vector3(0.8f, 0f, 0f), 0.35f, 2.2f, 5, new Color(0.8f, 0.8f, 0.85f), 0.6f);
            k.Prism(Vector3.zero, 1.4f, 0.15f, 8, new Color(0.6f, 0.6f, 0.65f));
            return k.Build("TwinPillars");
        });

        static readonly Dictionary<string, Mesh> meshes = new Dictionary<string, Mesh>();

        static Mesh Cached(string key, System.Func<Mesh> make)
        {
            if (meshes.TryGetValue(key, out var m) && m != null) return m;
            meshes[key] = m = make();
            return m;
        }

        public static Color ShrineColor(ShrineKind k)
        {
            switch (k)
            {
                case ShrineKind.Charge: return new Color(0.4f, 1.2f, 1.8f);
                case ShrineKind.Item: return new Color(1.8f, 1.4f, 0.4f);
                case ShrineKind.Greed: return new Color(1.8f, 1.1f, 0.2f);
                case ShrineKind.BossCurse: return new Color(1.8f, 0.3f, 0.3f);
                case ShrineKind.Challenge: return new Color(1.5f, 0.5f, 1.8f);
                case ShrineKind.Offering: return new Color(1.9f, 1.6f, 0.7f);
                default: return new Color(1.4f, 1.4f, 1.6f);
            }
        }

        public static void Build(RunController run, Placement p, Transform parent)
        {
            var at = Ground.Snap(new Vector3(p.X, 0f, p.Z));
            float yaw = Mathf.Atan2(-p.X, -p.Z) * Mathf.Rad2Deg;
            var rot = Quaternion.Euler(0f, yaw, 0f);
            var lowPoly = run.Assets.LowPoly;
            switch (p.Kind)
            {
                case FeatureKind.Chest:
                {
                    var go = WorldBuilder.CreateProp("Chest", ChestMesh(), WorldBuilder.Tinted(lowPoly, Color.white), at, rot, Vector3.one * 1.2f, parent, true);
                    go.AddComponent<Chest>();
                    break;
                }
                case FeatureKind.Merchant:
                {
                    var go = WorldBuilder.CreateProp("Merchant", StallMesh(), WorldBuilder.Tinted(lowPoly, Color.white), at, rot, Vector3.one * 1.3f, parent, true);
                    go.AddComponent<Merchant>().Stock(RunEconomy.Instance.RollItem(Rarity.Uncommon));
                    break;
                }
                case FeatureKind.Duplicator:
                {
                    var go = WorldBuilder.CreateProp("Duplicator", TwinPillarsMesh(), WorldBuilder.Tinted(lowPoly, Color.white), at, rot, Vector3.one * 1.2f, parent, true);
                    go.AddComponent<Duplicator>();
                    AddGlow(go.transform, new Vector3(0f, 1.2f, 0f), new Vector3(0.9f, 0.9f, 0.9f), new Color(1.2f, 1.2f, 1.8f));
                    break;
                }
                case FeatureKind.Shrine:
                case FeatureKind.OfferingShrine:
                {
                    var kind = p.Kind == FeatureKind.OfferingShrine ? ShrineKind.Offering : ShrineRules.KindFor(p.Index);
                    var go = WorldBuilder.CreateProp($"Shrine {kind}", PedestalMesh(), WorldBuilder.Tinted(lowPoly, new Color(0.6f, 0.6f, 0.62f)), at, rot, Vector3.one * 1.2f, parent, true);
                    var shrine = go.AddComponent<Shrine>();
                    shrine.Kind = kind;
                    shrine.Gem = AddGlow(go.transform, new Vector3(0f, 1.9f, 0f), new Vector3(0.6f, 0.8f, 0.6f), ShrineColor(kind));
                    if (kind == ShrineKind.Charge)
                    {
                        var ring = new GameObject("Charge Ring");
                        ring.transform.SetParent(go.transform, false);
                        ring.transform.localPosition = Vector3.up * 0.1f;
                        ring.transform.localScale = Vector3.one * Shrine.ChargeRadius / 1.2f;
                        ring.AddComponent<MeshFilter>().sharedMesh = Fx.Ring(0.9f, 48);
                        ring.AddComponent<MeshRenderer>().sharedMaterial = Fx.Fade(new Color(0.4f, 1f, 1.6f, 0.5f));
                        shrine.Ring = ring.transform;
                    }
                    break;
                }
            }
        }

        public static Transform AddGlow(Transform parent, Vector3 local, Vector3 scale, Color color)
        {
            var g = new GameObject("Glow");
            g.transform.SetParent(parent, false);
            g.transform.localPosition = local;
            g.transform.localScale = scale;
            g.AddComponent<MeshFilter>().sharedMesh = PlaceholderMeshes.XpGem();
            g.AddComponent<MeshRenderer>().sharedMaterial = Fx.Glow(color);
            return g.transform;
        }

        /// <summary>A free chest dropped by a boss, champion or challenge.</summary>
        public static Chest DropFreeChest(RunController run, Vector3 at, Transform parent)
        {
            var go = WorldBuilder.CreateProp("Free Chest", ChestMesh(), WorldBuilder.Tinted(run.Assets.LowPoly, new Color(1f, 0.92f, 0.7f)),
                Ground.Snap(at), Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), Vector3.one * 1.4f, parent, true);
            var chest = go.AddComponent<Chest>();
            chest.Free = true;
            chest.Discovered = true;
            AddGlow(go.transform, new Vector3(0f, 1.6f, 0f), Vector3.one * 0.5f, new Color(1.8f, 1.5f, 0.6f));
            Effects.Burst(Fx.Ring(0.6f, 32), go.transform.position + Vector3.up * 0.2f, Quaternion.identity, Vector3.one, Vector3.one * 5f, new Color(1.8f, 1.5f, 0.6f, 0.8f), 0.8f);
            return chest;
        }
    }

    public sealed class Chest : Interactable
    {
        public bool Free;
        bool opened;

        int Price => RunEconomy.Instance.ChestPrice;
        public override string Prompt => Free ? "Open chest" : CanUse ? $"Open chest ({Price} gold)" : $"Chest: {Price} gold (you have {RunEconomy.Instance.Wallet.Gold})";
        public override bool CanUse => !opened && (Free || RunEconomy.Instance.Wallet.Gold >= Price);
        public override bool Shown => !opened;
        public override string MapLabel => opened ? null : "Chest";
        public override Color MapColor => new Color(1f, 0.85f, 0.4f);

        void Awake()
        {
            Range = 2.8f;
            HoldSeconds = 0.35f;
        }

        public override void Use(PlayerCombat player)
        {
            if (opened || !RunEconomy.Instance.OpenChest(Free)) return;
            opened = true;
            Audio.Play(Sfx.Chest, 0.9f, 0f);
            Effects.Burst(Fx.Column(), transform.position, Quaternion.identity, new Vector3(0.6f, 0.5f, 0.6f), new Vector3(1.2f, 6f, 1.2f), new Color(1.8f, 1.5f, 0.6f, 0.8f), 0.6f);
            foreach (Transform c in transform) Destroy(c.gameObject);
            transform.localScale = new Vector3(transform.localScale.x, transform.localScale.y * 0.6f, transform.localScale.z);
        }
    }

    public sealed class Merchant : Interactable
    {
        ItemDef item;
        bool sold;
        int sales;

        public void Stock(ItemDef i) => item = i;

        int Price => EconomyRules.MerchantPrice(item.Rarity, RunEconomy.Instance.PaidChestsOpened, RunController.Instance.StageIndex);
        public override string Prompt => item == null ? "Sold out" : CanUse ? $"Buy {item.Name}, {item.Rarity} ({Price} gold)" : $"{item.Name}, {item.Rarity}: {Price} gold (you have {RunEconomy.Instance.Wallet.Gold})";
        public override bool CanUse => !sold && item != null && RunEconomy.Instance.Wallet.Gold >= Price;
        public override bool Shown => !sold && item != null;
        public override string MapLabel => "Merchant";
        public override Color MapColor => new Color(0.5f, 1f, 0.5f);

        void Awake()
        {
            Range = 3.5f;
            HoldSeconds = 0.5f;
        }

        public override void Use(PlayerCombat player)
        {
            if (sold || item == null || !RunEconomy.Instance.TryBuy(Price)) return;
            RunEconomy.Instance.Grant(item);
            sales++;
            // Restocks once: a second item goes on the stall after the first sale.
            if (EconomyRules.MerchantRestocksAfter(sales)) item = RunEconomy.Instance.RollItem(Rarity.Uncommon);
            else sold = true;
        }
    }

    public sealed class Duplicator : Interactable
    {
        bool used;
        int Price => EconomyRules.DuplicatorPrice(RunEconomy.Instance.PaidChestsOpened, RunController.Instance.StageIndex);
        ItemDef Target => RunController.Instance.Combat.Items.Find(RunController.Instance.Combat.Items.LastAdded);

        public override string Prompt => Target == null ? "Duplicate (no item yet)" : CanUse ? $"Duplicate {Target.Name} ({Price} gold)" : $"Duplicate {Target.Name}: {Price} gold (you have {RunEconomy.Instance.Wallet.Gold})";
        public override bool CanUse => !used && Target != null && RunEconomy.Instance.Wallet.Gold >= Price;
        public override bool Shown => !used;
        public override string MapLabel => "Duplicator";
        public override Color MapColor => new Color(0.8f, 0.8f, 1f);

        void Awake()
        {
            Range = 3f;
            HoldSeconds = 0.8f;
        }

        public override void Use(PlayerCombat player)
        {
            var target = Target;
            if (used || target == null || !RunEconomy.Instance.TryBuy(Price)) return;
            used = true;
            RunEconomy.Instance.Grant(target);
        }
    }
}
