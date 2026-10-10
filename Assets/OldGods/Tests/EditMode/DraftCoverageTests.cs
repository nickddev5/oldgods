using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using OldGods.Rules;
using OldGods.Runtime;
using UnityEditor;

namespace OldGods.Tests.EditMode
{
    /// <summary>
    /// Deals whole runs of drafts from the real content set, everything unlocked, the way the
    /// play bot takes cards. Guards against a weapon or passive that is never dealt.
    /// </summary>
    public class DraftCoverageTests
    {
        const string LibraryPath = "Assets/OldGods/Content/ContentLibrary.asset";
        static readonly ulong[] Seeds = { 0x1111, 0x2222, 0x3333, 0x4444, 0x5555, 0x6666, 0x7777, 0x8888 };

        static ContentSet Content()
        {
            var lib = AssetDatabase.LoadAssetAtPath<ContentLibrary>(LibraryPath);
            Assert.IsNotNull(lib, "content library missing");
            return lib.Load();
        }

        /// <summary>Plays the draft of one run: the god's start, then 80 level-ups taken as the smart bot would.</summary>
        static void PlayDraft(ContentSet content, GodDef god, RunSeed seed, Dictionary<string, int> offered)
        {
            var loadout = new Loadout();
            if (!string.IsNullOrEmpty(god.StartingWeapon)) loadout.AddWeapon(content.Weapon(god.StartingWeapon));
            if (!string.IsNullOrEmpty(god.StartingPassive)) loadout.AddPassive(content.Passive(god.StartingPassive));
            var deal = seed.DraftStream(god.Id);
            var pick = seed.Stream("playbot.draft", god.Id);
            for (int level = 2; level <= 80; level++)
            {
                var cards = DraftRules.Roll(loadout, content.Weapons, content.Passives, deal, 0f);
                foreach (var c in cards)
                    if (c.Kind != DraftKind.Restore) offered[c.Id] = offered.TryGetValue(c.Id, out int n) ? n + 1 : 1;
                DraftRules.Apply(loadout, cards[PlayBotRules.PickDraft(cards, loadout, 1f, BotPicks.Smart, pick)], content.Weapons, content.Passives);
            }
        }

        [Test]
        public void EveryWeaponAndPassiveIsDealtAcrossGodsAndSeeds()
        {
            var content = Content();
            var offered = new Dictionary<string, int>();
            foreach (var god in content.Gods)
                foreach (var s in Seeds)
                    PlayDraft(content, god, new RunSeed(s), offered);

            foreach (var w in content.Weapons)
                Assert.Greater(offered.TryGetValue(w.Id, out int n) ? n : 0, 0, $"{w.Id} was never dealt");
            foreach (var p in content.Passives)
                Assert.Greater(offered.TryGetValue(p.Id, out int n) ? n : 0, 0, $"{p.Id} was never dealt");

            // A new weapon is dealt as fairly as any other: no weapon below a third of the mean.
            var counts = content.Weapons.Select(w => offered.TryGetValue(w.Id, out int n) ? n : 0).ToList();
            double mean = counts.Average();
            for (int i = 0; i < counts.Count; i++)
                Assert.Greater(counts[i], mean / 3.0, $"{content.Weapons[i].Id} dealt {counts[i]} times, mean {mean:0}");
        }

        [Test]
        public void GodsOnTheSameSeedAreDealtDifferentCards()
        {
            var seed = new RunSeed(0x1111);
            var loadout = new Loadout();
            var content = Content();
            var a = DraftRules.Roll(loadout, content.Weapons, content.Passives, seed.DraftStream("god.storm"), 0f);
            var b = DraftRules.Roll(loadout, content.Weapons, content.Passives, seed.DraftStream("god.tide"), 0f);
            var again = DraftRules.Roll(loadout, content.Weapons, content.Passives, seed.DraftStream("god.storm"), 0f);
            CollectionAssert.AreEqual(a.Select(c => c.Id), again.Select(c => c.Id), "same god, same seed, same cards");
            CollectionAssert.AreNotEqual(a.Select(c => c.Id), b.Select(c => c.Id), "another god on the seed gets its own cards");
        }
    }
}
