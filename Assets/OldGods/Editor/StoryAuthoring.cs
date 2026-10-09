using System.Collections.Generic;
using OldGods.Runtime;
using UnityEditor;
using UnityEngine;

namespace OldGods.Editor
{
    /// <summary>
    /// Creates the story asset once. Every line is PLACEHOLDER (gameBible/08-story.md) and must
    /// agree with The Empty Throne's "Before the throne" canon: the throne stood empty, the old
    /// gods never shared it, Elias sat because he believed it right, their power merged into him
    /// and they live on in him, and The Last Test guards the throne from the unworthy.
    /// </summary>
    public static class StoryAuthoring
    {
        public const string StoryPath = ProjectBuilder.Root + "/Content/Story.asset";

        static StoryText.Line L(string id, string text) => new StoryText.Line { Id = id, Text = text };

        static StoryText.Fragment F(string id, string biome, string title, string text) =>
            new StoryText.Fragment { Id = id, BiomeId = biome, Title = title, Text = text };

        public static StoryText Create()
        {
            var existing = AssetDatabase.LoadAssetAtPath<StoryText>(StoryPath);
            if (existing != null) return existing;
            var s = ScriptableObject.CreateInstance<StoryText>();
            s.Premise =
                "Before anyone held the throne, there were the old gods. Many of them, each with a domain and a people.\n\n" +
                "Between them stood a throne no one had claimed. It stood empty through every age, guarded by a construct they called The Last Test.\n\n" +
                "Now the old gods walk the road to it. Some go to see it. Some go to break its guardian. One will sit.";
            s.GodLines = new List<StoryText.Line>
            {
                L("god.storm", "The sky gathers behind you."),
                L("god.forge", "The fire in your hands has never gone out."),
                L("god.tide", "The water remembers where it is going."),
                L("god.hunt", "You have already chosen your mark."),
                L("god.ember", "A small flame, walking a long road."),
                L("god.earth", "Each step is a promise to the ground."),
                L("god.elias", "You believe the throne should be taken. You walk to take it."),
            };
            s.BiomeLines = new List<StoryText.Line>
            {
                L("biome.grey_steppe", "The first road, and the wall that guards it."),
                L("biome.ash_wood", "It burned, and it has not stopped burning."),
                L("biome.drowned_coast", "The sea takes, and keeps."),
                L("biome.last_test", "The throne waits."),
                L("biome.greybox", "A place to learn the road."),
            };
            s.EliasEnding = new List<string>
            {
                "Elias climbs to the throne. No one has sat here before him.",
                "He sits because he believes it is right.",
                "The power of the old gods flows into him. One by one, they are gone from the world.",
                "They live on in him. The throne is empty no longer.",
            };
            s.RefusedEnding = new List<string>
            {
                "The Last Test falls.",
                "The throne does not answer you. It was never meant for you.",
                "Somewhere behind you, Elias is still walking.",
            };
            s.Fragments = new List<StoryText.Fragment>
            {
                F("lore.empty_seat", "biome.grey_steppe", "The Empty Seat",
                    "No god ever sat the throne. The old gods walked past it for ages, each of them sure it was meant for someone else."),
                F("lore.wardens_wall", "biome.grey_steppe", "The Warden's Wall",
                    "The Stone Warden was raised to keep the steppe's first road. When the road was forgotten, it kept walking the wall anyway."),
                F("lore.many_gods", "biome.grey_steppe", "Many Gods",
                    "There were many of us once. Storm and Forge, Tide and Hunt, Ember and Earth, and others whose names the grass has taken. None of us ruled. That was the point."),
                F("lore.the_burning", "biome.ash_wood", "The Burning",
                    "The wood burned in a quarrel between gods that no one now remembers starting. The stag walked out of the fire, and it has not stopped burning since."),
                F("lore.quiet_god", "biome.ash_wood", "A Quiet God",
                    "Elias was the quietest of us. He listened more than he spoke. Some of us mistook that for weakness."),
                F("lore.ash_and_memory", "biome.ash_wood", "Ash and Memory",
                    "Fire keeps what it takes. Stand close to the old embers and you can hear the gods arguing about the throne."),
                F("lore.what_the_sea_kept", "biome.drowned_coast", "What the Sea Kept",
                    "The Tide Mother keeps everything the sea takes: ships, songs, the dead. She does not give them back. She thinks of it as care."),
                F("lore.the_construct", "biome.drowned_coast", "The Last Test",
                    "Someone made a guardian for the throne and called it The Last Test. It keeps the throne from the unworthy. No one remembers who made it, or who made the throne."),
                F("lore.the_choice", "biome.drowned_coast", "The Choice",
                    "We could share the world, or let one of us hold it. We argued for an age. In the end one of us decided, and the rest of us followed him there."),
            };
            ProjectBuilder.EnsureFolder(System.IO.Path.GetDirectoryName(StoryPath).Replace('\\', '/'));
            AssetDatabase.CreateAsset(s, StoryPath);
            return s;
        }
    }
}
