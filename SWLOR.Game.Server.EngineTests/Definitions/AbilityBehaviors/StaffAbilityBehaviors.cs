using System.Collections.Generic;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions.AbilityBehaviors
{
    public class StaffAbilityBehaviors : IAbilityBehaviorSource
    {
        private const string StaffResref = "nw_wdbqs001";

        [EngineTest("Staff ability behaviors", Category = "AbilityBehavior", TimeoutSeconds = 1800f)]
        public static async Task Run(EngineTestContext ctx)
        {
            await AbilityBehaviorExecutor.RunAsync(ctx, new StaffAbilityBehaviors().BuildCases());
        }

        public List<AbilityBehaviorCase> BuildCases()
        {
            return new List<AbilityBehaviorCase>
            {
                // CrusherStanceAbilityDefinition - self stance path.
                new()
                {
                    Feat = FeatType.CrusherStance1,
                    Target = AbilityTargetKind.Self,
                    ExpectedActivatorStatusEffects = new[] { typeof(CrusherStanceStatusEffect) },
                    ExpectsRecast = true,
                },

                // GroundQuakeAbilityDefinition - Casted sphere AoE centered on self.
                new()
                {
                    Feat = FeatType.GroundQuake1,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = StaffResref,
                    TargetSetupStatusEffectFactory = () => new DazedStatusEffect(),
                    ExpectedTargetStatusEffects = new[] { typeof(KnockdownStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "A pre-existing Dazed status must allow the intended conditional Knockdown before shared hard-control immunity begins.",
                },
                new()
                {
                    Feat = FeatType.GroundQuake2,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = StaffResref,
                    TargetSetupStatusEffectFactory = () => new DazedStatusEffect(),
                    ExpectedTargetStatusEffects = new[] { typeof(KnockdownStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // LegSweepAbilityDefinition - Casted single-target hostile strike.
                new()
                {
                    Feat = FeatType.LegSweep1,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = StaffResref,
                    ExpectedTargetStatusEffects = new[] { typeof(KnockdownStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.LegSweep2,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = StaffResref,
                    ExpectedTargetStatusEffects = new[] { typeof(KnockdownStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.LegSweep3,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = StaffResref,
                    ExpectedTargetStatusEffects = new[] { typeof(KnockdownStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // LineBreakerAbilityDefinition - Casted line (Rect) AoE centered on self.
                new()
                {
                    Feat = FeatType.LineBreaker1,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = StaffResref,
                    ExpectedTargetStatusEffects = new[] { typeof(DisorientedStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.LineBreaker2,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = StaffResref,
                    ExpectedTargetStatusEffects = new[] { typeof(DisorientedStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.LineBreaker3,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = StaffResref,
                    ExpectedTargetStatusEffects = new[] { typeof(DisorientedStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.LineBreaker4,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = StaffResref,
                    ExpectedTargetStatusEffects = new[] { typeof(DisorientedStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // RibBreakerAbilityDefinition - Casted single-target hostile strike.
                new()
                {
                    Feat = FeatType.RibBreaker1,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = StaffResref,
                    ExpectedTargetStatusEffects = new[] { typeof(DazedStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.RibBreaker2,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = StaffResref,
                    ExpectedTargetStatusEffects = new[] { typeof(DazedStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.RibBreaker3,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = StaffResref,
                    ExpectedTargetStatusEffects = new[] { typeof(DazedStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // SentinelStanceAbilityDefinition - self stance path.
                new()
                {
                    Feat = FeatType.SentinelStance1,
                    Target = AbilityTargetKind.Self,
                    ExpectedActivatorStatusEffects = new[] { typeof(SentinelStanceStatusEffect) },
                    ExpectsRecast = true,
                },

                // ShelterCircleAbilityDefinition - self/party buff (NearbyPartyStatusIncludesSelf=true).
                new()
                {
                    Feat = FeatType.ShelterCircle1,
                    Target = AbilityTargetKind.Self,
                    ExpectedActivatorStatusEffects = new[] { typeof(ShelterCircleStatusEffect) },
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // SlamAbilityDefinition - Casted single-target hostile strike.
                new()
                {
                    Feat = FeatType.Slam1,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = StaffResref,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Extra damage against a controlled target is conditional and not guaranteed by the harness.",
                },
                new()
                {
                    Feat = FeatType.Slam2,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = StaffResref,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.Slam3,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = StaffResref,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.Slam4,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = StaffResref,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // SweepingGuardAbilityDefinition - Casted sphere AoE centered on self.
                new()
                {
                    Feat = FeatType.SweepingGuard1,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = StaffResref,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Grants a self physical-defense buff on impact; a temporary stat modifier, not a status effect class, not asserted.",
                },

                // UnmovingCenterAbilityDefinition - capstone self status (not hostile/friendly).
                new()
                {
                    Feat = FeatType.UnmovingCenter1,
                    Target = AbilityTargetKind.Self,
                    ExpectedActivatorStatusEffects = new[] { typeof(UnmovingCenterStatusEffect) },
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Also grants temporary Knockdown/Dazed immunity (engine effect, not asserted).",
                },

                // WorldbreakerAbilityDefinition - capstone Casted sphere AoE centered on self.
                new()
                {
                    Feat = FeatType.Worldbreaker1,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = StaffResref,
                    TargetSetupStatusEffects = new[] { typeof(HamstringStatusEffect) },
                    ExpectedTargetStatusEffects = new[] { typeof(DazedStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Hamstring supplies the prerequisite Control status without triggering the shared hard-control immunity gate.",
                },
            };
        }
    }
}
