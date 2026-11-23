using System.Runtime.InteropServices;
using NWNX.NET.Native;
using SWLOR.Game.Contracts;

namespace SWLOR.Game.NativeHooks;

/// <summary>
/// The hooks of the game's <c>Native/ProgFxCoatEffectHooks</c>: the engine's handlers for applying and removing a visual
/// effect and petrification. Each hook is installed on its own, so a hook that installed stays installed even if another
/// fails and a later retry never hooks the same function twice; each keeps the trampoline NWNX gave it when it was installed.
/// </summary>
internal static unsafe class ProgFxCoatEffectHooksTrampolines
{
    internal const string ApplyVisualEffectSymbol =
        "_ZN21CNWSEffectListHandler19OnApplyVisualEffectEP10CNWSObjectP11CGameEffecti";
    internal const string RemoveVisualEffectSymbol =
        "_ZN21CNWSEffectListHandler20OnRemoveVisualEffectEP10CNWSObjectP11CGameEffect";
    internal const string ApplyPetrifySymbol =
        "_ZN21CNWSEffectListHandler14OnApplyPetrifyEP10CNWSObjectP11CGameEffecti";
    internal const string RemovePetrifySymbol =
        "_ZN21CNWSEffectListHandler15OnRemovePetrifyEP10CNWSObjectP11CGameEffect";

    private static nint _applyVisualEffectFunction;
    private static nint _removeVisualEffectFunction;
    private static nint _applyPetrifyFunction;
    private static nint _removePetrifyFunction;
    private static delegate* unmanaged<void*, void*, void*, int, int> _originalApplyVisualEffect;
    private static delegate* unmanaged<void*, void*, void*, int> _originalRemoveVisualEffect;
    private static delegate* unmanaged<void*, void*, void*, int, int> _originalApplyPetrify;
    private static delegate* unmanaged<void*, void*, void*, int> _originalRemovePetrify;
    private static volatile IProgFxCoatEffectHooksSink? _sink;

    internal static bool IsEnabled =>
        _originalApplyVisualEffect != null && _originalRemoveVisualEffect != null &&
        _originalApplyPetrify != null && _originalRemovePetrify != null;

    internal static void Resolve(INativeFunctions native)
    {
        if (_applyVisualEffectFunction == 0)
            _applyVisualEffectFunction = native.GetExport(ApplyVisualEffectSymbol);
        if (_removeVisualEffectFunction == 0)
            _removeVisualEffectFunction = native.GetExport(RemoveVisualEffectSymbol);
        if (_applyPetrifyFunction == 0)
            _applyPetrifyFunction = native.GetExport(ApplyPetrifySymbol);
        if (_removePetrifyFunction == 0)
            _removePetrifyFunction = native.GetExport(RemovePetrifySymbol);
    }

    internal static void Enable(INativeFunctions native)
    {
        // Earliest: these hooks only observe, after every other hook has run. The original is reached through the
        // hook's trampoline, not the returned FunctionHook pointer.
        Resolve(native);
        if (_originalApplyVisualEffect == null)
        {
            delegate* unmanaged<void*, void*, void*, int, int> hook = &OnApplyVisualEffect;
            _originalApplyVisualEffect = (delegate* unmanaged<void*, void*, void*, int, int>)RequestHook(native, _applyVisualEffectFunction, (nint)hook);
        }

        if (_originalRemoveVisualEffect == null)
        {
            delegate* unmanaged<void*, void*, void*, int> hook = &OnRemoveVisualEffect;
            _originalRemoveVisualEffect = (delegate* unmanaged<void*, void*, void*, int>)RequestHook(native, _removeVisualEffectFunction, (nint)hook);
        }

        if (_originalApplyPetrify == null)
        {
            delegate* unmanaged<void*, void*, void*, int, int> hook = &OnApplyPetrify;
            _originalApplyPetrify = (delegate* unmanaged<void*, void*, void*, int, int>)RequestHook(native, _applyPetrifyFunction, (nint)hook);
        }

        if (_originalRemovePetrify == null)
        {
            delegate* unmanaged<void*, void*, void*, int> hook = &OnRemovePetrify;
            _originalRemovePetrify = (delegate* unmanaged<void*, void*, void*, int>)RequestHook(native, _removePetrifyFunction, (nint)hook);
        }
    }

    private static void* RequestHook(INativeFunctions native, nint function, nint hook) =>
        native.RequestHook(function, hook, HookOrder.Earliest)->m_trampoline;

    internal static IProgFxCoatEffectHooksSink? Sink => _sink;

    internal static void Bind(IProgFxCoatEffectHooksSink sink) => _sink = sink;

    internal static void Unbind(IProgFxCoatEffectHooksSink sink)
    {
        if (ReferenceEquals(_sink, sink))
            _sink = null;
    }

    [UnmanagedCallersOnly]
    private static int OnApplyVisualEffect(void* effectListHandler, void* pObject, void* pEffect, int bLoadingGame)
    {
        var sink = _sink;
        var original = _originalApplyVisualEffect;
        return sink is null
            ? original(effectListHandler, pObject, pEffect, bLoadingGame)
            : sink.ApplyVisualEffect(effectListHandler, pObject, pEffect, bLoadingGame, (nint)original);
    }

    [UnmanagedCallersOnly]
    private static int OnRemoveVisualEffect(void* effectListHandler, void* pObject, void* pEffect)
    {
        var sink = _sink;
        var original = _originalRemoveVisualEffect;
        return sink is null
            ? original(effectListHandler, pObject, pEffect)
            : sink.RemoveVisualEffect(effectListHandler, pObject, pEffect, (nint)original);
    }

    [UnmanagedCallersOnly]
    private static int OnApplyPetrify(void* effectListHandler, void* pObject, void* pEffect, int bLoadingGame)
    {
        var sink = _sink;
        var original = _originalApplyPetrify;
        return sink is null
            ? original(effectListHandler, pObject, pEffect, bLoadingGame)
            : sink.ApplyPetrify(effectListHandler, pObject, pEffect, bLoadingGame, (nint)original);
    }

    [UnmanagedCallersOnly]
    private static int OnRemovePetrify(void* effectListHandler, void* pObject, void* pEffect)
    {
        var sink = _sink;
        var original = _originalRemovePetrify;
        return sink is null
            ? original(effectListHandler, pObject, pEffect)
            : sink.RemovePetrify(effectListHandler, pObject, pEffect, (nint)original);
    }
}
