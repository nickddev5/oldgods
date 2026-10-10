using System;
using System.Collections.Generic;

namespace OldGods.Rules
{
    /// <summary>
    /// A yes/no decision tree: each branch asks one question of the state, each leaf gives an
    /// answer. Evaluating it also returns the path taken ("Boss awake > yes"), so a report can
    /// say why the bot did what it did.
    /// </summary>
    public sealed class DecisionTree<TState, TResult>
    {
        readonly string question;
        readonly Func<TState, bool> test;
        readonly DecisionTree<TState, TResult> yes, no;
        readonly string leafName;
        readonly Func<TState, TResult> leaf;

        DecisionTree(string question, Func<TState, bool> test, DecisionTree<TState, TResult> yes, DecisionTree<TState, TResult> no)
        {
            this.question = question;
            this.test = test;
            this.yes = yes;
            this.no = no;
        }

        DecisionTree(string name, Func<TState, TResult> leaf)
        {
            leafName = name;
            this.leaf = leaf;
        }

        public static DecisionTree<TState, TResult> Ask(string question, Func<TState, bool> test, DecisionTree<TState, TResult> yes, DecisionTree<TState, TResult> no) =>
            new DecisionTree<TState, TResult>(question, test, yes, no);

        public static DecisionTree<TState, TResult> Do(string name, Func<TState, TResult> leaf) => new DecisionTree<TState, TResult>(name, leaf);

        public static DecisionTree<TState, TResult> Do(string name, TResult result) => Do(name, _ => result);

        public bool IsLeaf => leaf != null;

        /// <summary>Walks the tree for a state. Path names the leaf reached, e.g. "Hurt badly: recover".</summary>
        public TResult Decide(TState state, out string path)
        {
            var node = this;
            string lastQuestion = null;
            bool lastAnswer = false;
            while (!node.IsLeaf)
            {
                lastQuestion = node.question;
                lastAnswer = node.test(state);
                node = lastAnswer ? node.yes : node.no;
            }
            path = lastQuestion == null ? node.leafName : $"{lastQuestion} {(lastAnswer ? "yes" : "no")}: {node.leafName}";
            return node.leaf(state);
        }

        /// <summary>Every leaf name, in order, for documentation and tests.</summary>
        public List<string> Leaves()
        {
            var list = new List<string>();
            Collect(list);
            return list;
        }

        void Collect(List<string> list)
        {
            if (IsLeaf) { list.Add(leafName); return; }
            yes.Collect(list);
            no.Collect(list);
        }
    }

    /// <summary>What the bot is walking toward.</summary>
    public enum BotGoal { Explore, Gem, Feature, Charge, Gate, Boss, Portal, Arena, Recover }

    /// <summary>How the bot treats the horde while it walks: keep away, or stay at its weapon's reach.</summary>
    public enum BotStance { Kite, Close }

    /// <summary>What the bot knows when it decides. Filled by the runtime each time it rethinks.</summary>
    public struct BotSituation
    {
        public bool FinalArena, BossAwake, BossDown, FreeChestNear, PortalOpen, GateUsable, FeatureWanted, FeatureIsCharge, GemNear, Threatened;
        public float Health;            // 0..1
        public float StageClock;        // 0..1 of the stage's timeline
        public float BossAt;            // fraction of the clock at which the bot wakes the boss
        public float CloseReach;        // metres the bot's shortest-reaching weapon hurts at (claws, auras)
        public int Touching;            // enemies within 3 m
    }

    /// <summary>The bot's two decision trees and the numbers they use. PLACEHOLDER thresholds.</summary>
    public static class BotTrees
    {
        /// <summary>A weapon reaching less than this is fought up close (claws, auras, orbiters).</summary>
        public const float CloseReach = 5f;
        public const float HurtBadly = 0.3f;
        public const float HurtForBoss = 0.5f;
        public const float BackOffHealth = 0.5f;
        public const int Swarmed = 4;

        /// <summary>Where to go next.</summary>
        public static readonly DecisionTree<BotSituation, BotGoal> Goal = Build();

        /// <summary>
        /// How to treat the horde on the way: holding any close-range weapon, the bot stays at its
        /// reach (ranged weapons hit from there too) unless hurt or swarmed.
        /// </summary>
        public static readonly DecisionTree<BotSituation, BotStance> Stance =
            DecisionTree<BotSituation, BotStance>.Ask("Holds a close-range weapon", s => s.CloseReach < CloseReach,
                DecisionTree<BotSituation, BotStance>.Ask("Hurt or swarmed", s => s.Health < BackOffHealth || s.Touching >= Swarmed,
                    DecisionTree<BotSituation, BotStance>.Do("back off", BotStance.Kite),
                    DecisionTree<BotSituation, BotStance>.Do("stay close", BotStance.Close)),
                DecisionTree<BotSituation, BotStance>.Do("kite", BotStance.Kite));

        static DecisionTree<BotSituation, BotGoal> Build()
        {
            DecisionTree<BotSituation, BotGoal> Ask(string q, Func<BotSituation, bool> t, DecisionTree<BotSituation, BotGoal> y, DecisionTree<BotSituation, BotGoal> n) =>
                DecisionTree<BotSituation, BotGoal>.Ask(q, t, y, n);
            DecisionTree<BotSituation, BotGoal> Do(string name, BotGoal g) => DecisionTree<BotSituation, BotGoal>.Do(name, g);

            var roam =
                Ask("Something worth using nearby", s => s.FeatureWanted,
                    Ask("It is a charge shrine", s => s.FeatureIsCharge, Do("charge the shrine", BotGoal.Charge), Do("use it", BotGoal.Feature)),
                    Ask("Gems close and no enemy on top", s => s.GemNear && !s.Threatened, Do("collect", BotGoal.Gem), Do("explore", BotGoal.Explore)));

            var stage =
                Ask("Badly hurt", s => s.Health < HurtBadly, Do("recover", BotGoal.Recover),
                    Ask("Time for the boss", s => s.GateUsable && s.StageClock >= s.BossAt && (s.Health >= HurtForBoss || s.StageClock >= 0.92f),
                        Do("wake the boss", BotGoal.Gate), roam));

            return Ask("In The Last Test", s => s.FinalArena, Do("fight the last boss", BotGoal.Arena),
                Ask("Boss awake", s => s.BossAwake, Do("fight the boss", BotGoal.Boss),
                    Ask("Boss down", s => s.BossDown,
                        Ask("Its free chest nearby", s => s.FreeChestNear, Do("open the boss chest", BotGoal.Feature),
                            Ask("Portal open", s => s.PortalOpen, Do("take the portal", BotGoal.Portal), Do("explore", BotGoal.Explore))),
                        stage)));
        }

        /// <summary>
        /// How far from the player a weapon hurts: the swing for claws, the field for auras, the
        /// ring for orbiters, the burst for player-centred strikes, otherwise its targeting range.
        /// </summary>
        public static float Reach(WeaponShape shape, in EffectiveWeapon e)
        {
            switch (shape)
            {
                case WeaponShape.Swipe:
                case WeaponShape.Aura: return e.Size;
                case WeaponShape.Orbit: return e.Range + e.Size * 0.5f;
                case WeaponShape.Area: return e.Range > 0f ? e.Range : e.Size * 1.5f;
                default: return e.Range;
            }
        }

        /// <summary>
        /// The distance a close-range bot holds from the nearest enemy: inside its reach with a
        /// margin, and not so close that it stands in every body.
        /// </summary>
        public static float HoldDistance(float reach) => Math.Max(1.2f, reach * 0.65f);
    }
}
