using System;
using System.Collections.Generic;
using UnityEngine;

namespace OldGods.Runtime
{
    /// <summary>
    /// Every line of story in the game, in one asset so it can be rewritten in one place.
    /// All of it is PLACEHOLDER until Nick marks it FINAL in gameBible/08-story.md.
    /// </summary>
    [CreateAssetMenu(menuName = "Old Gods/Story Text", fileName = "Story")]
    public sealed class StoryText : ScriptableObject
    {
        [Serializable]
        public struct Line
        {
            public string Id;
            [TextArea] public string Text;
        }

        [Serializable]
        public struct Fragment
        {
            public string Id;
            public string BiomeId;
            public string Title;
            [TextArea(3, 8)] public string Text;
        }

        [TextArea(4, 12)] public string Premise = "";
        [Tooltip("Shown when a run starts, by god id.")]
        public List<Line> GodLines = new List<Line>();
        [Tooltip("Shown under the biome name when a stage starts, by biome id.")]
        public List<Line> BiomeLines = new List<Line>();
        public List<string> EliasEnding = new List<string>();
        public List<string> RefusedEnding = new List<string>();
        public List<Fragment> Fragments = new List<Fragment>();

        public string GodLine(string id) => GodLines.Find(l => l.Id == id).Text;
        public string BiomeLine(string id) => BiomeLines.Find(l => l.Id == id).Text;
    }
}
