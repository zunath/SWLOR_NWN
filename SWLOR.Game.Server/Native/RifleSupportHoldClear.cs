using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using NWN.Native.API;
using NWNX.NET;
using NWNX.NET.Native;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Native;

/// <summary>Clears a stale client rifle layer without issuing a server gameplay action.</summary>
internal static unsafe class RifleSupportHoldClear
{
    private const uint AnimationUpdate = 4;
    private const uint ReplacementUpdate = 0x1000000;
    // Native FakeDodgeSide, not NWScript's differently numbered animation constant.
    private const int DodgeSide = 90;
    private static FunctionHook* _computeHook;
    private static FunctionHook* _writeHook;
    private static readonly RifleSupportHoldProjection Projection = new();

    internal static void Begin(uint creature, string physicalIdle) =>
        Projection.Begin(creature, physicalIdle, Environment.TickCount64);

    [NWNEventHandler(ScriptName.OnModuleLoad)]
    public static void RegisterHooks()
    {
        if (_computeHook != null) return;
        var library = NativeLibrary.GetMainProgramHandle();
        delegate* unmanaged<void*, void*, void*, void*, int, uint> compute = &ComputeUpdate;
        delegate* unmanaged<void*, void*, void*, void*, uint, uint, void> write = &WriteUpdate;
        _computeHook = NWNXAPI.RequestFunctionHook(NativeLibrary.GetExport(library,
            "_ZN11CNWSMessage21ComputeUpdateRequiredEP10CNWSPlayerP10CNWSObjectP17CLastUpdateObjecti"),
            (IntPtr)compute, HookOrder.Late);
        _writeHook = NWNXAPI.RequestFunctionHook(NativeLibrary.GetExport(library,
            "_ZN11CNWSMessage31WriteGameObjUpdate_UpdateObjectEP10CNWSPlayerP10CNWSObjectP17CLastUpdateObjectjj"),
            (IntPtr)write, HookOrder.Late);
    }

    [UnmanagedCallersOnly]
    private static uint ComputeUpdate(void* message, void* player, void* obj, void* last, int playerObject)
    {
        var original = (delegate* unmanaged<void*, void*, void*, void*, int, uint>)_computeHook->m_trampoline;
        var updates = original(message, player, obj, last, playerObject);
        try
        {
            var native = CNWSObject.FromPointer(obj);
            var actor = native.m_idSelf;
            Projection.ObserveAnimation(actor, native.m_nAnimation);
            var stage = Projection.Current(actor, CNWSPlayer.FromPointer(player).m_nPlayerID, Environment.TickCount64);
            if (stage is RifleSupportHoldStage.Clear or RifleSupportHoldStage.Restore)
                updates |= AnimationUpdate | ReplacementUpdate;
        }
        catch (Exception exception) { Log.WriteError(exception, "Could not prepare the rifle hold refresh"); }
        return updates;
    }

    [UnmanagedCallersOnly]
    private static void WriteUpdate(void* message, void* player, void* obj, void* last, uint updates, uint appearance)
    {
        var original = (delegate* unmanaged<void*, void*, void*, void*, uint, uint, void>)_writeHook->m_trampoline;
        var wrote = false;
        try
        {
            var native = CNWSObject.FromPointer(obj);
            var actor = native.m_idSelf;
            var observer = CNWSPlayer.FromPointer(player).m_nPlayerID;
            var now = Environment.TickCount64;
            Projection.ObserveAnimation(actor, native.m_nAnimation);
            var stage = Projection.Current(actor, observer, now);
            Dictionary<string, string> mappings = null;
            if (stage != RifleSupportHoldStage.None)
            {
                mappings = WeaponAttackAnimation.ReadAnimationReplacements(native);
                // A temporary ability, another mapping owner, or unequip takes precedence.
                // Native network updates can run outside a script; token reads use NWScript.
                var mappedPlaybackActive = ServerManager.Executor.ExecuteInScriptContext(
                    () => NamedAnimation.IsMappedPlaybackActive(actor), actor);
                if (mappedPlaybackActive ||
                    !mappings.GetValueOrDefault("xbowr", "").StartsWith("rf", StringComparison.Ordinal) ||
                    !CarrierAvailable(mappings, NamedAnimationPlayback.HoldCarrierSource, NamedAnimationPlayback.NoHoldName) ||
                    !CarrierAvailable(mappings, NamedAnimationPlayback.ShoulderHoldSource, Projection.PhysicalIdle(actor)))
                {
                    Projection.Cancel(actor);
                    stage = Projection.Current(actor, observer, now);
                }
            }
            if (stage == RifleSupportHoldStage.Clear)
            {
                var animation = native.m_nAnimation;
                var speed = native.m_fAnimSpeed;
                try
                {
                    Replace(native, NamedAnimationPlayback.ShoulderHoldSource, Projection.PhysicalIdle(actor));
                    Replace(native, NamedAnimationPlayback.HoldCarrierSource, NamedAnimationPlayback.NoHoldName);
                    native.m_nAnimation = DodgeSide;
                    native.m_fAnimSpeed = 1f;
                    wrote = true;
                    original(message, player, obj, last, updates | AnimationUpdate | ReplacementUpdate, appearance);
                }
                finally
                {
                    native.m_nAnimation = animation;
                    native.m_fAnimSpeed = speed;
                    Replace(native, NamedAnimationPlayback.ShoulderHoldSource,
                        mappings.GetValueOrDefault(NamedAnimationPlayback.ShoulderHoldSource, ""));
                    Replace(native, NamedAnimationPlayback.HoldCarrierSource,
                        mappings.GetValueOrDefault(NamedAnimationPlayback.HoldCarrierSource, ""));
                }
                Projection.Sent(actor, observer, stage, now);
                return;
            }
            if (stage == RifleSupportHoldStage.Hold) updates &= ~AnimationUpdate;
            if (stage == RifleSupportHoldStage.Restore) updates |= AnimationUpdate | ReplacementUpdate;
            wrote = true;
            original(message, player, obj, last, updates, appearance);
            Projection.Sent(actor, observer, stage, now);
        }
        catch (Exception exception)
        {
            Log.WriteError(exception, "Could not write the rifle hold refresh");
            if (!wrote) original(message, player, obj, last, updates, appearance);
        }
    }

    private static bool CarrierAvailable(Dictionary<string, string> mappings, string source, string owned) =>
        string.IsNullOrEmpty(mappings.GetValueOrDefault(source, "")) || mappings[source] == owned;

    private static void Replace(CNWSObject obj, string source, string replacement)
    {
        using var oldName = new CExoString(source);
        using var newName = new CExoString(replacement);
        obj.SetAnimationReplace(oldName, newName);
    }

    [NWNEventHandler(ScriptName.OnModuleHeartbeat)]
    public static void Prune() => Projection.Prune(GetIsObjectValid, Environment.TickCount64);
}
