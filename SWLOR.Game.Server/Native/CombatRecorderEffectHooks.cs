using System.Collections.Generic;
using System.Runtime.InteropServices;
using NWN.Native.API;
using NWNX.NET;
using NWNX.NET.Native;
using SWLOR.Game.Contracts;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.NativeSinks;

namespace SWLOR.Game.Server.Native
{
    /// <summary>
    /// Observes every damage and heal effect the engine applies, so the combat recorder sees
    /// exact HP and temporary-HP changes from all sources. Installed the first time a recording
    /// starts; while no recording runs each hook only checks <see cref="Domain.Combat.ICombatRecorderService.IsRecording"/>
    /// and calls the original function.
    /// </summary>
    public static unsafe class CombatRecorderEffectHooks
    {
        private const string ApplyDamageSymbol = "_ZN21CNWSEffectListHandler13OnApplyDamageEP10CNWSObjectP11CGameEffecti";
        private const string ApplyHealSymbol = "_ZN21CNWSEffectListHandler11OnApplyHealEP10CNWSObjectP11CGameEffecti";
        private const int EngineTemporaryHitPointsEffectType = 15;

        private static delegate* unmanaged<void*, void*, void*, int, int> _originalApplyDamage;
        private static delegate* unmanaged<void*, void*, void*, int, int> _originalApplyHeal;
        private static ICombatRecorderEffectHooksSink _sink;

        /// <summary>
        /// Installs each hook once. A hook that installed stays installed even if the other one
        /// fails, so a later retry never hooks the same function twice.
        /// </summary>
        public static void Install()
        {
            // Earliest: these hooks only observe, and running outermost lets the before/after
            // reads include every other hook's adjustments. The original is reached through the
            // hook's trampoline, not the returned FunctionHook pointer.
            _sink ??= new CombatRecorderEffectHooksSink();
            if (_originalApplyDamage == null)
            {
                delegate* unmanaged<void*, void*, void*, int, int> damageHook = &OnApplyDamage;
                _originalApplyDamage = RequestHook(ApplyDamageSymbol, damageHook);
            }

            if (_originalApplyHeal == null)
            {
                delegate* unmanaged<void*, void*, void*, int, int> healHook = &OnApplyHeal;
                _originalApplyHeal = RequestHook(ApplyHealSymbol, healHook);
            }
        }

        private static delegate* unmanaged<void*, void*, void*, int, int> RequestHook(
            string symbol, delegate* unmanaged<void*, void*, void*, int, int> hook)
        {
            var function = NativeLibrary.GetExport(NativeLibrary.GetMainProgramHandle(), symbol);
            var functionHook = NWNXAPI.RequestFunctionHook(function, (IntPtr)hook, HookOrder.Earliest);
            return (delegate* unmanaged<void*, void*, void*, int, int>)functionHook->m_trampoline;
        }

        [UnmanagedCallersOnly]
        private static int OnApplyDamage(void* effectListHandler, void* pObject, void* pEffect, int bLoadingGame)
            => _sink.ApplyDamage(effectListHandler, pObject, pEffect, bLoadingGame, (nint)_originalApplyDamage);

        [UnmanagedCallersOnly]
        private static int OnApplyHeal(void* effectListHandler, void* pObject, void* pEffect, int bLoadingGame)
            => _sink.ApplyHeal(effectListHandler, pObject, pEffect, bLoadingGame, (nint)_originalApplyHeal);

        /// <summary>
        /// A creature's current HP without temporary HP, read the same way as the damage hook.
        /// </summary>
        internal static int GetHitPointsExcludingTemporary(uint creature)
        {
            var gameObject = NWNXUtils.GetGameObject(creature);
            return gameObject == IntPtr.Zero ? 0 : CNWSObject.FromPointer(gameObject).GetCurrentHitPoints(1);
        }

        /// <summary>
        /// A creature's temporary-HP pools, read the same way as the damage hook.
        /// </summary>
        internal static CombatRecordTemporaryHitPointPool[] CaptureTemporaryHitPointPools(uint creature)
        {
            var gameObject = NWNXUtils.GetGameObject(creature);
            return gameObject == IntPtr.Zero ? null : CaptureTemporaryHitPointPools(CNWSObject.FromPointer(gameObject));
        }

        /// <summary>
        /// Reads each temporary-HP effect's remaining pool straight from the native effect list.
        /// Script effect iteration is avoided because it would reset an iterator a running script
        /// may be using on the same creature.
        /// </summary>
        internal static CombatRecordTemporaryHitPointPool[] CaptureTemporaryHitPointPools(CNWSObject target)
        {
            List<CombatRecordTemporaryHitPointPool> pools = null;
            foreach (var effect in target.m_appliedEffects)
            {
                if (effect.m_nType != EngineTemporaryHitPointsEffectType)
                    continue;

                pools ??= new List<CombatRecordTemporaryHitPointPool>();
                pools.Add(new CombatRecordTemporaryHitPointPool
                {
                    EffectId = effect.m_nID,
                    EffectKey = TemporaryHitPointEffects.GetEffectKey(effect.m_sCustomTag.ToString()),
                    Amount = Math.Max(0, effect.GetInteger(0))
                });
            }

            return pools?.ToArray();
        }
    }
}
