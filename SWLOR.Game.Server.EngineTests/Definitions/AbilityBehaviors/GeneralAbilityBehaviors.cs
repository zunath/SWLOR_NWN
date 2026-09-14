using System.Collections.Generic;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;

namespace SWLOR.Game.Server.EngineTests.Definitions.AbilityBehaviors
{
    /// <summary>
    /// Covers the top-level files directly in Feature/AbilityDefinition/ (AbilityAreaEffects,
    /// AbilityEffectScaling, AbilityTargeting, CapstoneAbility, CombatAreaPulses,
    /// DeviceAbilityEffects, LeadershipAbilityEffects, TemporaryHitPointEffects,
    /// WeaponActiveAbilityDefinitionBase). None of them implement IAbilityListDefinition or
    /// register a FeatType - they are shared static helpers/base classes consumed by the
    /// per-tree ability definitions, which cover their behavior indirectly. This source
    /// intentionally declares zero cases.
    /// </summary>
    public class GeneralAbilityBehaviors : IAbilityBehaviorSource
    {
        [EngineTest("General ability behaviors", Category = "AbilityBehavior", TimeoutSeconds = 60f)]
        public static async Task Run(EngineTestContext ctx)
        {
            await AbilityBehaviorExecutor.RunAsync(ctx, new GeneralAbilityBehaviors().BuildCases());
        }

        public List<AbilityBehaviorCase> BuildCases()
        {
            return new List<AbilityBehaviorCase>();
        }
    }
}
