using System.Collections.Generic;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions.AbilityBehaviors
{
    public class SpearAbilityBehaviors : IAbilityBehaviorSource
    {
        private const string SpearResref = "nw_wplhb001";

        [EngineTest("Spear ability behaviors", Category = "AbilityBehavior", TimeoutSeconds = 1800f)]
        public static async Task Run(EngineTestContext ctx)
        {
            await AbilityBehaviorExecutor.RunAsync(ctx, new SpearAbilityBehaviors().BuildCases());
        }

        public List<AbilityBehaviorCase> BuildCases()
        {
            return new List<AbilityBehaviorCase>
            {
                // CripplingDefenseAbilityDefinition - capstone Casted single-target hostile strike.
                new()
                {
                    Feat = FeatType.CripplingDefense1,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = SpearResref,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Also marks the activator as taking on an Exposed-on-costly-ability rider; a temporary stat modifier, not a status effect class, not asserted.",
                },

                // DisablingStrikeAbilityDefinition - Casted single-target hostile strike.
                new()
                {
                    Feat = FeatType.DisablingStrike1,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = SpearResref,
                    ExpectedTargetStatusEffects = new[] { typeof(FoggyMindStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.DisablingStrike2,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = SpearResref,
                    ExpectedTargetStatusEffects = new[] { typeof(FoggyMindStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.DisablingStrike3,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = SpearResref,
                    ExpectedTargetStatusEffects = new[] { typeof(FoggyMindStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.DisablingStrike4,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = SpearResref,
                    ExpectedTargetStatusEffects = new[] { typeof(FoggyMindStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // DisruptionFieldAbilityDefinition - Casted sphere AoE centered on self.
                new()
                {
                    Feat = FeatType.DisruptionField1,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = SpearResref,
                    ExpectedTargetStatusEffects = new[] { typeof(ForceDisruptionStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.DisruptionField2,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = SpearResref,
                    ExpectedTargetStatusEffects = new[] { typeof(ForceDisruptionStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // ForcebaneAbilityDefinition - capstone Casted sphere AoE centered on self.
                new()
                {
                    Feat = FeatType.Forcebane1,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = SpearResref,
                    ExpectedTargetStatusEffects = new[] { typeof(ForceDisruptionStatusEffect), typeof(FoggyMindStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // HamperingBarrageAbilityDefinition - Casted cone AoE centered on self.
                new()
                {
                    Feat = FeatType.HamperingBarrage1,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = SpearResref,
                    ExpectedTargetStatusEffects = new[] { typeof(HamstringStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.HamperingBarrage2,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = SpearResref,
                    ExpectedTargetStatusEffects = new[] { typeof(HamstringStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // InterruptionStrikeAbilityDefinition - Casted single-target hostile strike.
                new()
                {
                    Feat = FeatType.InterruptionStrike1,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = SpearResref,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "FP/Stamina drain rider only fires while the target is mid-activation (Combat.IsUsingAbility); not guaranteed on a passive spawn, so not asserted.",
                },
                new()
                {
                    Feat = FeatType.InterruptionStrike2,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = SpearResref,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.InterruptionStrike3,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = SpearResref,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // PerceptiveStanceAbilityDefinition - self stance path.
                new()
                {
                    Feat = FeatType.PerceptiveStance1,
                    Target = AbilityTargetKind.Self,
                    ExpectedActivatorStatusEffects = new[] { typeof(PerceptiveStanceStatusEffect) },
                    ExpectsRecast = true,
                },

                // SweepingFlankAbilityDefinition - Casted sphere AoE centered on self.
                new()
                {
                    Feat = FeatType.SweepingFlank1,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = SpearResref,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Extra damage when beside/behind the target is positional and not guaranteed by the harness.",
                },
                new()
                {
                    Feat = FeatType.SweepingFlank2,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = SpearResref,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.SweepingFlank3,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = SpearResref,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // VigorStanceAbilityDefinition - self stance path.
                new()
                {
                    Feat = FeatType.VigorStance1,
                    Target = AbilityTargetKind.Self,
                    ExpectedActivatorStatusEffects = new[] { typeof(VigorStanceStatusEffect) },
                    ExpectsRecast = true,
                },

                // VigorThrustAbilityDefinition - Casted single-target hostile strike.
                new()
                {
                    Feat = FeatType.VigorThrust1,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = SpearResref,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Grants a self evasion buff on impact; a temporary stat modifier, not a status effect class, not asserted.",
                },
                new()
                {
                    Feat = FeatType.VigorThrust2,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = SpearResref,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.VigorThrust3,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = SpearResref,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.VigorThrust4,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = SpearResref,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
            };
        }
    }
}
