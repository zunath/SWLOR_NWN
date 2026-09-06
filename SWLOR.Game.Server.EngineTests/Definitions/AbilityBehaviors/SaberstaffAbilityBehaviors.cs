using System.Collections.Generic;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions.AbilityBehaviors
{
    /// <summary>
    /// Declarative engine-test coverage for every FeatType registered by the Saberstaff
    /// ability definitions (SWLOR.Game.Server/Feature/AbilityDefinition/Saberstaff).
    /// </summary>
    public class SaberstaffAbilityBehaviors : IAbilityBehaviorSource
    {
        private const string SaberstaffResref = "vet_trnsabstaff";

        [EngineTest("Saberstaff ability behaviors", Category = "AbilityBehavior", TimeoutSeconds = 1800f)]
        public static async Task Run(EngineTestContext ctx)
        {
            await AbilityBehaviorExecutor.RunAsync(ctx, new SaberstaffAbilityBehaviors().BuildCases());
        }

        public List<AbilityBehaviorCase> BuildCases()
        {
            return new List<AbilityBehaviorCase>
            {
                // CircleSlashAbilityDefinition - hostile AoE; self attack-deflection buff is a stat
                // modifier (not a status effect type) so it is not asserted.
                new AbilityBehaviorCase
                {
                    Feat = FeatType.CircleSlash1,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = SaberstaffResref,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Grants self attack-deflection via a temporary stat modifier, not a status effect class; not asserted."
                },
                new AbilityBehaviorCase
                {
                    Feat = FeatType.CircleSlash2,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = SaberstaffResref,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Grants self attack-deflection via a temporary stat modifier, not a status effect class; not asserted."
                },
                new AbilityBehaviorCase
                {
                    Feat = FeatType.CircleSlash3,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = SaberstaffResref,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Grants self attack-deflection via a temporary stat modifier, not a status effect class; not asserted."
                },

                // ConduitStanceAbilityDefinition - self-toggle stance, no resource cost.
                new AbilityBehaviorCase
                {
                    Feat = FeatType.ConduitStance1,
                    Target = AbilityTargetKind.Self,
                    EquipMainHandResref = SaberstaffResref,
                    ExpectedActivatorStatusEffects = new[] { typeof(ConduitStanceStatusEffect) },
                    ExpectsRecast = true
                },

                // DoubleStrikeAbilityDefinition - hostile 2-hit casted strike; FP restore is
                // conditional on both hits landing, not asserted.
                new AbilityBehaviorCase
                {
                    Feat = FeatType.DoubleStrike1,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = SaberstaffResref,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Two-hit impact; FP restore only if both hits land (conditional, not asserted)."
                },
                new AbilityBehaviorCase
                {
                    Feat = FeatType.DoubleStrike2,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = SaberstaffResref,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Two-hit impact; FP restore only if both hits land (conditional, not asserted)."
                },
                new AbilityBehaviorCase
                {
                    Feat = FeatType.DoubleStrike3,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = SaberstaffResref,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Two-hit impact; FP restore only if both hits land (conditional, not asserted)."
                },
                new AbilityBehaviorCase
                {
                    Feat = FeatType.DoubleStrike4,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = SaberstaffResref,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Two-hit impact; FP restore only if both hits land (conditional, not asserted)."
                },

                // FocusedArcAbilityDefinition - hostile single-target damage; the high-resource bonus
                // damage is conditional on the activator's FP/STM being at or above 60%.
                new AbilityBehaviorCase
                {
                    Feat = FeatType.FocusedArc1,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = SaberstaffResref,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Bonus damage above 60% resources is conditional; base damage assertion is enough to cover the impact."
                },
                new AbilityBehaviorCase
                {
                    Feat = FeatType.FocusedArc2,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = SaberstaffResref,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Bonus damage above 60% resources is conditional; base damage assertion is enough to cover the impact."
                },
                new AbilityBehaviorCase
                {
                    Feat = FeatType.FocusedArc3,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = SaberstaffResref,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Bonus damage above 60% resources is conditional; base damage assertion is enough to cover the impact."
                },
                new AbilityBehaviorCase
                {
                    Feat = FeatType.FocusedArc4,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = SaberstaffResref,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Bonus damage above 60% resources is conditional; base damage assertion is enough to cover the impact."
                },

                // GuardedChannelAbilityDefinition - hostile damage; self-defense buff is a conditional
                // stat modifier (requires resources above 40%), not a status effect type.
                new AbilityBehaviorCase
                {
                    Feat = FeatType.GuardedChannel1,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = SaberstaffResref,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Self-defense buff is a conditional temporary stat modifier, not a status effect class; not asserted."
                },
                new AbilityBehaviorCase
                {
                    Feat = FeatType.GuardedChannel2,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = SaberstaffResref,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Self-defense buff is a conditional temporary stat modifier, not a status effect class; not asserted."
                },
                new AbilityBehaviorCase
                {
                    Feat = FeatType.GuardedChannel3,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = SaberstaffResref,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Self-defense buff is a conditional temporary stat modifier, not a status effect class; not asserted."
                },

                // InfiniteConduitAbilityDefinition - capstone self-toggle stance.
                new AbilityBehaviorCase
                {
                    Feat = FeatType.InfiniteConduit1,
                    Target = AbilityTargetKind.Self,
                    EquipMainHandResref = SaberstaffResref,
                    ExpectedActivatorStatusEffects = new[] { typeof(InfiniteConduitStatusEffect) },
                    ExpectsSTMCost = true,
                    ExpectsRecast = true
                },

                // MaelstromArcAbilityDefinition - hostile cone AoE; per-hit FP restore is conditional.
                new AbilityBehaviorCase
                {
                    Feat = FeatType.MaelstromArc1,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = SaberstaffResref,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "FP restore on hit is a conditional rider, not an upfront cost/effect; not asserted."
                },
                new AbilityBehaviorCase
                {
                    Feat = FeatType.MaelstromArc2,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = SaberstaffResref,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "FP restore on hit is a conditional rider, not an upfront cost/effect; not asserted."
                },

                // SaberCycloneAbilityDefinition - capstone self buff. For 45 seconds, later area
                // combat abilities restore FP and grant Attack Deflection; the activation itself
                // neither targets nor damages an enemy.
                new AbilityBehaviorCase
                {
                    Feat = FeatType.SaberCyclone1,
                    Target = AbilityTargetKind.Self,
                    EquipMainHandResref = SaberstaffResref,
                    ExpectedActivatorStatAdjustments = new Dictionary<StatType, int>
                    {
                        [StatType.AreaAbilityUsedFPRestore] = 4,
                        [StatType.AreaAbilityUsedAttackDeflection] = 8,
                        [StatType.AreaAbilityUsedAttackDeflectionDurationSeconds] = 30
                    },
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "The three temporary stat modifiers last 45 seconds and empower subsequent hostile area ability uses, including empty areas."
                },

                // SeverFocusAbilityDefinition - hostile damage; resource drain is conditional on the
                // activator's own FP/STM being above 80%.
                new AbilityBehaviorCase
                {
                    Feat = FeatType.SeverFocus1,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = SaberstaffResref,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Target resource drain requires the activator's FP/STM above 80% (conditional, not asserted)."
                },
                new AbilityBehaviorCase
                {
                    Feat = FeatType.SeverFocus2,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = SaberstaffResref,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Target resource drain requires the activator's FP/STM above 80% (conditional, not asserted)."
                },

                // TempestStanceAbilityDefinition - self-toggle stance (isArea:true is vestigial here
                // since isHostile:false routes it through the self-status branch regardless).
                new AbilityBehaviorCase
                {
                    Feat = FeatType.TempestStance1,
                    Target = AbilityTargetKind.Self,
                    EquipMainHandResref = SaberstaffResref,
                    ExpectedActivatorStatusEffects = new[] { typeof(TempestStanceStatusEffect) },
                    ExpectsRecast = true
                }
            };
        }
    }
}
