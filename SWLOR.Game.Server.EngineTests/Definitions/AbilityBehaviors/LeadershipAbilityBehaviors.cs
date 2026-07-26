using System.Collections.Generic;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions.AbilityBehaviors
{
    public class LeadershipAbilityBehaviors : IAbilityBehaviorSource
    {
        [EngineTest("Leadership ability behaviors", Category = "AbilityBehavior", TimeoutSeconds = 1800f)]
        public static async Task Run(EngineTestContext ctx)
        {
            await AbilityBehaviorExecutor.RunAsync(ctx, new LeadershipAbilityBehaviors().BuildCases());
        }

        public List<AbilityBehaviorCase> BuildCases()
        {
            return new List<AbilityBehaviorCase>
            {
                // BreakMoraleAbilityDefinition - hostile area debuff, unconditional Flash (+
                // Weakened at tier 2) applied to hostiles within Leadership command radius.
                new()
                {
                    Feat = FeatType.BreakMorale1,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(FlashStatusEffect) },
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.BreakMorale2,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(FlashStatusEffect), typeof(WeakenedStatusEffect) },
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // ChargeOrderAbilityDefinition - self toggle aura gated on
                // StatType.ChargeOrderAuraLevel, a perk-investment stat bonus
                // (Perk.GetStatBonus -> GetStatBonusPerkLevel) that reads the NPC's
                // "PERK_LEVEL_{perk}" local directly (no max-level fallback for NPCs, unlike
                // GetPerkLevel), so the perk level - and thus the aura - is pinned explicitly here.
                new()
                {
                    Feat = FeatType.ChargeOrder1,
                    Target = AbilityTargetKind.Self,
                    ExpectedActivatorStatusEffects = new[] { typeof(ChargeOrder1StatusEffect) },
                    ExpectsRecast = true,
                    SetupNPCPerkLevels = new() { [PerkType.ChargeOrder] = 1 },
                    Notes = "ToggleVanguardCommandAura is gated on StatType.ChargeOrderAuraLevel, a perk-investment bonus that GetStatBonusPerkLevel reads from the NPC's PERK_LEVEL_ local with no max-level fallback (unlike GetPerkLevel), so PerkType.ChargeOrder is pinned to level 1 to make the aura branch deterministic. No cost is declared.",
                },
                new()
                {
                    Feat = FeatType.ChargeOrder2,
                    Target = AbilityTargetKind.Self,
                    ExpectedActivatorStatusEffects = new[] { typeof(ChargeOrder2StatusEffect) },
                    ExpectsRecast = true,
                    SetupNPCPerkLevels = new() { [PerkType.ChargeOrder] = 2 },
                },

                // CleanseOrderAbilityDefinition - tier 1 only cleanses + grants raw temp HP (no
                // tracked status); tier 2 additionally applies an unconditional status.
                new()
                {
                    Feat = FeatType.CleanseOrder1,
                    Target = AbilityTargetKind.Self,
                    TargetSetupStatusEffects = new[] { typeof(PoisonStatusEffect) },
                    ExpectedRemovedTargetStatusEffects = new[] { typeof(PoisonStatusEffect) },
                    ExpectsActivatorTemporaryHP = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Asserts both unconditional outcomes: one TreatmentKit2 cleanse and a newly added raw temporary-HP effect.",
                },
                new()
                {
                    Feat = FeatType.CleanseOrder2,
                    Target = AbilityTargetKind.Self,
                    TargetSetupStatusEffects = new[] { typeof(PoisonStatusEffect) },
                    ExpectedRemovedTargetStatusEffects = new[] { typeof(PoisonStatusEffect) },
                    ExpectedActivatorStatusEffects = new[] { typeof(CleanseOrder2StatusEffect) },
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // CoordinatedFocusAbilityDefinition - same stat-gated toggle-aura pattern as
                // ChargeOrder; perk level is pinned per tier so the correct status effect fires.
                new()
                {
                    Feat = FeatType.CoordinatedFocus1,
                    Target = AbilityTargetKind.Self,
                    ExpectedActivatorStatusEffects = new[] { typeof(CoordinatedFocus1StatusEffect) },
                    ExpectsRecast = true,
                    SetupNPCPerkLevels = new() { [PerkType.CoordinatedFocus] = 1 },
                    Notes = "Toggle aura gated on StatType.CoordinatedFocusAuraLevel (0 for a bare NPC without the pin above). No cost is declared.",
                },
                new()
                {
                    Feat = FeatType.CoordinatedFocus2,
                    Target = AbilityTargetKind.Self,
                    ExpectedActivatorStatusEffects = new[] { typeof(CoordinatedFocus2StatusEffect) },
                    ExpectsRecast = true,
                    SetupNPCPerkLevels = new() { [PerkType.CoordinatedFocus] = 2 },
                },
                new()
                {
                    Feat = FeatType.CoordinatedFocus3,
                    Target = AbilityTargetKind.Self,
                    ExpectedActivatorStatusEffects = new[] { typeof(CoordinatedFocus3StatusEffect) },
                    ExpectsRecast = true,
                    SetupNPCPerkLevels = new() { [PerkType.CoordinatedFocus] = 3 },
                },

                // DecisiveCommandAbilityDefinition - capstone self/party buff, unconditional status.
                new()
                {
                    Feat = FeatType.DecisiveCommand1,
                    Target = AbilityTargetKind.Self,
                    ExpectedActivatorStatusEffects = new[] { typeof(DecisiveCommand1StatusEffect) },
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // FieldRecoveryAbilityDefinition - same stat-gated toggle-aura pattern; no cost
                // declared. Perk level is pinned per tier so the correct status effect fires.
                new()
                {
                    Feat = FeatType.FieldRecovery1,
                    Target = AbilityTargetKind.Self,
                    ExpectedActivatorStatusEffects = new[] { typeof(FieldRecovery1StatusEffect) },
                    ExpectsRecast = true,
                    SetupNPCPerkLevels = new() { [PerkType.FieldRecovery] = 1 },
                    Notes = "Toggle aura gated on StatType.FieldRecoveryAuraLevel (0 for a bare NPC without the pin above). No cost is declared.",
                },
                new()
                {
                    Feat = FeatType.FieldRecovery2,
                    Target = AbilityTargetKind.Self,
                    ExpectedActivatorStatusEffects = new[] { typeof(FieldRecovery2StatusEffect) },
                    ExpectsRecast = true,
                    SetupNPCPerkLevels = new() { [PerkType.FieldRecovery] = 2 },
                },

                // HoldTheLineAbilityDefinition - capstone self/party buff, unconditional status.
                new()
                {
                    Feat = FeatType.HoldTheLine1,
                    Target = AbilityTargetKind.Self,
                    ExpectedActivatorStatusEffects = new[] { typeof(HoldTheLine1StatusEffect) },
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // PressTheAttackAbilityDefinition - self/party buff, unconditional status.
                new()
                {
                    Feat = FeatType.PressTheAttack1,
                    Target = AbilityTargetKind.Self,
                    ExpectedActivatorStatusEffects = new[] { typeof(PressTheAttack1StatusEffect) },
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.PressTheAttack2,
                    Target = AbilityTargetKind.Self,
                    ExpectedActivatorStatusEffects = new[] { typeof(PressTheAttack2StatusEffect) },
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.PressTheAttack3,
                    Target = AbilityTargetKind.Self,
                    ExpectedActivatorStatusEffects = new[] { typeof(PressTheAttack3StatusEffect) },
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // RallyingStandardAbilityDefinition - same stat-gated toggle-aura pattern; no cost
                // declared. Perk level is pinned per tier so the correct status effect fires.
                new()
                {
                    Feat = FeatType.RallyingStandard1,
                    Target = AbilityTargetKind.Self,
                    ExpectedActivatorStatusEffects = new[] { typeof(RallyingStandard1StatusEffect) },
                    ExpectsRecast = true,
                    SetupNPCPerkLevels = new() { [PerkType.RallyingStandard] = 1 },
                    Notes = "Toggle aura gated on StatType.RallyingStandardAuraLevel (0 for a bare NPC without the pin above). No cost is declared.",
                },
                new()
                {
                    Feat = FeatType.RallyingStandard2,
                    Target = AbilityTargetKind.Self,
                    ExpectedActivatorStatusEffects = new[] { typeof(RallyingStandard2StatusEffect) },
                    ExpectsRecast = true,
                    SetupNPCPerkLevels = new() { [PerkType.RallyingStandard] = 2 },
                },

                // RousingShoutAbilityDefinition - self/ally target; grants raw temp HP always, but
                // the low-HP status rider only applies when the target is at or below 35% HP,
                // which a freshly spawned full-health test creature never is.
                new()
                {
                    Feat = FeatType.RousingShout1,
                    Target = AbilityTargetKind.Self,
                    ExpectsActivatorTemporaryHP = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Grants raw temporary HP unconditionally; the low-HP status rider requires the target at or below 35% HP, which a fresh full-health spawn never is. TriageProtocol/BolsterResolve riders are also stat-gated to 0.",
                },
                new()
                {
                    Feat = FeatType.RousingShout2,
                    Target = AbilityTargetKind.Self,
                    ExpectsActivatorTemporaryHP = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.RousingShout3,
                    Target = AbilityTargetKind.Self,
                    ExpectsActivatorTemporaryHP = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // SteadyFormationAbilityDefinition - same stat-gated toggle-aura pattern; no cost
                // declared. Perk level is pinned per tier so the correct status effect fires.
                new()
                {
                    Feat = FeatType.SteadyFormation1,
                    Target = AbilityTargetKind.Self,
                    ExpectedActivatorStatusEffects = new[] { typeof(SteadyFormation1StatusEffect) },
                    ExpectsRecast = true,
                    SetupNPCPerkLevels = new() { [PerkType.SteadyFormation] = 1 },
                    Notes = "Toggle aura gated on StatType.SteadyFormationAuraLevel (0 for a bare NPC without the pin above). No cost is declared.",
                },
                new()
                {
                    Feat = FeatType.SteadyFormation2,
                    Target = AbilityTargetKind.Self,
                    ExpectedActivatorStatusEffects = new[] { typeof(SteadyFormation2StatusEffect) },
                    ExpectsRecast = true,
                    SetupNPCPerkLevels = new() { [PerkType.SteadyFormation] = 2 },
                },

                // WatchfulPresenceAbilityDefinition - same stat-gated toggle-aura pattern; no cost
                // declared. Perk level is pinned per tier so the correct status effect fires.
                new()
                {
                    Feat = FeatType.WatchfulPresence1,
                    Target = AbilityTargetKind.Self,
                    ExpectedActivatorStatusEffects = new[] { typeof(WatchfulPresence1StatusEffect) },
                    ExpectsRecast = true,
                    SetupNPCPerkLevels = new() { [PerkType.WatchfulPresence] = 1 },
                    Notes = "Toggle aura gated on StatType.WatchfulPresenceAuraLevel (0 for a bare NPC without the pin above). No cost is declared.",
                },
                new()
                {
                    Feat = FeatType.WatchfulPresence2,
                    Target = AbilityTargetKind.Self,
                    ExpectedActivatorStatusEffects = new[] { typeof(WatchfulPresence2StatusEffect) },
                    ExpectsRecast = true,
                    SetupNPCPerkLevels = new() { [PerkType.WatchfulPresence] = 2 },
                },
                new()
                {
                    Feat = FeatType.WatchfulPresence3,
                    Target = AbilityTargetKind.Self,
                    ExpectedActivatorStatusEffects = new[] { typeof(WatchfulPresence3StatusEffect) },
                    ExpectsRecast = true,
                    SetupNPCPerkLevels = new() { [PerkType.WatchfulPresence] = 3 },
                },
            };
        }
    }
}
