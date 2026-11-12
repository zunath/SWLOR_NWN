using SWLOR.Game.Contracts;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.NativeSinks;

namespace SWLOR.Game.Server.Native
{
    /// <summary>
    /// Enables the nine chat send hooks that let <see cref="ChatPortraitSpeakersSink"/> watch each chat line the engine
    /// sends to one client. Every player chat channel the engine fans out, including NWNX Chat sends and SpeakString,
    /// reaches one of these functions per recipient; each only forwards to the sink. The hooks themselves are
    /// <c>SWLOR.Game.NativeHooks.ChatPortraitSpeakersTrampolines</c>.
    /// </summary>
    public static class ChatPortraitSpeakers
    {
        [BootPhase(BootReload.NativeHook)]
        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void RegisterHooks()
        {
            NativeHookAccess.Hooks.Enable(NativeHookId.ChatPortraitSpeakers);
        }
    }
}
