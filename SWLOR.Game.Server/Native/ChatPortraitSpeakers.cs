using System.Runtime.InteropServices;
using NWNX.NET;
using NWNX.NET.Native;
using SWLOR.Game.Contracts;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.NativeSinks;

namespace SWLOR.Game.Server.Native
{
    /// <summary>
    /// Installs the nine chat send hooks that let <see cref="ChatPortraitSpeakersSink"/> watch each chat line the engine
    /// sends to one client. Every player chat channel the engine fans out, including NWNX Chat sends and SpeakString,
    /// reaches one of these functions per recipient; each only forwards to the sink.
    /// </summary>
    public static unsafe class ChatPortraitSpeakers
    {
        // Each takes (CNWSMessage*, recipient player ID, speaker object ID, CExoString by value).
        private static readonly string[] Symbols =
        {
            "_ZN11CNWSMessage27SendServerToPlayerChat_TalkEjj10CExoString",
            "_ZN11CNWSMessage30SendServerToPlayerChat_WhisperEjj10CExoString",
            "_ZN11CNWSMessage28SendServerToPlayerChat_PartyEjj10CExoString",
            "_ZN11CNWSMessage27SendServerToPlayerChat_TellEjj10CExoString",
            "_ZN11CNWSMessage28SendServerToPlayerChat_ShoutEjj10CExoString",
            "_ZN11CNWSMessage35SendServerToPlayerChat_Silent_ShoutEjj10CExoString",
            "_ZN11CNWSMessage30SendServerToPlayerChat_DM_TalkEjj10CExoString",
            "_ZN11CNWSMessage33SendServerToPlayerChat_DM_WhisperEjj10CExoString",
            "_ZN11CNWSMessage38SendServerToPlayerChat_DM_Silent_ShoutEjj10CExoString",
        };

        private static readonly delegate* unmanaged<void*, uint, uint, void*, int>[] Originals =
            new delegate* unmanaged<void*, uint, uint, void*, int>[Symbols.Length];

        private static IChatPortraitSpeakersSink _sink;

        [BootPhase(BootReload.NativeHook)]
        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void RegisterHooks()
        {
            _sink ??= new ChatPortraitSpeakersSink();
            delegate* unmanaged<void*, uint, uint, void*, int>[] hooks =
            {
                &Talk, &Whisper, &Party, &Tell, &Shout, &SilentShout, &DMTalk, &DMWhisper, &DMSilentShout,
            };

            for (var index = 0; index < Symbols.Length; index++)
            {
                if (Originals[index] != null)
                    continue;

                // Latest: a hook that drops the line before the engine sends it never reaches this one.
                var function = NativeLibrary.GetExport(NativeLibrary.GetMainProgramHandle(), Symbols[index]);
                var functionHook = NWNXAPI.RequestFunctionHook(function, (IntPtr)hooks[index], HookOrder.Latest);
                Originals[index] = (delegate* unmanaged<void*, uint, uint, void*, int>)functionHook->m_trampoline;
            }
        }

        [UnmanagedCallersOnly]
        private static int Talk(void* message, uint recipient, uint speaker, void* text) => _sink.Send(message, recipient, speaker, text, (nint)Originals[0]);

        [UnmanagedCallersOnly]
        private static int Whisper(void* message, uint recipient, uint speaker, void* text) => _sink.Send(message, recipient, speaker, text, (nint)Originals[1]);

        [UnmanagedCallersOnly]
        private static int Party(void* message, uint recipient, uint speaker, void* text) => _sink.Send(message, recipient, speaker, text, (nint)Originals[2]);

        [UnmanagedCallersOnly]
        private static int Tell(void* message, uint recipient, uint speaker, void* text) => _sink.Send(message, recipient, speaker, text, (nint)Originals[3]);

        [UnmanagedCallersOnly]
        private static int Shout(void* message, uint recipient, uint speaker, void* text) => _sink.Send(message, recipient, speaker, text, (nint)Originals[4]);

        [UnmanagedCallersOnly]
        private static int SilentShout(void* message, uint recipient, uint speaker, void* text) => _sink.Send(message, recipient, speaker, text, (nint)Originals[5]);

        [UnmanagedCallersOnly]
        private static int DMTalk(void* message, uint recipient, uint speaker, void* text) => _sink.Send(message, recipient, speaker, text, (nint)Originals[6]);

        [UnmanagedCallersOnly]
        private static int DMWhisper(void* message, uint recipient, uint speaker, void* text) => _sink.Send(message, recipient, speaker, text, (nint)Originals[7]);

        [UnmanagedCallersOnly]
        private static int DMSilentShout(void* message, uint recipient, uint speaker, void* text) => _sink.Send(message, recipient, speaker, text, (nint)Originals[8]);
    }
}
