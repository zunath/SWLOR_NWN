using System.Collections.Generic;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions.AbilityBehaviors
{
    public class VibrobladeAbilityBehaviors : IAbilityBehaviorSource
    {
        private const string VibrobladeResref = "nw_wswls001";

        [EngineTest("Vibroblade ability behaviors", Category = "AbilityBehavior", TimeoutSeconds = 1800f)]
        public static async Task Run(EngineTestContext ctx)
        {
            await AbilityBehaviorExecutor.RunAsync(ctx, new VibrobladeAbilityBehaviors().BuildCases());
        }

        public List<AbilityBehaviorCase> BuildCases()
        {
            return new List<AbilityBehaviorCase>
            {
                // HackingBlade was legacy pre-combat-upgrade content: the _22 migration refunds
                // its perk from players and the Bible's Vibroblade tree replaced it (Rending
                // Strike is the queued weapon ability). Its leftover ability definition
                // registered feats no perk granted and crashed NPC perk-level lookups - found
                // by this suite and deleted; no cases exist for it by design.

                // BerserkerStanceAbilityDefinition - ConfigureToggle: self stance.
                new()
                {
                    Feat = FeatType.BerserkerStance1,
                    Target = AbilityTargetKind.Self,
                    ExpectedActivatorStatusEffects = new[] { typeof(BerserkerStanceStatusEffect) },
                    ExpectsRecast = true,
                },

                // CoveringStrikeAbilityDefinition - casted sphere AoE centered on self.
                new()
                {
                    Feat = FeatType.CoveringStrike1,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = VibrobladeResref,
                    ExpectedTargetStatusEffects = new[] { typeof(CoveringStrikeStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.CoveringStrike2,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = VibrobladeResref,
                    ExpectedTargetStatusEffects = new[] { typeof(CoveringStrikeStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.CoveringStrike3,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = VibrobladeResref,
                    ExpectedTargetStatusEffects = new[] { typeof(CoveringStrikeStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // DefensiveStanceAbilityDefinition - ConfigureToggle: self stance.
                new()
                {
                    Feat = FeatType.DefensiveStance1,
                    Target = AbilityTargetKind.Self,
                    ExpectedActivatorStatusEffects = new[] { typeof(DefensiveStanceStatusEffect) },
                    ExpectsRecast = true,
                },

                // InvincibleAbilityDefinition - capstone self buff (ConfigureSelfStatus).
                new()
                {
                    Feat = FeatType.Invincible1,
                    Target = AbilityTargetKind.Self,
                    ExpectedActivatorStatusEffects = new[] { typeof(InvincibleStatusEffect) },
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Capstone. Also grants temporary Knockdown/Dazed immunity (engine effect, not asserted).",
                },

                // RendingStrikeAbilityDefinition - casted single-target hostile strike.
                new()
                {
                    Feat = FeatType.RendingStrike1,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = VibrobladeResref,
                    ExpectedTargetStatusEffects = new[] { typeof(ExposedStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.RendingStrike2,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = VibrobladeResref,
                    ExpectedTargetStatusEffects = new[] { typeof(ExposedStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // RiotBladeAbilityDefinition - ConfigureWeapon: queued weapon ability, hostile.
                new()
                {
                    Feat = FeatType.RiotBlade1,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = VibrobladeResref,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.RiotBlade2,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = VibrobladeResref,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.RiotBlade3,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = VibrobladeResref,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.RiotBlade4,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = VibrobladeResref,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // SavageCleaveAbilityDefinition - casted sphere AoE centered on self.
                new()
                {
                    Feat = FeatType.SavageCleave1,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = VibrobladeResref,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.SavageCleave2,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = VibrobladeResref,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    ImpactRefundsCosts = true,
                    Notes = "RestoreSecondaryTargetStamina refunds 2 STM per secondary target hit (ImpactedTargetCount - 1), so only the net stamina dip is observable.",
                },
                new()
                {
                    Feat = FeatType.SavageCleave3,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = VibrobladeResref,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    ImpactRefundsCosts = true,
                    Notes = "RestoreSecondaryTargetStamina refunds 2 STM per secondary target hit (ImpactedTargetCount - 1), so only the net stamina dip is observable.",
                },

                // ShieldBashAbilityDefinition - manually-built weapon ability, hostile.
                new()
                {
                    Feat = FeatType.ShieldBash1,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = VibrobladeResref,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Weapon-skill impact damage plus the physical-defense rider must produce attributed queued-ability damage.",
                },
                new()
                {
                    Feat = FeatType.ShieldBash2,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = VibrobladeResref,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.ShieldBash3,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = VibrobladeResref,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.ShieldBash4,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = VibrobladeResref,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // ShieldWallAbilityDefinition - channeled party buff (ConfigurePartyStatus, includeSelf).
                new()
                {
                    Feat = FeatType.ShieldWall1,
                    Target = AbilityTargetKind.Self,
                    ExpectedActivatorStatusEffects = new[] { typeof(ShieldWallStatusEffect) },
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Channeled: impact/costs/recast apply at channel start, not on completion.",
                },
                new()
                {
                    Feat = FeatType.ShieldWall2,
                    Target = AbilityTargetKind.Self,
                    ExpectedActivatorStatusEffects = new[] { typeof(ShieldWallStatusEffect) },
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Channeled: impact/costs/recast apply at channel start, not on completion.",
                },
            };
        }
    }
}
