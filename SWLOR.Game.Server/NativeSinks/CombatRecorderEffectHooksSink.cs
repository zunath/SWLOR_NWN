using NWN.Native.API;
using SWLOR.Game.Contracts;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Native;
using ObjectType = NWN.Native.API.ObjectType;
using static SWLOR.Game.Server.Composition.GameServices;

namespace SWLOR.Game.Server.NativeSinks;

/// <summary>
/// Observes every damage and heal effect the engine applies, so the combat recorder sees exact HP and temporary-HP
/// changes from all sources. <see cref="CombatRecorderEffectHooks"/> installs the hooks the first time a recording
/// starts; while no recording runs each body only checks <see cref="ICombatRecorderService.IsRecording"/> and calls the
/// original function.
/// </summary>
public sealed unsafe class CombatRecorderEffectHooksSink : ICombatRecorderEffectHooksSink
{
    public int ApplyDamage(void* effectListHandler, void* pObject, void* pEffect, int bLoadingGame, nint original)
    {
        var applyOriginal = (delegate* unmanaged<void*, void*, void*, int, int>)original;
        if (!CombatRecorder.IsRecording || bLoadingGame != 0)
            return applyOriginal(effectListHandler, pObject, pEffect, bLoadingGame);

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
                poolsBefore = CombatRecorderEffectHooks.CaptureTemporaryHitPointPools(target);
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
            LogFailure("OnApplyDamage", ex);
        }

        var result = applyOriginal(effectListHandler, pObject, pEffect, bLoadingGame);
        if (target == null)
            return result;

        try
        {
            var targetId = target.m_idSelf;
            var hitPointsAfter = target.GetCurrentHitPoints(1);
            var poolsAfter = CombatRecorderEffectHooks.CaptureTemporaryHitPointPools(target);
            ServerManager.Executor.ExecuteInScriptContext(() =>
                CombatRecorder.RecordDamageApplied(
                    applicationIndex, isEngineApplied, creator, targetId, damage, hitPointsBefore, hitPointsAfter,
                    maxHitPoints, poolsBefore, poolsAfter));
        }
        catch (Exception ex)
        {
            LogFailure("OnApplyDamage", ex);
        }
        finally
        {
            CombatRecorder.EndDamageApplication(applicationIndex);
        }

        return result;
    }

    public int ApplyHeal(void* effectListHandler, void* pObject, void* pEffect, int bLoadingGame, nint original)
    {
        var applyOriginal = (delegate* unmanaged<void*, void*, void*, int, int>)original;
        if (!CombatRecorder.IsRecording || bLoadingGame != 0)
            return applyOriginal(effectListHandler, pObject, pEffect, bLoadingGame);

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
            LogFailure("OnApplyHeal", ex);
        }

        // The PvP healing reduction runs inside this call and writes its cut into this heal's frame.
        var targetId = target?.m_idSelf ?? OBJECT_INVALID;
        if (target != null)
            CombatRecorder.BeginHealApplication(targetId);
        int result;
        var pvpReduced = 0;
        try
        {
            result = applyOriginal(effectListHandler, pObject, pEffect, bLoadingGame);
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
            LogFailure("OnApplyHeal", ex);
        }

        return result;
    }

    private static void LogFailure(string hook, Exception ex)
    {
        Log.Write(LogGroup.Error, $"Combat recorder {hook} hook failed: {ex}", true);
    }
}
