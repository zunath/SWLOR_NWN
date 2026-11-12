using System.Runtime.InteropServices;
using NWNX.NET.Native;
using SWLOR.Game.Contracts;

namespace SWLOR.Game.NativeHooks;

/// <summary>
/// The hooks of the game's <c>Native/WeaponAttackAnimation</c>: <c>CNWSMessage::ComputeUpdateRequired</c> and
/// <c>WriteGameObjUpdate_UpdateObject</c>, installed in that order; the first marks the file installed. Both run per
/// object per frame.
/// </summary>
internal static unsafe class WeaponAttackAnimationTrampolines
{
    internal const string ComputeSymbol = "_ZN11CNWSMessage21ComputeUpdateRequiredEP10CNWSPlayerP10CNWSObjectP17CLastUpdateObjecti";
    internal const string WriteSymbol = "_ZN11CNWSMessage31WriteGameObjUpdate_UpdateObjectEP10CNWSPlayerP10CNWSObjectP17CLastUpdateObjectjj";

    private static bool _resolved;
    private static nint _computeFunction;
    private static nint _writeFunction;
    private static FunctionHook* _computeHook;
    private static FunctionHook* _writeHook;
    private static volatile IWeaponAttackAnimationSink? _sink;

    internal static bool IsEnabled => _computeHook != null;

    internal static void Resolve(INativeFunctions native)
    {
        if (_resolved)
            return;

        _computeFunction = native.GetExport(ComputeSymbol);
        _writeFunction = native.GetExport(WriteSymbol);
        _resolved = true;
    }

    internal static void Enable(INativeFunctions native)
    {
        if (_computeHook != null)
            return;

        Resolve(native);
        delegate* unmanaged<void*, void*, void*, void*, int, uint> compute = &ComputeUpdate;
        delegate* unmanaged<void*, void*, void*, void*, uint, uint, void> write = &WriteUpdate;
        _computeHook = native.RequestHook(_computeFunction, (nint)compute, HookOrder.Late);
        _writeHook = native.RequestHook(_writeFunction, (nint)write, HookOrder.Late);
    }

    internal static IWeaponAttackAnimationSink? Sink => _sink;

    internal static void Bind(IWeaponAttackAnimationSink sink) => _sink = sink;

    internal static void Unbind(IWeaponAttackAnimationSink sink)
    {
        if (ReferenceEquals(_sink, sink))
            _sink = null;
    }

    [UnmanagedCallersOnly]
    private static uint ComputeUpdate(void* message, void* player, void* obj, void* last, int playerObject)
    {
        var sink = _sink;
        return sink is null
            ? ((delegate* unmanaged<void*, void*, void*, void*, int, uint>)_computeHook->m_trampoline)(message, player, obj, last, playerObject)
            : sink.ComputeUpdate(message, player, obj, last, playerObject, (nint)_computeHook->m_trampoline);
    }

    [UnmanagedCallersOnly]
    private static void WriteUpdate(void* message, void* player, void* obj, void* last, uint updates, uint appearance)
    {
        var sink = _sink;
        if (sink is null)
            ((delegate* unmanaged<void*, void*, void*, void*, uint, uint, void>)_writeHook->m_trampoline)(message, player, obj, last, updates, appearance);
        else
            sink.WriteUpdate(message, player, obj, last, updates, appearance, (nint)_writeHook->m_trampoline);
    }
}
