using System.Collections.Generic;
using System.Threading.Tasks;
using SWLOR.Game.Server.Service.EngineTestService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.EngineTestDefinition.AbilityBehaviors
{
    public class CombatAnalyzerAbilityBehaviors : IAbilityBehaviorSource
    {
        [EngineTest("Combat Analyzer ability behaviors", Category = "AbilityBehavior", TimeoutSeconds = 1800f)]
        public static async Task Run(EngineTestContext ctx)
        {
            await AbilityBehaviorExecutor.RunAsync(ctx, new CombatAnalyzerAbilityBehaviors().BuildCases());
        }

        public List<AbilityBehaviorCase> BuildCases()
        {
            return new List<AbilityBehaviorCase>
            {
                // OverclockedAnalyzerAbilityDefinition - self buff, unconditional status effect.
                new()
                {
                    Feat = FeatType.Overload,
                    Target = AbilityTargetKind.Self,
                    ExpectedActivatorStatusEffects = new[] { typeof(OverloadStatusEffect) },
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
            };
        }
    }
}
