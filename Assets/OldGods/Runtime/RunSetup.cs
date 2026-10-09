using System.Collections.Generic;

namespace OldGods.Runtime
{
    /// <summary>
    /// What the menus chose for the next run: the god and difficulty modifiers. Read by
    /// RunController at start; set by character select. Survives the scene load.
    /// </summary>
    public static class RunSetup
    {
        /// <summary>The chosen god's id; null means the first unlocked god.</summary>
        public static string GodId;
        public static readonly List<string> Modifiers = new List<string>();
        /// <summary>Set to skip menus straight into a run (tests, smoke, Play from Run.unity).</summary>
        public static bool Direct = true;
    }
}
