using System.Runtime.InteropServices;
using NWNX.NET.Native;
using SWLOR.Game.Contracts;

namespace SWLOR.Game.NativeHooks;

/// <summary>
/// The nine chat send hooks of the game's <c>Native/ChatPortraitSpeakers</c>: every player chat channel the engine fans
/// out reaches one of these functions per recipient. Each hook is installed on its own (a failed one is retried alone)
/// and keeps the trampoline NWNX gave it when it was installed.
/// </summary>
internal static unsafe class ChatPortraitSpeakersTrampolines
{
    // Each takes (CNWSMessage*, recipient player ID, speaker object ID, CExoString by value).
    internal static readonly string[] Symbols =
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

    private static readonly nint[] Functions = new nint[Symbols.Length];

    private static readonly delegate* unmanaged<void*, uint, uint, void*, int>[] Originals =
        new delegate* unmanaged<void*, uint, uint, void*, int>[Symbols.Length];

    private static bool _resolved;
    private static volatile IChatPortraitSpeakersSink? _sink;

    internal static bool IsEnabled
    {
        get
        {
            foreach (var original in Originals)
            {
                if (original == null)
                    return false;
            }

            return true;
        }
    }

    internal static void Resolve(INativeFunctions native)
    {
        if (_resolved)
            return;

        for (var index = 0; index < Symbols.Length; index++)
            Functions[index] = native.GetExport(Symbols[index]);
        _resolved = true;
    }

    internal static void Enable(INativeFunctions native)
    {
        Resolve(native);
        delegate* unmanaged<void*, uint, uint, void*, int>[] hooks =
        {
            &Talk, &Whisper, &Party, &Tell, &Shout, &SilentShout, &DMTalk, &DMWhisper, &DMSilentShout,
        };

        for (var index = 0; index < Symbols.Length; index++)
        {
            if (Originals[index] != null)
                continue;

            // Latest: a hook that drops the line before the engine sends it never reaches this one.
            var functionHook = native.RequestHook(Functions[index], (nint)hooks[index], HookOrder.Latest);
            Originals[index] = (delegate* unmanaged<void*, uint, uint, void*, int>)functionHook->m_trampoline;
        }
    }

    internal static IChatPortraitSpeakersSink? Sink => _sink;

    internal static void Bind(IChatPortraitSpeakersSink sink) => _sink = sink;

    internal static void Unbind(IChatPortraitSpeakersSink sink)
    {
        if (ReferenceEquals(_sink, sink))
            _sink = null;
    }

    [UnmanagedCallersOnly]
    private static int Talk(void* message, uint recipient, uint speaker, void* text)
    {
        var sink = _sink;
        var original = Originals[0];
        return sink is null ? original(message, recipient, speaker, text) : sink.Send(message, recipient, speaker, text, (nint)original);
    }

    [UnmanagedCallersOnly]
    private static int Whisper(void* message, uint recipient, uint speaker, void* text)
    {
        var sink = _sink;
        var original = Originals[1];
        return sink is null ? original(message, recipient, speaker, text) : sink.Send(message, recipient, speaker, text, (nint)original);
    }

    [UnmanagedCallersOnly]
    private static int Party(void* message, uint recipient, uint speaker, void* text)
    {
        var sink = _sink;
        var original = Originals[2];
        return sink is null ? original(message, recipient, speaker, text) : sink.Send(message, recipient, speaker, text, (nint)original);
    }

    [UnmanagedCallersOnly]
    private static int Tell(void* message, uint recipient, uint speaker, void* text)
    {
        var sink = _sink;
        var original = Originals[3];
        return sink is null ? original(message, recipient, speaker, text) : sink.Send(message, recipient, speaker, text, (nint)original);
    }

    [UnmanagedCallersOnly]
    private static int Shout(void* message, uint recipient, uint speaker, void* text)
    {
        var sink = _sink;
        var original = Originals[4];
        return sink is null ? original(message, recipient, speaker, text) : sink.Send(message, recipient, speaker, text, (nint)original);
    }

    [UnmanagedCallersOnly]
    private static int SilentShout(void* message, uint recipient, uint speaker, void* text)
    {
        var sink = _sink;
        var original = Originals[5];
        return sink is null ? original(message, recipient, speaker, text) : sink.Send(message, recipient, speaker, text, (nint)original);
    }

    [UnmanagedCallersOnly]
    private static int DMTalk(void* message, uint recipient, uint speaker, void* text)
    {
        var sink = _sink;
        var original = Originals[6];
        return sink is null ? original(message, recipient, speaker, text) : sink.Send(message, recipient, speaker, text, (nint)original);
    }

    [UnmanagedCallersOnly]
    private static int DMWhisper(void* message, uint recipient, uint speaker, void* text)
    {
        var sink = _sink;
        var original = Originals[7];
        return sink is null ? original(message, recipient, speaker, text) : sink.Send(message, recipient, speaker, text, (nint)original);
    }

    [UnmanagedCallersOnly]
    private static int DMSilentShout(void* message, uint recipient, uint speaker, void* text)
    {
        var sink = _sink;
        var original = Originals[8];
        return sink is null ? original(message, recipient, speaker, text) : sink.Send(message, recipient, speaker, text, (nint)original);
    }
}
