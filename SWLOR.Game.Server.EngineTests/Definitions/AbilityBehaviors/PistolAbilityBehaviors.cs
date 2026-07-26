using System.Collections.Generic;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions.AbilityBehaviors
{
    /// <summary>
    /// Declarative engine-test coverage for every FeatType registered by the Pistol
    /// ability definitions (SWLOR.Game.Server/Feature/AbilityDefinition/Pistol).
    /// </summary>
    public class PistolAbilityBehaviors : IAbilityBehaviorSource
    {
        private const string PistolResref = "b_pistol";

        [EngineTest("Pistol ability behaviors", Category = "AbilityBehavior", TimeoutSeconds = 1800f)]
        public static async Task Run(EngineTestContext ctx)
        {
            await AbilityBehaviorExecutor.RunAsync(ctx, new PistolAbilityBehaviors().BuildCases());
        }

        public List<AbilityBehaviorCase> BuildCases()
        {
            return new List<AbilityBehaviorCase>
            {
                // DeadMansHandAbilityDefinition - capstone hostile single-target damage.
                new AbilityBehaviorCase
                {
                    Feat = FeatType.DeadMansHand1,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = PistolResref,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true
                },

                // DisarmingShotAbilityDefinition - hostile damage; target attack-percent debuff is an
                // unconditional temporary stat modifier on the target, not a status effect type.
                new AbilityBehaviorCase
                {
                    Feat = FeatType.DisarmingShot1,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = PistolResref,
                    ExpectedTargetStatAdjustments = new() { [StatType.AttackPercentAdjustment] = -10 },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Asserts the unconditional -10 attack-percent temporary modifier as well as damage."
                },
                new AbilityBehaviorCase
                {
                    Feat = FeatType.DisarmingShot2,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = PistolResref,
                    ExpectedTargetStatAdjustments = new() { [StatType.AttackPercentAdjustment] = -12 },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
                new AbilityBehaviorCase
                {
                    Feat = FeatType.DisarmingShot3,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = PistolResref,
                    ExpectedTargetStatAdjustments = new() { [StatType.AttackPercentAdjustment] = -15 },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
                new AbilityBehaviorCase
                {
                    Feat = FeatType.DisarmingShot4,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = PistolResref,
                    ExpectedTargetStatAdjustments = new() { [StatType.AttackPercentAdjustment] = -18 },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // DoubleShotAbilityDefinition - hostile 2-hit casted damage; crit-hit stamina restore
                // is conditional.
                new AbilityBehaviorCase
                {
                    Feat = FeatType.DoubleShot1,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = PistolResref,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    ImpactRefundsCosts = true,
                    Notes = "Two-hit impact; RestoreStaminaIfAnyCriticalHit refunds 2 STM when a hit crits, so only the net stamina dip is observable."
                },
                new AbilityBehaviorCase
                {
                    Feat = FeatType.DoubleShot2,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = PistolResref,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    ImpactRefundsCosts = true,
                    Notes = "Two-hit impact; RestoreStaminaIfAnyCriticalHit refunds 3 STM when a hit crits, so only the net stamina dip is observable."
                },
                new AbilityBehaviorCase
                {
                    Feat = FeatType.DoubleShot3,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = PistolResref,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    ImpactRefundsCosts = true,
                    Notes = "Two-hit impact; RestoreStaminaIfAnyCriticalHit refunds 4 STM when a hit crits, making even the net deduction nondeterministic - only the dip is asserted."
                },

                // FanTheHammerAbilityDefinition - hostile AoE (max 3 targets) damage.
                new AbilityBehaviorCase
                {
                    Feat = FeatType.FanTheHammer1,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = PistolResref,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true
                },
                new AbilityBehaviorCase
                {
                    Feat = FeatType.FanTheHammer2,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = PistolResref,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true
                },

                // GamblerStanceAbilityDefinition - self-toggle stance, no resource cost.
                new AbilityBehaviorCase
                {
                    Feat = FeatType.GamblerStance1,
                    Target = AbilityTargetKind.Self,
                    EquipMainHandResref = PistolResref,
                    ExpectedActivatorStatusEffects = new[] { typeof(GamblerStanceStatusEffect) },
                    ExpectsRecast = true
                },

                // InterruptingShotAbilityDefinition - hostile damage; Disoriented only applies if the
                // target is mid-activation of its own ability at impact time (conditional).
                new AbilityBehaviorCase
                {
                    Feat = FeatType.InterruptingShot1,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = PistolResref,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Disoriented only applies if the target is using an ability at impact (conditional, not asserted)."
                },
                new AbilityBehaviorCase
                {
                    Feat = FeatType.InterruptingShot2,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = PistolResref,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Disoriented only applies if the target is using an ability at impact (conditional, not asserted)."
                },
                new AbilityBehaviorCase
                {
                    Feat = FeatType.InterruptingShot3,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = PistolResref,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Disoriented only applies if the target is using an ability at impact (conditional, not asserted)."
                },

                // LastWordAbilityDefinition - capstone hostile single-target damage; riders require an
                // avoided attack or a defeated enemy (conditional).
                new AbilityBehaviorCase
                {
                    Feat = FeatType.LastWord1,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = PistolResref,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Avoided-attack/defeated-enemy riders are conditional temporary stat modifiers; not asserted."
                },

                // PointBlankBurstAbilityDefinition - hostile AoE; self-evasion buff is an unconditional
                // temporary stat modifier on the activator, not a status effect type.
                new AbilityBehaviorCase
                {
                    Feat = FeatType.PointBlankBurst1,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = PistolResref,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Self-evasion buff is a temporary stat modifier, not a status effect class; not asserted."
                },
                new AbilityBehaviorCase
                {
                    Feat = FeatType.PointBlankBurst2,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = PistolResref,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Self-evasion buff is a temporary stat modifier, not a status effect class; not asserted."
                },

                // QuickDrawAbilityDefinition - hostile damage; crit-rate bonus vs a not-recently-hit
                // target is a hit-roll modifier, not a status effect type.
                new AbilityBehaviorCase
                {
                    Feat = FeatType.QuickDraw1,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = PistolResref,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Crit-rate bonus vs a not-recently-hit target is a hit-roll modifier, not a status effect; not asserted."
                },
                new AbilityBehaviorCase
                {
                    Feat = FeatType.QuickDraw2,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = PistolResref,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Crit-rate bonus vs a not-recently-hit target is a hit-roll modifier, not a status effect; not asserted."
                },
                new AbilityBehaviorCase
                {
                    Feat = FeatType.QuickDraw3,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = PistolResref,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Crit-rate bonus vs a not-recently-hit target is a hit-roll modifier, not a status effect; not asserted."
                },
                new AbilityBehaviorCase
                {
                    Feat = FeatType.QuickDraw4,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = PistolResref,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Crit-rate bonus vs a not-recently-hit target is a hit-roll modifier, not a status effect; not asserted."
                },

                // SkirmisherStanceAbilityDefinition - self-toggle stance, no resource cost.
                new AbilityBehaviorCase
                {
                    Feat = FeatType.SkirmisherStance1,
                    Target = AbilityTargetKind.Self,
                    EquipMainHandResref = PistolResref,
                    ExpectedActivatorStatusEffects = new[] { typeof(SkirmisherStanceStatusEffect) },
                    ExpectsRecast = true
                }
            };
        }
    }
}
