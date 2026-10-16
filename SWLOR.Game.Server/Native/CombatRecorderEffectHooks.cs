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

        /// <summary>
        /// Installs each hook once. A hook that installed stays installed even if the other one
        /// fails, so a later retry never hooks the same function twice.
        /// </summary>
        public static void Install()
        {
            // Earliest: these hooks only observe, and running outermost lets the before/after
            // reads include every other hook's adjustments. The original is reached through the
            // hook's trampoline, not the returned FunctionHook pointer.
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
        {
            if (!CombatRecorder.IsRecording || bLoadingGame != 0)
                return _originalApplyDamage(effectListHandler, pObject, pEffect, bLoadingGame);

            CNWSObject target = null;
            uint creator = OBJECT_INVALID;
            var damage = new CombatRecordDamageBreakdown();
            var hitPointsBefore = 0;
            var maxHitPoints = 0;
            var applicationIndex = -1;
            // No managed entry point on the stack means the engine itself is applying the hit.
            var isEngineApplied = ManagedEntryDepth.Current == 0;
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
                    // Read before the engine call: a lethal hit runs the death scripts inside it,
                    // and clearing status effects there can lower the maximum.
                    maxHitPoints = target.GetMaxHitPoints(1);
                    poolsBefore = CaptureTemporaryHitPointPools(target);
                    // An engine-applied weapon hit has no script context, and the recorder calls NWScript.
                    var source = creator;
                    var targetId = target.m_idSelf;
                    var amount = damage.Total;
                    var hitPoints = hitPointsBefore;
                    applicationIndex = ServerManager.Executor.ExecuteInScriptContext(() =>
                        CombatRecorder.BeginDamageApplication(source, targetId, amount, hitPoints));
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
                var poolsAfter = CaptureTemporaryHitPointPools(target);
                ServerManager.Executor.ExecuteInScriptContext(() =>
                    CombatRecorder.RecordDamageApplied(
                        applicationIndex, isEngineApplied, creator, targetId, damage, hitPointsBefore, hitPointsAfter,
                        maxHitPoints, poolsBefore, poolsAfter));
            }
            catch (Exception ex)
            {
                LogFailure(nameof(OnApplyDamage), ex);
            }
            finally
            {
                CombatRecorder.EndDamageApplication(applicationIndex);
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

            // The PvP healing reduction runs inside this call and writes its cut into this heal's frame.
            var targetId = target?.m_idSelf ?? OBJECT_INVALID;
            if (target != null)
                CombatRecorder.BeginHealApplication(targetId);
            int result;
            var pvpReduced = 0;
            try
            {
                result = _originalApplyHeal(effectListHandler, pObject, pEffect, bLoadingGame);
            }
            finally
            {
                if (target != null)
                    pvpReduced = CombatRecorder.EndHealApplication(targetId);
            }

            if (target == null)
                return result;

            try
            {
                var hitPointsAfter = target.GetCurrentHitPoints(1);
                ServerManager.Executor.ExecuteInScriptContext(() =>
                    CombatRecorder.RecordHealingApplied(
                        creator, targetId, requested, hitPointsBefore, hitPointsAfter, GetMaxHitPoints(targetId), pvpReduced));
            }
            catch (Exception ex)
            {
                LogFailure(nameof(OnApplyHeal), ex);
            }

            return result;
        }

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
