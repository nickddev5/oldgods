using System;

namespace OldGods.Runtime
{
    /// <summary>Reads Old Gods switches from the player's command line (-smoke, -probe, -seed HEX).</summary>
    public static class CommandLine
    {
        static string[] args;

        /// <summary>Set by tests to simulate arguments.</summary>
        public static string[] Override;

        static string[] Args => Override ?? (args ??= Environment.GetCommandLineArgs());

        public static bool Has(string flag)
        {
            foreach (var a in Args)
                if (string.Equals(a, flag, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        public static string Value(string flag)
        {
            var a = Args;
            for (int i = 0; i < a.Length - 1; i++)
                if (string.Equals(a[i], flag, StringComparison.OrdinalIgnoreCase)) return a[i + 1];
            return null;
        }
    }
}
