using System.Collections.Generic;
using System.Runtime.InteropServices;
using NWN.Native.API;
using NWNX.NET;
using NWNX.NET.Native;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatRecorderService;
using SWLOR.Game.Server.Service.LogService;
using ObjectType = NWN.Native.API.ObjectType;

namespace SWLOR.Game.Server.Native
{
    /// <summary>
    /// Observes every damage and heal effect the engine applies, so the combat recorder sees
    /// exact HP and temporary-HP changes from all sources. Installed the first time a recording
    /// starts; while no recording runs each hook only checks <see cref="CombatRecorder.IsRecording"/>
    /// and calls the original function.
    /// </summary>
    public static unsafe class CombatRecorderEffectHooks
    {
        private const string ApplyDamageSymbol = "_ZN21CNWSEffectListHandler13OnApplyDamageEP10CNWSObjectP11CGameEffecti";
        private const string ApplyHealSymbol = "_ZN21CNWSEffectListHandler11OnApplyHealEP10CNWSObjectP11CGameEffecti";
        private const int EngineTemporaryHitPointsEffectType = 15;

        private static delegate* unmanaged<void*, void*, void*, int, int> _originalApplyDamage;
        private static delegate* unmanaged<void*, void*, void*, int, int> _originalApplyHeal;
        private static bool _isInstalled;

        public static void Install()
        {
            if (_isInstalled)
                return;

            var program = NativeLibrary.GetMainProgramHandle();
            var applyDamage = NativeLibrary.GetExport(program, ApplyDamageSymbol);
            var applyHeal = NativeLibrary.GetExport(program, ApplyHealSymbol);

            // Earliest: these hooks only observe, and running outermost lets the before/after
            // reads include every other hook's adjustments. The original is reached through the
            // hook's trampoline, not the returned FunctionHook pointer.
            delegate* unmanaged<void*, void*, void*, int, int> damageHook = &OnApplyDamage;
            delegate* unmanaged<void*, void*, void*, int, int> healHook = &OnApplyHeal;
            var damageFunctionHook = NWNXAPI.RequestFunctionHook(applyDamage, (IntPtr)damageHook, HookOrder.Earliest);
            _originalApplyDamage = (delegate* unmanaged<void*, void*, void*, int, int>)damageFunctionHook->m_trampoline;
            var healFunctionHook = NWNXAPI.RequestFunctionHook(applyHeal, (IntPtr)healHook, HookOrder.Earliest);
            _originalApplyHeal = (delegate* unmanaged<void*, void*, void*, int, int>)healFunctionHook->m_trampoline;
            _isInstalled = true;
        }

        [UnmanagedCallersOnly]
        private static int OnApplyDamage(void* effectListHandler, void* pObject, void* pEffect, int bLoadingGame)
        {
            if (!CombatRecorder.IsRecording || bLoadingGame != 0)
                return _originalApplyDamage(effectListHandler, pObject, pEffect, bLoadingGame);

            CNWSObject target = null;
            uint creator = OBJECT_INVALID;
            var damage = new CombatRecordDamageBreakdown();
            var hitPointsBefore = 0;
            CombatRecordTemporaryHitPointPool[] poolsBefore = null;
            try
            {
                target = CNWSObject.FromPointer(pObject);
                if (target != null && target.m_nObjectType == (int)ObjectType.Creature)
                {
                    var effect = CGameEffect.FromPointer(pEffect);
                    creator = effect.m_oidCreator;
                    for (var index = 0; index < CombatRecordDamageBreakdown.NativeDamageTypeCount; index++)
                    {
                        damage.AddNative(index, effect.GetInteger(index));
                    }

                    hitPointsBefore = target.GetCurrentHitPoints(1);
                    poolsBefore = CaptureTemporaryHitPointPools(target);
                }
                else
                {
                    target = null;
                }
            }
            catch (Exception ex)
            {
                target = null;
                LogFailure(nameof(OnApplyDamage), ex);
            }

            var result = _originalApplyDamage(effectListHandler, pObject, pEffect, bLoadingGame);
            if (target == null)
                return result;

            try
            {
                var targetId = target.m_idSelf;
                var hitPointsAfter = target.GetCurrentHitPoints(1);
                var maxHitPoints = target.GetMaxHitPoints(1);
                var poolsAfter = CaptureTemporaryHitPointPools(target);
                ServerManager.Executor.ExecuteInScriptContext(() =>
                    CombatRecorder.RecordDamageApplied(
                        creator, targetId, damage, hitPointsBefore, hitPointsAfter, maxHitPoints, poolsBefore, poolsAfter));
            }
            catch (Exception ex)
            {
                LogFailure(nameof(OnApplyDamage), ex);
            }

            return result;
        }

        [UnmanagedCallersOnly]
        private static int OnApplyHeal(void* effectListHandler, void* pObject, void* pEffect, int bLoadingGame)
        {
            if (!CombatRecorder.IsRecording || bLoadingGame != 0)
                return _originalApplyHeal(effectListHandler, pObject, pEffect, bLoadingGame);

            CNWSObject target = null;
            uint creator = OBJECT_INVALID;
            var requested = 0;
            var hitPointsBefore = 0;
            try
            {
                target = CNWSObject.FromPointer(pObject);
                if (target != null && target.m_nObjectType == (int)ObjectType.Creature)
                {
                    var effect = CGameEffect.FromPointer(pEffect);
                    creator = effect.m_oidCreator;
                    requested = effect.GetInteger(0);
                    hitPointsBefore = target.GetCurrentHitPoints(1);
                }
                else
                {
                    target = null;
                }
            }
            catch (Exception ex)
            {
                target = null;
                LogFailure(nameof(OnApplyHeal), ex);
            }

            var result = _originalApplyHeal(effectListHandler, pObject, pEffect, bLoadingGame);
            if (target == null)
                return result;

            try
            {
                var targetId = target.m_idSelf;
                var hitPointsAfter = target.GetCurrentHitPoints(1);
                var maxHitPoints = target.GetMaxHitPoints(1);
                ServerManager.Executor.ExecuteInScriptContext(() =>
                    CombatRecorder.RecordHealingApplied(creator, targetId, requested, hitPointsBefore, hitPointsAfter, maxHitPoints));
            }
            catch (Exception ex)
            {
                LogFailure(nameof(OnApplyHeal), ex);
            }

            return result;
        }

        /// <summary>
        /// Reads each temporary-HP effect's remaining pool straight from the native effect list.
        /// Script effect iteration is avoided because it would reset an iterator a running script
        /// may be using on the same creature.
        /// </summary>
        private static CombatRecordTemporaryHitPointPool[] CaptureTemporaryHitPointPools(CNWSObject target)
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

        private static void LogFailure(string hook, Exception ex)
        {
            Log.Write(LogGroup.Error, $"Combat recorder {hook} hook failed: {ex}", true);
        }
    }
}
