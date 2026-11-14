using SWLOR.Game.Contracts;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.NativeSinks;

namespace SWLOR.Game.Server.Native
{
    /// <summary>
    /// Makes the JSON that clients send with NUI events and bind updates safe for the engine to decode and log: enables
    /// the <c>CNWMessage::ReadJSON</c> hook that forwards to <see cref="ClientJsonUtf8GuardSink"/>, and keeps its counters.
    /// </summary>
    public static class ClientJsonUtf8Guard
    {
        /// <summary>The number of invalid UTF-8 bytes replaced since the module loaded.</summary>
        public static long ReplacedByteCount { get; internal set; }

        /// <summary>The number of client JSON payloads made unreadable since the module loaded.</summary>
        public static long RejectedPayloadCount { get; internal set; }

        [BootPhase(BootReload.NativeHook)]
        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void RegisterHook()
        {
            NativeHookAccess.Hooks.Enable(NativeHookId.ClientJsonUtf8Guard);
        }
    }
}
