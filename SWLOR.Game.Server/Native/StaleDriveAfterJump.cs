using System;
using SWLOR.Game.Contracts;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;

namespace SWLOR.Game.Server.Native
{
    /// <summary>
    /// Keyboard movement sends drive paths that start at the client's own position. When a
    /// teleport lands while the player holds a movement key, the client can keep driving from
    /// where it stood before the jump, and the engine accepts those starts and pulls the creature
    /// back. Shadow Step and Force Leap were undone this way. After a teleport, a drive path that
    /// starts far from the creature's server position starts from the server position instead,
    /// until the client sends a path from the server position.
    /// </summary>
    public static class StaleDriveAfterJump
    {
        public const float StaleStartDistanceMeters = 2f;
        public const long JumpWindowMilliseconds = 1500;
        public const long CorrectionTimeoutMilliseconds = 10000;

        [BootPhase(BootReload.NativeHook)]
        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void RegisterHooks()
        {
            NativeHookAccess.Hooks.Enable(NativeHookId.StaleDriveAfterJump);
        }

        /// <summary>
        /// A jump that moved the creature farther than a stale start can be told apart from
        /// ordinary client drift.
        /// </summary>
        public static bool IsTeleport(float fromX, float fromY, float toX, float toY)
        {
            return HorizontalDistance(fromX, fromY, toX, toY) > StaleStartDistanceMeters;
        }

        /// <summary>
        /// Decides how to handle a drive path that arrives after a teleport and updates the watch.
        /// </summary>
        /// <param name="state">The creature's watch, updated when a path is restarted.</param>
        /// <param name="now">The current time in milliseconds.</param>
        /// <param name="startGapMeters">Horizontal distance between the path start and the server position.</param>
        /// <param name="clientPathNumber">The client's path number. The engine continues a path under the same number.</param>
        public static StaleDriveDecision Decide(ref StaleDriveState state, long now, float startGapMeters, int clientPathNumber)
        {
            if (startGapMeters > StaleStartDistanceMeters)
            {
                var watching = state.Correcting
                    ? now - state.LastCorrectionAt <= CorrectionTimeoutMilliseconds
                    : now - state.JumpedAt >= 0 && now - state.JumpedAt <= JumpWindowMilliseconds;
                if (!watching)
                    return StaleDriveDecision.Release;

                state.Correcting = true;
                state.LastCorrectionAt = now;
                state.LastPathNumber = clientPathNumber;
                return StaleDriveDecision.StartFromServer;
            }

            // The engine continues a restarted path from the server position under the same number.
            // A new path that starts at the server position means the client has caught up.
            if (state.Correcting)
                return clientPathNumber == state.LastPathNumber
                    ? StaleDriveDecision.LeaveAlone
                    : StaleDriveDecision.Release;

            return now - state.JumpedAt > JumpWindowMilliseconds
                ? StaleDriveDecision.Release
                : StaleDriveDecision.LeaveAlone;
        }

        internal static bool IsExpired(StaleDriveState state, long now)
        {
            return state.Correcting
                ? now - state.LastCorrectionAt > CorrectionTimeoutMilliseconds
                : now - state.JumpedAt > JumpWindowMilliseconds;
        }

        internal static float HorizontalDistance(float ax, float ay, float bx, float by)
        {
            var dx = ax - bx;
            var dy = ay - by;
            return MathF.Sqrt(dx * dx + dy * dy);
        }
    }
}
