using System.Runtime.InteropServices;
using NWNX.NET.Native;
using SWLOR.Game.Contracts;

namespace SWLOR.Game.NativeHooks;

/// <summary>
/// The hooks of the game's <c>Native/PlayerFileCheckpoint</c>: <c>CNWSCreature::SaveCreature</c>, <c>LoadCreature</c>
/// and <c>CNWSCreatureStats::ReadStatsFromGff</c>, installed in that order; the save hook marks the file installed.
/// </summary>
internal static unsafe class PlayerFileCheckpointTrampolines
{
    internal const string SaveSymbol = "_ZN12CNWSCreature12SaveCreatureEP7CResGFFP10CResStructiiii";
    internal const string LoadSymbol = "_ZN12CNWSCreature12LoadCreatureEP7CResGFFP10CResStructiiii";
    internal const string ReadStatsSymbol = "_ZN17CNWSCreatureStats16ReadStatsFromGffEP7CResGFFP10CResStructP26CNWSCreatureAppearanceInfoiiii";

    private static bool _resolved;
    private static nint _saveFunction;
    private static nint _loadFunction;
    private static nint _readStatsFunction;
    private static FunctionHook* _saveHook;
    private static FunctionHook* _loadHook;
    private static FunctionHook* _readStatsHook;
    private static volatile IPlayerFileCheckpointSink? _sink;

    internal static bool IsEnabled => _saveHook != null;

    internal static void Resolve(INativeFunctions native)
    {
        if (_resolved)
            return;

        _saveFunction = native.GetExport(SaveSymbol);
        _loadFunction = native.GetExport(LoadSymbol);
        _readStatsFunction = native.GetExport(ReadStatsSymbol);
        _resolved = true;
    }

    internal static void Enable(INativeFunctions native)
    {
        if (_saveHook != null)
            return;

        Resolve(native);
        delegate* unmanaged<void*, void*, void*, int, int, int, int, int> save = &Save;
        delegate* unmanaged<void*, void*, void*, int, int, int, int, int> load = &Load;
        delegate* unmanaged<void*, void*, void*, void*, int, int, int, int, uint> readStats = &ReadStats;
        _saveHook = native.RequestHook(_saveFunction, (nint)save, HookOrder.Early);
        _loadHook = native.RequestHook(_loadFunction, (nint)load, HookOrder.Early);
        _readStatsHook = native.RequestHook(_readStatsFunction, (nint)readStats, HookOrder.Early);
    }

    internal static IPlayerFileCheckpointSink? Sink => _sink;

    internal static void Bind(IPlayerFileCheckpointSink sink) => _sink = sink;

    internal static void Unbind(IPlayerFileCheckpointSink sink)
    {
        if (ReferenceEquals(_sink, sink))
            _sink = null;
    }

    [UnmanagedCallersOnly]
    private static int Save(void* creature, void* resource, void* structure,
        int associates, int desiredArea, int exporting, int objectIds)
    {
        var sink = _sink;
        return sink is null
            ? ((delegate* unmanaged<void*, void*, void*, int, int, int, int, int>)_saveHook->m_trampoline)(creature, resource, structure,
                associates, desiredArea, exporting, objectIds)
            : sink.Save(creature, resource, structure, associates, desiredArea, exporting, objectIds, (nint)_saveHook->m_trampoline);
    }

    [UnmanagedCallersOnly]
    private static int Load(void* creature, void* resource, void* structure,
        int saveGame, int associate, int preserveIds, int copyObject)
    {
        var sink = _sink;
        return sink is null
            ? ((delegate* unmanaged<void*, void*, void*, int, int, int, int, int>)_loadHook->m_trampoline)(creature, resource, structure,
                saveGame, associate, preserveIds, copyObject)
            : sink.Load(creature, resource, structure, saveGame, associate, preserveIds, copyObject, (nint)_loadHook->m_trampoline);
    }

    [UnmanagedCallersOnly]
    private static uint ReadStats(void* stats, void* resource, void* structure, void* appearance,
        int saveGame, int playerCharacter, int copyObject, int loadObjectId)
    {
        var sink = _sink;
        return sink is null
            ? ((delegate* unmanaged<void*, void*, void*, void*, int, int, int, int, uint>)_readStatsHook->m_trampoline)(stats, resource,
                structure, appearance, saveGame, playerCharacter, copyObject, loadObjectId)
            : sink.ReadStats(stats, resource, structure, appearance, saveGame, playerCharacter, copyObject, loadObjectId,
                (nint)_readStatsHook->m_trampoline);
    }
}
