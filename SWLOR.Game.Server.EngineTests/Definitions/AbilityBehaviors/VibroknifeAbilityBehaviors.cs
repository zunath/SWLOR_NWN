using System.Collections.Generic;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions.AbilityBehaviors
{
    public class VibroknifeAbilityBehaviors : IAbilityBehaviorSource
    {
        private const string VibroknifeResref = "nw_wswdg001";

        [EngineTest("Vibroknife ability behaviors", Category = "AbilityBehavior", TimeoutSeconds = 1800f)]
        public static async Task Run(EngineTestContext ctx)
        {
            await AbilityBehaviorExecutor.RunAsync(ctx, new VibroknifeAbilityBehaviors().BuildCases());
        }

        public List<AbilityBehaviorCase> BuildCases()
        {
            return new List<AbilityBehaviorCase>
            {
                // AssassinsStanceAbilityDefinition - self stance path (not hostile/friendly, statusEffect set).
                new()
                {
                    Feat = FeatType.AssassinsStance1,
                    Target = AbilityTargetKind.Self,
                    ExpectedActivatorStatusEffects = new[] { typeof(AssassinsStanceStatusEffect) },
                    ExpectsRecast = true,
                },

                // BackstabAbilityDefinition - Casted single-target hostile strike.
                new()
                {
                    Feat = FeatType.Backstab1,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = VibroknifeResref,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "ConditionalTargetStatusEffect (Knockdown) requires the activator to be positioned behind the target; positioning isn't guaranteed by the harness, so not asserted.",
                },
                new()
                {
                    Feat = FeatType.Backstab2,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = VibroknifeResref,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // CripplingSliceAbilityDefinition - Casted single-target hostile strike.
                new()
                {
                    Feat = FeatType.CripplingSlice1,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = VibroknifeResref,
                    ExpectedTargetStatusEffects = new[] { typeof(ExposedStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.CripplingSlice2,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = VibroknifeResref,
                    ExpectedTargetStatusEffects = new[] { typeof(ExposedStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.CripplingSlice3,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = VibroknifeResref,
                    ExpectedTargetStatusEffects = new[] { typeof(ExposedStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // EscapeArtistAbilityDefinition - capstone Casted single-target hostile strike.
                new()
                {
                    Feat = FeatType.EscapeArtist1,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = VibroknifeResref,
                    ExpectedTargetStatusEffects = new[] { typeof(BlindStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Also grants the activator temporary invisibility (engine effect, not a StatusEffectDefinition class), not asserted.",
                },

                // PathogenStrikeAbilityDefinition - IsQueuedWeaponAbility: weapon-queued ability.
                new()
                {
                    Feat = FeatType.PathogenStrike1,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = VibroknifeResref,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Asserts unconditional queued-hit damage; duration extension still requires pre-existing Venom/Infection.",
                },
                new()
                {
                    Feat = FeatType.PathogenStrike2,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = VibroknifeResref,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.PathogenStrike3,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = VibroknifeResref,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.PathogenStrike4,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = VibroknifeResref,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // ShadowflowStanceAbilityDefinition - self stance path.
                new()
                {
                    Feat = FeatType.ShadowflowStance1,
                    Target = AbilityTargetKind.Self,
                    ExpectedActivatorStatusEffects = new[] { typeof(ShadowflowStanceStatusEffect) },
                    ExpectsRecast = true,
                },

                // VeiledStrikeAbilityDefinition - IsQueuedWeaponAbility: weapon-queued ability.
                new()
                {
                    Feat = FeatType.VeiledStrike1,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = VibroknifeResref,
                    TargetSetupStatusEffects = new[] { typeof(ExposedStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "The Bible makes Exposed a precondition for bonus damage, not an applied rider. The case pre-applies it and asserts attributed queued damage.",
                },
                new()
                {
                    Feat = FeatType.VeiledStrike2,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = VibroknifeResref,
                    TargetSetupStatusEffects = new[] { typeof(ExposedStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.VeiledStrike3,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = VibroknifeResref,
                    TargetSetupStatusEffects = new[] { typeof(ExposedStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.VeiledStrike4,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = VibroknifeResref,
                    TargetSetupStatusEffects = new[] { typeof(ExposedStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // ViralCascadeAbilityDefinition - capstone Casted single-target hostile strike.
                new()
                {
                    Feat = FeatType.ViralCascade1,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = VibroknifeResref,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Bonus damage/status consumption requires pre-existing Venom/Infection stacks on the target; not present on a fresh spawn.",
                },

                // VirulentBladeAbilityDefinition - IsQueuedWeaponAbility: weapon-queued ability.
                new()
                {
                    Feat = FeatType.VirulentBlade1,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = VibroknifeResref,
                    ExpectedTargetStatusEffects = new[] { typeof(VenomStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "The shared executor lands the queued hit and asserts both Venom and attributed ability damage.",
                },
                new()
                {
                    Feat = FeatType.VirulentBlade2,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = VibroknifeResref,
                    ExpectedTargetStatusEffects = new[] { typeof(VenomStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.VirulentBlade3,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = VibroknifeResref,
                    ExpectedTargetStatusEffects = new[] { typeof(VenomStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // VolatileCompoundAbilityDefinition - Casted single-target hostile strike.
                new()
                {
                    Feat = FeatType.VolatileCompound1,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = VibroknifeResref,
                    ExpectedTargetStatusEffects = new[] { typeof(PoisonResistancePenaltyStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.VolatileCompound2,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = VibroknifeResref,
                    ExpectedTargetStatusEffects = new[] { typeof(PoisonResistancePenaltyStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
            };
        }
    }
}
