using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Core
{
    /// <summary>
    /// How many NWNX managed entry points (main loop, script, closure, signal) are on the stack
    /// while a combat recording runs. Zero inside an engine callback means the engine itself is
    /// acting, for example applying a weapon hit, rather than a script or closure.
    /// </summary>
    public static class ManagedEntryDepth
    {
        public static int Current { get; private set; }

        /// <summary>
        /// Counts only while recording, so the entry points pay a single flag check otherwise.
        /// Pass the result to <see cref="Exit"/>.
        /// </summary>
        internal static bool Enter()
        {
            if (!CombatRecorder.IsRecording)
                return false;

            Current++;
            return true;
        }

        internal static void Exit(bool entered)
        {
            if (entered)
                Current--;
        }
    }
}
