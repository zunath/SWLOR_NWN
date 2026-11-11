using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using NWN.Native.API;
using NWNX.NET;
using NWNX.NET.Native;
using SWLOR.Game.Contracts;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.NativeSinks;

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
    public static unsafe class StaleDriveAfterJump
    {
        public const float StaleStartDistanceMeters = 2f;
        public const long JumpWindowMilliseconds = 1500;
        public const long CorrectionTimeoutMilliseconds = 10000;

        private static FunctionHook* _jumpHook;
        private static FunctionHook* _driveHook;
        private static IStaleDriveAfterJumpSink _sink;

        [BootPhase(BootReload.NativeHook)]
        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void RegisterHooks()
        {
            if (_jumpHook != null)
                return;

            _sink ??= new StaleDriveAfterJumpSink();
            var library = NativeLibrary.GetMainProgramHandle();
            delegate* unmanaged<void*, void*, uint> jump = &OnAIActionJumpToPoint;
            delegate* unmanaged<void*, ushort, void*, int, int, int, int, int> drive = &OnAddDriveAction;
            _jumpHook = NWNXAPI.RequestFunctionHook(
                NativeLibrary.GetExport(library, "_ZN12CNWSCreature19AIActionJumpToPointEP20CNWSObjectActionNode"),
                (IntPtr)jump, HookOrder.Late);
            _driveHook = NWNXAPI.RequestFunctionHook(
                NativeLibrary.GetExport(library, "_ZN12CNWSCreature14AddDriveActionEtRK6Vectoriiii"),
                (IntPtr)drive, HookOrder.Late);
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

        [UnmanagedCallersOnly]
        private static uint OnAIActionJumpToPoint(void* creature, void* node)
            => _sink.JumpToPoint(creature, node, (nint)_jumpHook->m_trampoline);

        [UnmanagedCallersOnly]
        private static int OnAddDriveAction(void* creature, ushort groupId, void* pathStart, int bearing,
            int clientPathNumber, int driveFlags, int wayPointsToGenerate)
            => _sink.AddDriveAction(creature, groupId, pathStart, bearing, clientPathNumber, driveFlags,
                wayPointsToGenerate, (nint)_driveHook->m_trampoline);
    }
}
