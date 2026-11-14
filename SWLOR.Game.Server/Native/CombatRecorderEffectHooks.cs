using System.Collections.Generic;
using NWN.Native.API;
using SWLOR.Game.Contracts;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Feature.AbilityDefinition;

namespace SWLOR.Game.Server.Native
{
    /// <summary>
    /// Observes every damage and heal effect the engine applies, so the combat recorder sees
    /// exact HP and temporary-HP changes from all sources. Installed the first time a recording
    /// starts; while no recording runs each hook only checks <see cref="Domain.Combat.ICombatRecorderService.IsRecording"/>
    /// and calls the original function.
    /// </summary>
    public static class CombatRecorderEffectHooks
    {
        private const int EngineTemporaryHitPointsEffectType = 15;

        /// <summary>
        /// Enables the two hooks (<c>SWLOR.Game.NativeHooks.CombatRecorderEffectHooksTrampolines</c>), each once for the
        /// process. A hook that installed stays installed even if the other one fails, so a later retry never hooks the
        /// same function twice.
        /// </summary>
        public static void Install()
        {
            NativeHookAccess.Hooks.Enable(NativeHookId.CombatRecorderEffectHooks);
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
