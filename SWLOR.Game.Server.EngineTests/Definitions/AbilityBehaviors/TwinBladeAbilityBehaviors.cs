using System.Collections.Generic;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions.AbilityBehaviors
{
    public class TwinBladeAbilityBehaviors : IAbilityBehaviorSource
    {
        private const string TwinBladeResref = "nw_wdbax001";

        [EngineTest("Twin Blade ability behaviors", Category = "AbilityBehavior", TimeoutSeconds = 1800f)]
        public static async Task Run(EngineTestContext ctx)
        {
            await AbilityBehaviorExecutor.RunAsync(ctx, new TwinBladeAbilityBehaviors().BuildCases());
        }

        public List<AbilityBehaviorCase> BuildCases()
        {
            return new List<AbilityBehaviorCase>
            {
                // BladeVortexAbilityDefinition - ConfigureWeaponAbility, isArea+isHostile, not
                // queued (IsQueuedWeaponAbility unset) so this is a Casted sphere AoE, not a weapon ability.
                new()
                {
                    Feat = FeatType.BladeVortex1,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = TwinBladeResref,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "The 4 STM refund requires at least three landed targets; this single-target fixture pays the full cost.",
                },
                new()
                {
                    Feat = FeatType.BladeVortex2,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = TwinBladeResref,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "The 6 STM refund requires at least three landed targets; this single-target fixture pays the full cost.",
                },

                // CrossCutAbilityDefinition - Casted single-target, 2 hits.
                new()
                {
                    Feat = FeatType.CrossCut1,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = TwinBladeResref,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Grants self-haste if both hits land; not asserted (stat modifier, not a status effect class).",
                },
                new()
                {
                    Feat = FeatType.CrossCut2,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = TwinBladeResref,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.CrossCut3,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = TwinBladeResref,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.CrossCut4,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = TwinBladeResref,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // CycloneStanceAbilityDefinition - not hostile/friendly, statusEffect set -> self stance path.
                new()
                {
                    Feat = FeatType.CycloneStance1,
                    Target = AbilityTargetKind.Self,
                    ExpectedActivatorStatusEffects = new[] { typeof(CycloneStanceStatusEffect) },
                    ExpectsRecast = true,
                },

                // LaceratingTwinCutAbilityDefinition - Casted single-target, 2 hits, applies Bleed.
                new()
                {
                    Feat = FeatType.LaceratingTwinCut1,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = TwinBladeResref,
                    ExpectedTargetStatusEffects = new[] { typeof(BleedStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.LaceratingTwinCut2,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = TwinBladeResref,
                    ExpectedTargetStatusEffects = new[] { typeof(BleedStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.LaceratingTwinCut3,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = TwinBladeResref,
                    ExpectedTargetStatusEffects = new[] { typeof(BleedStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.LaceratingTwinCut4,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = TwinBladeResref,
                    ExpectedTargetStatusEffects = new[] { typeof(BleedStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // LaceratorStanceAbilityDefinition - self stance path.
                new()
                {
                    Feat = FeatType.LaceratorStance1,
                    Target = AbilityTargetKind.Self,
                    ExpectedActivatorStatusEffects = new[] { typeof(LaceratorStanceStatusEffect) },
                    ExpectsRecast = true,
                },

                // RedBloomAbilityDefinition - capstone Casted sphere AoE centered on self.
                new()
                {
                    Feat = FeatType.RedBloom1,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = TwinBladeResref,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Bonus bleed-consumption/hemorrhage-spread riders require the target to already be bleeding; a fresh spawn is not, so no status effect is asserted.",
                },

                // SerratedArcAbilityDefinition - Casted sphere AoE centered on self.
                new()
                {
                    Feat = FeatType.SerratedArc1,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = TwinBladeResref,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "SpreadBleedFromTarget only fires if the target is already bleeding; not guaranteed, so no status effect is asserted.",
                },
                new()
                {
                    Feat = FeatType.SerratedArc2,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = TwinBladeResref,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.SerratedArc3,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = TwinBladeResref,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // SpinningWhirlAbilityDefinition - Casted sphere AoE centered on self, capped targets.
                new()
                {
                    Feat = FeatType.SpinningWhirl1,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = TwinBladeResref,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.SpinningWhirl2,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = TwinBladeResref,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.SpinningWhirl3,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = TwinBladeResref,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // TempestBloomAbilityDefinition - capstone Casted sphere AoE centered on self.
                new()
                {
                    Feat = FeatType.TempestBloom1,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = TwinBladeResref,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Fragmentation pulse and defeated-enemy bonuses are temporary stat modifiers, not status effect classes; not asserted.",
                },

                // TwinRuptureAbilityDefinition - Casted single-target.
                new()
                {
                    Feat = FeatType.TwinRupture1,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = TwinBladeResref,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "ConditionalTargetStatusEffect (Hemorrhage) requires the target to already carry a Bleeding-category status; not guaranteed, so not asserted.",
                },
                new()
                {
                    Feat = FeatType.TwinRupture2,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = TwinBladeResref,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
            };
        }
    }
}
