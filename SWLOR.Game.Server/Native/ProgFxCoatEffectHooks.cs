using System.Runtime.InteropServices;
using NWN.Native.API;
using NWNX.NET;
using NWNX.NET.Native;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.LogService;
using ObjectType = NWN.Native.API.ObjectType;

namespace SWLOR.Game.Server.Native
{
    /// <summary>
    /// Tells <see cref="ProgFxCoatRenderer"/> when a coating visual effect or petrification is applied
    /// to or removed from a creature. NWNX's effect events skip visual effects, and a DM's
    /// ##dm_visualeffect applies one outside any script, so the engine's own handlers are observed.
    /// </summary>
    public static unsafe class ProgFxCoatEffectHooks
    {
        private const string ApplyVisualEffectSymbol =
            "_ZN21CNWSEffectListHandler19OnApplyVisualEffectEP10CNWSObjectP11CGameEffecti";
        private const string RemoveVisualEffectSymbol =
            "_ZN21CNWSEffectListHandler20OnRemoveVisualEffectEP10CNWSObjectP11CGameEffect";
        private const string ApplyPetrifySymbol =
            "_ZN21CNWSEffectListHandler14OnApplyPetrifyEP10CNWSObjectP11CGameEffecti";
        private const string RemovePetrifySymbol =
            "_ZN21CNWSEffectListHandler15OnRemovePetrifyEP10CNWSObjectP11CGameEffect";

        private static delegate* unmanaged<void*, void*, void*, int, int> _originalApplyVisualEffect;
        private static delegate* unmanaged<void*, void*, void*, int> _originalRemoveVisualEffect;
        private static delegate* unmanaged<void*, void*, void*, int, int> _originalApplyPetrify;
        private static delegate* unmanaged<void*, void*, void*, int> _originalRemovePetrify;

        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void RegisterHooks()
        {
            // Earliest: these hooks only observe, after every other hook has run.
            if (_originalApplyVisualEffect == null)
            {
                delegate* unmanaged<void*, void*, void*, int, int> hook = &OnApplyVisualEffect;
                _originalApplyVisualEffect = (delegate* unmanaged<void*, void*, void*, int, int>)RequestHook(ApplyVisualEffectSymbol, (IntPtr)hook);
            }

            if (_originalRemoveVisualEffect == null)
            {
                delegate* unmanaged<void*, void*, void*, int> hook = &OnRemoveVisualEffect;
                _originalRemoveVisualEffect = (delegate* unmanaged<void*, void*, void*, int>)RequestHook(RemoveVisualEffectSymbol, (IntPtr)hook);
            }

            if (_originalApplyPetrify == null)
            {
                delegate* unmanaged<void*, void*, void*, int, int> hook = &OnApplyPetrify;
                _originalApplyPetrify = (delegate* unmanaged<void*, void*, void*, int, int>)RequestHook(ApplyPetrifySymbol, (IntPtr)hook);
            }

            if (_originalRemovePetrify == null)
            {
                delegate* unmanaged<void*, void*, void*, int> hook = &OnRemovePetrify;
                _originalRemovePetrify = (delegate* unmanaged<void*, void*, void*, int>)RequestHook(RemovePetrifySymbol, (IntPtr)hook);
            }
        }

        private static void* RequestHook(string symbol, IntPtr hook)
        {
            var function = NativeLibrary.GetExport(NativeLibrary.GetMainProgramHandle(), symbol);
            var functionHook = NWNXAPI.RequestFunctionHook(function, hook, HookOrder.Earliest);
            return functionHook->m_trampoline;
        }

        [UnmanagedCallersOnly]
        private static int OnApplyVisualEffect(void* effectListHandler, void* pObject, void* pEffect, int bLoadingGame)
        {
            var result = _originalApplyVisualEffect(effectListHandler, pObject, pEffect, bLoadingGame);
            QueueVisualEffect(pObject, pEffect);
            return result;
        }

        [UnmanagedCallersOnly]
        private static int OnRemoveVisualEffect(void* effectListHandler, void* pObject, void* pEffect)
        {
            var result = _originalRemoveVisualEffect(effectListHandler, pObject, pEffect);
            QueueVisualEffect(pObject, pEffect);
            return result;
        }

        [UnmanagedCallersOnly]
        private static int OnApplyPetrify(void* effectListHandler, void* pObject, void* pEffect, int bLoadingGame)
        {
            var result = _originalApplyPetrify(effectListHandler, pObject, pEffect, bLoadingGame);
            QueueCreature(pObject);
            return result;
        }

        [UnmanagedCallersOnly]
        private static int OnRemovePetrify(void* effectListHandler, void* pObject, void* pEffect)
        {
            var result = _originalRemovePetrify(effectListHandler, pObject, pEffect);
            QueueCreature(pObject);
            return result;
        }

        private static void QueueVisualEffect(void* pObject, void* pEffect)
        {
            try
            {
                // Integer 0 is the visualeffects.2da row. Most effects never coat; skip them cheaply.
                if (ProgFxCoatRenderer.IsCoatingVisualEffect(CGameEffect.FromPointer(pEffect).GetInteger(0)))
                    QueueCreature(pObject);
            }
            catch (Exception ex)
            {
                LogFailure(ex);
            }
        }

        private static void QueueCreature(void* pObject)
        {
            try
            {
                var target = CNWSObject.FromPointer(pObject);
                if (target == null || target.m_nObjectType != (int)ObjectType.Creature)
                    return;

                var creature = target.m_idSelf;
                ServerManager.Executor.ExecuteInScriptContext(() => ProgFxCoatRenderer.QueueRefresh(creature), creature);
            }
            catch (Exception ex)
            {
                LogFailure(ex);
            }
        }

        private static void LogFailure(Exception ex)
        {
            Log.Write(LogGroup.Error, $"ProgFX coat effect hook failed: {ex}", true);
        }
    }
}
