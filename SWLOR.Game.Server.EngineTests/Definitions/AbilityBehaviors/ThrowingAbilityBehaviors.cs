using System.Collections.Generic;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions.AbilityBehaviors
{
    /// <summary>
    /// Declarative engine-test coverage for every FeatType registered by the Throwing
    /// ability definitions (SWLOR.Game.Server/Feature/AbilityDefinition/Throwing).
    /// </summary>
    public class ThrowingAbilityBehaviors : IAbilityBehaviorSource
    {
        private const string ThrowingResref = "b_shuriken";

        [EngineTest("Throwing ability behaviors", Category = "AbilityBehavior", TimeoutSeconds = 1800f)]
        public static async Task Run(EngineTestContext ctx)
        {
            await AbilityBehaviorExecutor.RunAsync(ctx, new ThrowingAbilityBehaviors().BuildCases());
        }

        public List<AbilityBehaviorCase> BuildCases()
        {
            return new List<AbilityBehaviorCase>
            {
                // ConcussiveTossAbilityDefinition - hostile AoE damage + unconditional Dazed.
                new AbilityBehaviorCase
                {
                    Feat = FeatType.ConcussiveToss1,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = ThrowingResref,
                    ExpectedTargetStatusEffects = new[] { typeof(DazedStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true
                },
                new AbilityBehaviorCase
                {
                    Feat = FeatType.ConcussiveToss2,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = ThrowingResref,
                    ExpectedTargetStatusEffects = new[] { typeof(DazedStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true
                },

                // ExplosiveTossAbilityDefinition - hostile AoE damage + unconditional Burn.
                new AbilityBehaviorCase
                {
                    Feat = FeatType.ExplosiveToss1,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = ThrowingResref,
                    ExpectedTargetStatusEffects = new[] { typeof(BurnStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true
                },
                new AbilityBehaviorCase
                {
                    Feat = FeatType.ExplosiveToss2,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = ThrowingResref,
                    ExpectedTargetStatusEffects = new[] { typeof(BurnStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true
                },
                new AbilityBehaviorCase
                {
                    Feat = FeatType.ExplosiveToss3,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = ThrowingResref,
                    ExpectedTargetStatusEffects = new[] { typeof(BurnStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true
                },
                new AbilityBehaviorCase
                {
                    Feat = FeatType.ExplosiveToss4,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = ThrowingResref,
                    ExpectedTargetStatusEffects = new[] { typeof(BurnStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true
                },

                // FlashTossAbilityDefinition - hostile damage + unconditional Blind.
                new AbilityBehaviorCase
                {
                    Feat = FeatType.FlashToss1,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = ThrowingResref,
                    ExpectedTargetStatusEffects = new[] { typeof(BlindStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true
                },
                new AbilityBehaviorCase
                {
                    Feat = FeatType.FlashToss2,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = ThrowingResref,
                    ExpectedTargetStatusEffects = new[] { typeof(BlindStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true
                },
                new AbilityBehaviorCase
                {
                    Feat = FeatType.FlashToss3,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = ThrowingResref,
                    ExpectedTargetStatusEffects = new[] { typeof(BlindStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true
                },

                // FlurryStanceAbilityDefinition - self-toggle stance, no resource cost.
                new AbilityBehaviorCase
                {
                    Feat = FeatType.FlurryStance1,
                    Target = AbilityTargetKind.Self,
                    EquipMainHandResref = ThrowingResref,
                    ExpectedActivatorStatusEffects = new[] { typeof(FlurryStanceStatusEffect) },
                    ExpectsRecast = true
                },

                // OrdnanceStanceAbilityDefinition - self-toggle stance (isArea:true is vestigial since
                // isHostile:false routes it through the self-status branch regardless).
                new AbilityBehaviorCase
                {
                    Feat = FeatType.OrdnanceStance1,
                    Target = AbilityTargetKind.Self,
                    EquipMainHandResref = ThrowingResref,
                    ExpectedActivatorStatusEffects = new[] { typeof(OrdnanceStanceStatusEffect) },
                    ExpectsRecast = true
                },

                // PerfectFlurryAbilityDefinition - capstone hostile AoE damage; bleed-spread riders
                // require the target to already be bleeding (conditional, not present on a fresh target).
                new AbilityBehaviorCase
                {
                    Feat = FeatType.PerfectFlurry1,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = ThrowingResref,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Bleed-spread and bonus damage vs bleeding targets require a pre-existing Bleed (conditional, not asserted)."
                },

                // PiercingTossAbilityDefinition - hostile damage + unconditional Bleed.
                new AbilityBehaviorCase
                {
                    Feat = FeatType.PiercingToss1,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = ThrowingResref,
                    ExpectedTargetStatusEffects = new[] { typeof(BleedStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true
                },
                new AbilityBehaviorCase
                {
                    Feat = FeatType.PiercingToss2,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = ThrowingResref,
                    ExpectedTargetStatusEffects = new[] { typeof(BleedStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true
                },
                new AbilityBehaviorCase
                {
                    Feat = FeatType.PiercingToss3,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = ThrowingResref,
                    ExpectedTargetStatusEffects = new[] { typeof(BleedStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true
                },
                new AbilityBehaviorCase
                {
                    Feat = FeatType.PiercingToss4,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = ThrowingResref,
                    ExpectedTargetStatusEffects = new[] { typeof(BleedStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true
                },

                // PinningTossAbilityDefinition - hostile damage + unconditional Hamstring; Disoriented
                // requires the target already be Bleeding (conditional, not asserted).
                new AbilityBehaviorCase
                {
                    Feat = FeatType.PinningToss1,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = ThrowingResref,
                    ExpectedTargetStatusEffects = new[] { typeof(HamstringStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Disoriented additionally requires the target already have a Bleeding-category status (conditional, not asserted)."
                },
                new AbilityBehaviorCase
                {
                    Feat = FeatType.PinningToss2,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = ThrowingResref,
                    ExpectedTargetStatusEffects = new[] { typeof(HamstringStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Disoriented additionally requires the target already have a Bleeding-category status (conditional, not asserted)."
                },
                new AbilityBehaviorCase
                {
                    Feat = FeatType.PinningToss3,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = ThrowingResref,
                    ExpectedTargetStatusEffects = new[] { typeof(HamstringStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Disoriented additionally requires the target already have a Bleeding-category status (conditional, not asserted)."
                },

                // RainOfSteelAbilityDefinition - capstone hostile AoE damage; fragmentation/defeated-
                // enemy riders are temporary stat modifiers, not status effect types.
                new AbilityBehaviorCase
                {
                    Feat = FeatType.RainOfSteel1,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = ThrowingResref,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Fragmentation/defeated-enemy riders are temporary stat modifiers, not status effect classes; not asserted."
                },

                // SeveringTossAbilityDefinition - hostile damage; Hemorrhage requires the target already
                // be Bleeding (conditional, not asserted).
                new AbilityBehaviorCase
                {
                    Feat = FeatType.SeveringToss1,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = ThrowingResref,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Hemorrhage requires the target already have a Bleeding-category status (conditional, not asserted)."
                },
                new AbilityBehaviorCase
                {
                    Feat = FeatType.SeveringToss2,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = ThrowingResref,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Hemorrhage requires the target already have a Bleeding-category status (conditional, not asserted)."
                }
            };
        }
    }
}
