using System.Runtime.InteropServices;
using NWNX.NET;
using NWNX.NET.Native;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Native
{
    /// <summary>
    /// Watches each chat line the engine sends to one client, so <see cref="ChatPortraitTells"/> knows which
    /// speaker's portrait that client's chat log holds for each connection slot. Every player chat channel the
    /// engine fans out, including NWNX Chat sends and SpeakString, reaches one of these functions per recipient.
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

        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void RegisterHooks()
        {
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

        private static int Send(int index, void* message, uint recipient, uint speaker, void* text)
        {
            try
            {
                ChatPortraitTells.RecordChatLine(recipient, speaker);
            }
            catch (Exception exception)
            {
                Log.WriteError(exception, "Could not record a chat portrait speaker");
            }

            return Originals[index](message, recipient, speaker, text);
        }

        [UnmanagedCallersOnly]
        private static int Talk(void* message, uint recipient, uint speaker, void* text) => Send(0, message, recipient, speaker, text);

        [UnmanagedCallersOnly]
        private static int Whisper(void* message, uint recipient, uint speaker, void* text) => Send(1, message, recipient, speaker, text);

        [UnmanagedCallersOnly]
        private static int Party(void* message, uint recipient, uint speaker, void* text) => Send(2, message, recipient, speaker, text);

        [UnmanagedCallersOnly]
        private static int Tell(void* message, uint recipient, uint speaker, void* text) => Send(3, message, recipient, speaker, text);

        [UnmanagedCallersOnly]
        private static int Shout(void* message, uint recipient, uint speaker, void* text) => Send(4, message, recipient, speaker, text);

        [UnmanagedCallersOnly]
        private static int SilentShout(void* message, uint recipient, uint speaker, void* text) => Send(5, message, recipient, speaker, text);

        [UnmanagedCallersOnly]
        private static int DMTalk(void* message, uint recipient, uint speaker, void* text) => Send(6, message, recipient, speaker, text);

        [UnmanagedCallersOnly]
        private static int DMWhisper(void* message, uint recipient, uint speaker, void* text) => Send(7, message, recipient, speaker, text);

        [UnmanagedCallersOnly]
        private static int DMSilentShout(void* message, uint recipient, uint speaker, void* text) => Send(8, message, recipient, speaker, text);
    }
}
