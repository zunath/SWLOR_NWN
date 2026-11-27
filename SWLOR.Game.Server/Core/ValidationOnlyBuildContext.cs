using System.Threading;

namespace SWLOR.Game.Server.Core
{
    /// <summary>
    /// The ambient "validation-only build" flag of the current async flow: window layouts are built without the live
    /// NWN engine so ordinary unit tests can validate them. It is process-wide build-mode state, not game state, so it
    /// lives outside the GUI service and works without a container (<c>GuiLayoutValidator.BeginValidationOnlyBuild</c>
    /// is the public entry).
    /// </summary>
    internal static class ValidationOnlyBuildContext
    {
        private static readonly AsyncLocal<int> Depth = new();

        internal static bool IsActive => Depth.Value > 0;

        internal static void Enter() => Depth.Value++;

        internal static void Exit() => Depth.Value--;
    }
}
