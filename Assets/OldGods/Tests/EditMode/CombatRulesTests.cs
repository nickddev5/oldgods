using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using OldGods.Rules;

namespace OldGods.Tests.EditMode
{
    static class TestContent
    {
        public static WeaponDef Weapon(string id, WeaponShape shape = WeaponShape.Projectile) => new WeaponDef
        {
            Id = id, Name = id, Description = "", Shape = shape,
            Base = new WeaponStats { Damage = 10f, Cooldown = 1f, Count = 1f, Size = 1f, Speed = 10f, Duration = 1f, Range = 10f, Knockback = 1f },
            UpgradePool = new List<WeaponUpgrade>
            {
                new WeaponUpgrade(WeaponStat.Damage, 4f),
                new WeaponUpgrade(WeaponStat.Cooldown, 0.1f),
                new WeaponUpgrade(WeaponStat.Count, 1f),
            },
        };

        public static PassiveDef Passive(string id, StatId stat = StatId.Damage, float add = 0.1f) =>
            new PassiveDef { Id = id, Name = id, Description = "", PerLevel = new StatMod(stat, add) };

        public static List<WeaponDef> Weapons(int n) => Enumerable.Range(0, n).Select(i => Weapon("w" + i)).ToList();
        public static List<PassiveDef> Passives(int n) => Enumerable.Range(0, n).Select(i => Passive("p" + i)).ToList();
    }

    public class StatTests
    {
        [Test]
        public void AddsThenPercents()
        {
            var b = StatBlock.Default();
            b.Apply(new StatMod(StatId.MaxHealth, 20f));
            b.Apply(new StatMod(StatId.MaxHealth, 0f, 0.5f));
            Assert.AreEqual(180f, b.Value(StatId.MaxHealth), 1e-3f);
            b.Clear();
            Assert.AreEqual(100f, b.Value(StatId.MaxHealth), 1e-3f);
        }

        [Test]
        public void ClampsEvasionAndMultipliers()
        {
            var b = StatBlock.Default();
            b.Apply(new StatMod(StatId.Evasion, 5f));
            Assert.AreEqual(0.75f, b.Value(StatId.Evasion), 1e-4f);
            b.Apply(new StatMod(StatId.MoveSpeed, -10f));
            Assert.AreEqual(0.1f, b.Value(StatId.MoveSpeed), 1e-4f);
        }

        [Test]
        public void DescribesMods()
        {
            Assert.AreEqual("+10% Damage", StatText.Describe(new StatMod(StatId.Damage, 0.1f)));
            Assert.AreEqual("+15 Max Health", StatText.Describe(new StatMod(StatId.MaxHealth, 15f)));
        }
    }

    public class WeaponTests
    {
        [Test]
        public void EffectiveAppliesUpgradesAndPlayerStats()
        {
            var w = new WeaponState(TestContent.Weapon("w"));
            w.AddBonus(WeaponStat.Damage, 5f);
            w.AddBonus(WeaponStat.Cooldown, 0.2f);
            var player = StatBlock.Default();
            player.Apply(new StatMod(StatId.Damage, 0.5f));
            player.Apply(new StatMod(StatId.AttackSpeed, 0.25f));
            player.Apply(new StatMod(StatId.ProjectileCount, 2f));
            var e = w.Effective(player);
            Assert.AreEqual(15f * 1.5f, e.Damage, 1e-3f);
            Assert.AreEqual(1f * 0.8f / 1.25f, e.Cooldown, 1e-3f);
            Assert.AreEqual(3, e.Count);
        }

        [Test]
        public void AurasIgnoreExtraProjectiles()
        {
            var w = new WeaponState(TestContent.Weapon("aura", WeaponShape.Aura));
            var player = StatBlock.Default();
            player.Apply(new StatMod(StatId.ProjectileCount, 3f));
            Assert.AreEqual(1, w.Effective(player).Count);
        }

        [Test]
        public void CooldownNeverDropsBelowFloor()
        {
            var w = new WeaponState(TestContent.Weapon("w"));
            w.AddBonus(WeaponStat.Cooldown, 5f);
            var player = StatBlock.Default();
            player.Apply(new StatMod(StatId.AttackSpeed, 100f));
            Assert.GreaterOrEqual(w.Effective(player).Cooldown, 0.08f);
        }

        [Test]
        public void CritsMultiplyDamage()
        {
            var e = new EffectiveWeapon { Damage = 10f, CritChance = 0.25f, CritMultiplier = 2f };
            Assert.AreEqual(20f, WeaponState.RollDamage(e, 0.1f, out bool crit));
            Assert.IsTrue(crit);
            Assert.AreEqual(10f, WeaponState.RollDamage(e, 0.9f, out crit));
            Assert.IsFalse(crit);
        }
    }

    public class SwipeTests
    {
        const float Ahead = 0f; // heading along +z

        [Test]
        public void CatchesFoesInFrontWithinReach()
        {
            Assert.IsTrue(Swipe.Catches(0f, 2f, 0.4f, Ahead, 2.5f, 140f));
            Assert.IsTrue(Swipe.Catches(1.2f, 1.2f, 0.4f, Ahead, 2.5f, 140f), "45 degrees off is inside a 140 degree arc");
        }

        [Test]
        public void MissesFoesBeyondReachOrOutsideTheArc()
        {
            Assert.IsFalse(Swipe.Catches(0f, 3.5f, 0.4f, Ahead, 2.5f, 140f), "too far");
            Assert.IsFalse(Swipe.Catches(2f, 0f, 0.4f, Ahead, 2.5f, 140f), "90 degrees off");
            Assert.IsFalse(Swipe.Catches(0f, -2f, 0.4f, Ahead, 2.5f, 140f), "behind");
        }

        [Test]
        public void BodyRadiusExtendsReachAndFoesOnThePlayerAreCaught()
        {
            Assert.IsTrue(Swipe.Catches(0f, 2.8f, 0.4f, Ahead, 2.5f, 140f));
            Assert.IsTrue(Swipe.Catches(0.1f, -0.1f, 0.4f, Ahead, 2.5f, 140f));
        }

        [Test]
        public void ArcWrapsAcrossTheBack()
        {
            float back = Swipe.HeadingTo(0f, -1f);
            Assert.IsTrue(Swipe.Catches(-0.3f, -2f, 0.4f, back, 2.5f, 140f));
            Assert.IsTrue(Swipe.Catches(0.3f, -2f, 0.4f, back, 2.5f, 140f));
        }

        [Test]
        public void ExtraSwipesSpreadEvenlySoTheSecondStrikesBehind()
        {
            float aim = Swipe.HeadingTo(1f, 0f);
            Assert.AreEqual(aim, Swipe.Heading(aim, 0, 1), 1e-5f);
            float second = Swipe.Heading(aim, 1, 2);
            Assert.IsTrue(Swipe.Catches(-2f, 0f, 0.4f, second, 2.5f, 140f));
            Assert.IsFalse(Swipe.Catches(2f, 0f, 0.4f, second, 2.5f, 140f));
        }
    }

    public class XpTests
    {
        [Test]
        public void CurveRises()
        {
            Assert.AreEqual(5, XpRules.Required(1));
            for (int l = 1; l < 50; l++) Assert.Greater(XpRules.Required(l + 1), XpRules.Required(l));
        }

        [Test]
        public void CurveSteepensLate()
        {
            // Early levels cost about what they did (5 + 4(l-1)^1.25); level 60 costs about four times as much.
            int Old(int l) => (int)System.Math.Round(5.0 + 4.0 * System.Math.Pow(l - 1, 1.25));
            for (int l = 1; l <= 25; l++) Assert.LessOrEqual(XpRules.Required(l), Old(l) * 1.02f + 1f, $"level {l}");
            Assert.Greater(XpRules.Required(60), Old(60) * 3.5f);
        }

        [Test]
        public void TrackerCarriesOverflow()
        {
            var t = new XpTracker();
            int gained = t.Add(XpRules.Required(1) + XpRules.Required(2) + 1f);
            Assert.AreEqual(2, gained);
            Assert.AreEqual(3, t.Level);
            Assert.AreEqual(1f, t.Xp, 1e-4f);
            Assert.AreEqual(0, t.Add(0f));
        }
    }

    public class DraftTests
    {
        [Test]
        public void DealsThreeDistinctCards()
        {
            var l = new Loadout();
            var weapons = TestContent.Weapons(6);
            var passives = TestContent.Passives(6);
            for (int seed = 0; seed < 50; seed++)
            {
                var cards = DraftRules.Roll(l, weapons, passives, new Rng((ulong)seed), 0f);
                Assert.AreEqual(3, cards.Count);
                Assert.AreEqual(3, cards.Select(c => c.Id).Distinct().Count());
            }
        }

        [Test]
        public void SameSeedSameCards()
        {
            var l = new Loadout();
            var a = DraftRules.Roll(l, TestContent.Weapons(6), TestContent.Passives(6), new Rng(9), 0f);
            var b = DraftRules.Roll(l, TestContent.Weapons(6), TestContent.Passives(6), new Rng(9), 0f);
            CollectionAssert.AreEqual(a.Select(c => c.ToString()), b.Select(c => c.ToString()));
        }

        [Test]
        public void FullSlotsOnlyOfferUpgrades()
        {
            var l = new Loadout { WeaponSlots = 1, PassiveSlots = 1 };
            var weapons = TestContent.Weapons(4);
            var passives = TestContent.Passives(4);
            l.AddWeapon(weapons[0]);
            l.AddPassive(passives[0]);
            for (int seed = 0; seed < 30; seed++)
            {
                var cards = DraftRules.Roll(l, weapons, passives, new Rng((ulong)seed), 0f);
                Assert.IsTrue(cards.All(c => c.Kind == DraftKind.UpgradeWeapon || c.Kind == DraftKind.UpgradePassive));
                Assert.AreEqual(2, cards.Count);
            }
        }

        [Test]
        public void MaxedAndBanishedItemsAreNotOffered()
        {
            var l = new Loadout { WeaponSlots = 1, PassiveSlots = 0, MaxLevel = 2 };
            var weapons = TestContent.Weapons(2);
            l.AddWeapon(weapons[0]);
            l.Weapons[0].Level = 2;
            var cards = DraftRules.Roll(l, weapons, new List<PassiveDef>(), new Rng(1), 0f);
            Assert.AreEqual(3, cards.Count, "three boons when nothing is left");
            Assert.AreEqual(DraftKind.Restore, cards[0].Kind);
            Assert.IsTrue(cards.Skip(1).All(c => c.Kind == DraftKind.Boon));

            var l2 = new Loadout();
            var charges = new DraftCharges();
            Assert.IsTrue(DraftRules.Banish(l2, charges, "w1"));
            Assert.IsFalse(DraftRules.Banish(l2, charges, "w0"), "only one banish");
            for (int seed = 0; seed < 30; seed++)
                Assert.IsFalse(DraftRules.Roll(l2, weapons, new List<PassiveDef>(), new Rng((ulong)seed), 0f).Any(c => c.Id == "w1"));
        }

        [Test]
        public void DryDraftDealsRestoreGoldAndAStatBoon()
        {
            var l = new Loadout { WeaponSlots = 0, PassiveSlots = 0 };
            var cards = DraftRules.Roll(l, TestContent.Weapons(2), TestContent.Passives(2), new Rng(5), 0f, 3, null, 40);
            CollectionAssert.AreEqual(new[] { BoonRules.RestoreId, BoonRules.SpoilsId }, cards.Take(2).Select(c => c.Id));
            Assert.AreEqual(BoonRules.SpoilsGold(40), cards[1].Gold);
            Assert.IsTrue(BoonRules.StatBoons.Any(b => b.Id == cards[2].Id));
            Assert.IsFalse(DraftRules.Banish(l, new DraftCharges(), cards[2].Id), "boons cannot be banished");

            var refreshed = DraftRules.Roll(l, TestContent.Weapons(2), TestContent.Passives(2), new Rng(6), 0f, 3, cards.Select(c => c.Id).ToList(), 40);
            Assert.AreEqual(2, refreshed.Count, "a refresh deals the two other stat boons");
            Assert.IsFalse(refreshed.Any(c => cards.Any(d => d.Id == c.Id)));
        }

        [Test]
        public void StatBoonsStackHyperbolically()
        {
            var b = BoonRules.StatBoons.First(x => x.Stat == StatId.Damage);
            var l = new Loadout();
            float prevTotal = 0f, prevStep = float.MaxValue;
            for (int n = 1; n <= 200; n++)
            {
                Assert.IsTrue(DraftRules.Apply(l, new DraftOption { Kind = DraftKind.Boon, Id = b.Id }, new List<WeaponDef>(), new List<PassiveDef>()));
                float total = BoonRules.Total(b, n);
                Assert.Greater(total, prevTotal);
                Assert.Less(total - prevTotal, prevStep, "each boon adds less");
                Assert.Less(total, b.Cap, "never reaches the cap");
                prevStep = total - prevTotal;
                prevTotal = total;
            }
            Assert.AreEqual(b.Cap * (1f - 1f / (1f + b.Rate)), BoonRules.Total(b, 1), 1e-5f);
            var s = StatBlock.Default();
            s.ApplyAll(l.PassiveMods());
            Assert.AreEqual(1f + BoonRules.Total(b, 200), s.Value(StatId.Damage), 1e-4f);
        }

        [Test]
        public void ExcludeKeepsRefreshedCardsDifferent()
        {
            var l = new Loadout();
            var weapons = TestContent.Weapons(6);
            var first = DraftRules.Roll(l, weapons, new List<PassiveDef>(), new Rng(3), 0f);
            var exclude = first.Select(c => c.Id).ToList();
            var second = DraftRules.Roll(l, weapons, new List<PassiveDef>(), new Rng(4), 0f, 3, exclude);
            Assert.IsFalse(second.Any(c => exclude.Contains(c.Id)));
        }

        [Test]
        public void LuckRaisesAverageRarity()
        {
            float Average(float luck)
            {
                var rng = new Rng(77);
                float sum = 0f;
                for (int i = 0; i < 5000; i++) sum += (int)DraftRules.RollRarity(rng, luck);
                return sum / 5000f;
            }
            Assert.Greater(Average(0.5f), Average(0f) + 0.2f);
        }

        [Test]
        public void ApplyLevelsAndRespectsSlots()
        {
            var l = new Loadout { WeaponSlots = 1 };
            var weapons = TestContent.Weapons(2);
            var passives = TestContent.Passives(1);
            Assert.IsTrue(DraftRules.Apply(l, new DraftOption { Kind = DraftKind.NewWeapon, Id = "w0" }, weapons, passives));
            Assert.IsFalse(DraftRules.Apply(l, new DraftOption { Kind = DraftKind.NewWeapon, Id = "w1" }, weapons, passives));
            var up = new DraftOption { Kind = DraftKind.UpgradeWeapon, Id = "w0" };
            up.WeaponRolls.Add(new WeaponUpgrade(WeaponStat.Damage, 4f));
            Assert.IsTrue(DraftRules.Apply(l, up, weapons, passives));
            Assert.AreEqual(2, l.Weapons[0].Level);
            Assert.AreEqual(4f, l.Weapons[0].Bonus(WeaponStat.Damage), 1e-4f);

            Assert.IsTrue(DraftRules.Apply(l, new DraftOption { Kind = DraftKind.NewPassive, Id = "p0" }, weapons, passives));
            Assert.IsTrue(DraftRules.Apply(l, new DraftOption { Kind = DraftKind.UpgradePassive, Id = "p0", PassiveRoll = new StatMod(StatId.Damage, 0.2f) }, weapons, passives));
            var b = StatBlock.Default();
            b.ApplyAll(l.PassiveMods());
            Assert.AreEqual(1f + 0.1f + 0.2f, b.Value(StatId.Damage), 1e-4f);
        }

        [Test]
        public void RarityScalesUpgrades()
        {
            var dmg = new WeaponUpgrade(WeaponStat.Damage, 4f);
            Assert.AreEqual(4f, DraftRules.ScaleUpgrade(dmg, Rarity.Common), 1e-4f);
            Assert.Greater(DraftRules.ScaleUpgrade(dmg, Rarity.Legendary), DraftRules.ScaleUpgrade(dmg, Rarity.Rare));
            var count = new WeaponUpgrade(WeaponStat.Count, 1f);
            Assert.AreEqual(1f, DraftRules.ScaleUpgrade(count, Rarity.Rare));
            Assert.AreEqual(2f, DraftRules.ScaleUpgrade(count, Rarity.Legendary));
        }

        [Test]
        public void EpicUpgradesRollTwoStats()
        {
            var l = new Loadout { WeaponSlots = 1, PassiveSlots = 0 };
            var weapons = TestContent.Weapons(1);
            l.AddWeapon(weapons[0]);
            bool sawEpic = false;
            for (int seed = 0; seed < 400 && !sawEpic; seed++)
            {
                var card = DraftRules.Roll(l, weapons, new List<PassiveDef>(), new Rng((ulong)seed), 2f)[0];
                if (card.Rarity >= Rarity.Epic)
                {
                    sawEpic = true;
                    Assert.AreEqual(2, card.WeaponRolls.Count);
                    Assert.AreNotEqual(card.WeaponRolls[0].Stat, card.WeaponRolls[1].Stat);
                }
                else Assert.AreEqual(1, card.WeaponRolls.Count);
            }
            Assert.IsTrue(sawEpic);
        }
    }
}
