using System.Collections.Generic;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions.AbilityBehaviors
{
    public sealed class UnarmedAbilityBehaviors : IAbilityBehaviorSource
    {
        [EngineTest("Unarmed ability behaviors", Category = "Unarmed", TimeoutSeconds = 1800f)]
        public static async Task Run(EngineTestContext context) =>
            await AbilityBehaviorExecutor.RunAsync(context, new UnarmedAbilityBehaviors().BuildCases());

        public List<AbilityBehaviorCase> BuildCases() => new()
        {
            new()
            {
                Feat = FeatType.Jab1,
                EquipMainHandResref = "t_pglove",
                Target = AbilityTargetKind.HostileCreature,
                ExpectsRecast = true,
                ExpectsTargetDamage = true,
                ExpectsSTMCost = true,
            },
            new()
            {
                Feat = FeatType.Jab2,
                EquipMainHandResref = "t_pglove",
                Target = AbilityTargetKind.HostileCreature,
                ExpectsRecast = true,
                ExpectsTargetDamage = true,
                ExpectsSTMCost = true,
            },
            new()
            {
                Feat = FeatType.Jab3,
                EquipMainHandResref = "t_pglove",
                Target = AbilityTargetKind.HostileCreature,
                ExpectsRecast = true,
                ExpectsTargetDamage = true,
                ExpectsSTMCost = true,
            },
            new()
            {
                Feat = FeatType.Jab4,
                EquipMainHandResref = "t_pglove",
                Target = AbilityTargetKind.HostileCreature,
                ExpectsRecast = true,
                ExpectsTargetDamage = true,
                ExpectsSTMCost = true,
            },
            new()
            {
                Feat = FeatType.PalmStrike1,
                EquipMainHandResref = "t_pglove",
                Target = AbilityTargetKind.HostileCreature,
                ExpectsRecast = true,
                ExpectsTargetDamage = true,
                ExpectsSTMCost = true,
            },
            new()
            {
                Feat = FeatType.PalmStrike2,
                EquipMainHandResref = "t_pglove",
                Target = AbilityTargetKind.HostileCreature,
                ExpectsRecast = true,
                ExpectsTargetDamage = true,
                ExpectsSTMCost = true,
            },
            new()
            {
                Feat = FeatType.PalmStrike3,
                EquipMainHandResref = "t_pglove",
                Target = AbilityTargetKind.HostileCreature,
                ExpectsRecast = true,
                ExpectsTargetDamage = true,
                ExpectsSTMCost = true,
            },
            new()
            {
                Feat = FeatType.FinishingCombination1,
                EquipMainHandResref = "t_pglove",
                Target = AbilityTargetKind.HostileCreature,
                ExpectsRecast = true,
                ExpectsTargetDamage = true,
                ExpectsSTMCost = true,
            },
            new()
            {
                Feat = FeatType.FinishingCombination2,
                EquipMainHandResref = "t_pglove",
                Target = AbilityTargetKind.HostileCreature,
                ExpectsRecast = true,
                ExpectsTargetDamage = true,
                ExpectsSTMCost = true,
            },
            new()
            {
                Feat = FeatType.FlowStance1,
                EquipMainHandResref = "t_pglove",
                Target = AbilityTargetKind.Self,
                ExpectsRecast = true,
                ExpectedActivatorStatusEffects = new[] { typeof(FlowStanceStatusEffect) },
            },
            new()
            {
                Feat = FeatType.PerfectForm1,
                EquipMainHandResref = "t_pglove",
                Target = AbilityTargetKind.HostileCreature,
                PrepareActivationAsync = EarnFinishAsync,
                ExpectsRecast = true,
                ExpectsTargetDamage = true,
                ExpectsSTMCost = true,
                ImpactRefundsCosts = true,
                Notes = "Requires Finish, earned first through real Jab and Palm Strike casts. Controlled Breathing restores 2 STM when Perfect Form spends Finish.",
            },
            new()
            {
                Feat = FeatType.DirtyJab1,
                EquipMainHandResref = "t_pglove",
                Target = AbilityTargetKind.HostileCreature,
                ExpectsRecast = true,
                ExpectsTargetDamage = true,
                ExpectsSTMCost = true,
                ExpectedTargetStatusEffects = new[] { typeof(RattledStatusEffect) },
            },
            new()
            {
                Feat = FeatType.DirtyJab2,
                EquipMainHandResref = "t_pglove",
                Target = AbilityTargetKind.HostileCreature,
                ExpectsRecast = true,
                ExpectsTargetDamage = true,
                ExpectsSTMCost = true,
                ExpectedTargetStatusEffects = new[] { typeof(RattledStatusEffect) },
            },
            new()
            {
                Feat = FeatType.DirtyJab3,
                EquipMainHandResref = "t_pglove",
                Target = AbilityTargetKind.HostileCreature,
                ExpectsRecast = true,
                ExpectsTargetDamage = true,
                ExpectsSTMCost = true,
                ExpectedTargetStatusEffects = new[] { typeof(RattledStatusEffect) },
            },
            new()
            {
                Feat = FeatType.DirtyJab4,
                EquipMainHandResref = "t_pglove",
                Target = AbilityTargetKind.HostileCreature,
                ExpectsRecast = true,
                ExpectsTargetDamage = true,
                ExpectsSTMCost = true,
                ExpectedTargetStatusEffects = new[] { typeof(RattledStatusEffect) },
            },
            new()
            {
                Feat = FeatType.Uppercut1,
                EquipMainHandResref = "t_pglove",
                Target = AbilityTargetKind.HostileCreature,
                ExpectsRecast = true,
                ExpectsTargetDamage = true,
                ExpectsSTMCost = true,
            },
            new()
            {
                Feat = FeatType.Uppercut2,
                EquipMainHandResref = "t_pglove",
                Target = AbilityTargetKind.HostileCreature,
                ExpectsRecast = true,
                ExpectsTargetDamage = true,
                ExpectsSTMCost = true,
            },
            new()
            {
                Feat = FeatType.Uppercut3,
                EquipMainHandResref = "t_pglove",
                Target = AbilityTargetKind.HostileCreature,
                ExpectsRecast = true,
                ExpectsTargetDamage = true,
                ExpectsSTMCost = true,
            },
            new()
            {
                Feat = FeatType.LowKick1,
                EquipMainHandResref = "t_pglove",
                Target = AbilityTargetKind.HostileCreature,
                ExpectsRecast = true,
                ExpectsTargetDamage = true,
                ExpectsSTMCost = true,
                ExpectedTargetStatusEffects = new[] { typeof(LowKickStatusEffect) },
            },
            new()
            {
                Feat = FeatType.LowKick2,
                EquipMainHandResref = "t_pglove",
                Target = AbilityTargetKind.HostileCreature,
                ExpectsRecast = true,
                ExpectsTargetDamage = true,
                ExpectsSTMCost = true,
                ExpectedTargetStatusEffects = new[] { typeof(LowKickStatusEffect) },
            },
            new()
            {
                Feat = FeatType.BruiserStance1,
                EquipMainHandResref = "t_pglove",
                Target = AbilityTargetKind.Self,
                ExpectsRecast = true,
                ExpectedActivatorStatusEffects = new[] { typeof(BruiserStanceStatusEffect) },
            },
            new()
            {
                Feat = FeatType.Haymaker1,
                EquipMainHandResref = "t_pglove",
                Target = AbilityTargetKind.HostileCreature,
                ExpectsRecast = true,
                ExpectsTargetDamage = true,
                ExpectsSTMCost = true,
            },
        };

        private static async Task EarnFinishAsync(EngineTestContext ctx, uint caster, uint target)
        {
            foreach (var feat in new[] { FeatType.Jab1, FeatType.PalmStrike1 })
            {
                var ability = Ability.GetAbilityDetail(feat);
                Ability.ClearLastCompletedAbilityImpactSummary(caster);
                await ctx.ExecuteInCreatureContextAsync(caster, () =>
                    ctx.Assert(UsePerkFeat.TryUseAbility(caster, target, feat, GetLocation(target)),
                        $"{ability.Name} starts: {Ability.GetLastActivationDenialReason()}"));
                await ctx.WaitUntilAsync(() => Ability.GetLastCompletedAbilityImpactSummary(caster)?.Ability == ability,
                    6f, $"{ability.Name} to complete its actual activation");
                // The next technique waits out the one-second technique lock.
                await ctx.DelaySecondsAsync(1.05f);
            }
            ctx.AssertEqual(MeleeTechniqueStage.Finish, MeleeTechnique.GetSetupStage(caster, target),
                "Jab and Palm Strike earn the Finish Perfect Form requires");
        }
    }
}
