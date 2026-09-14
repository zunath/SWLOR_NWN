using System.Collections.Generic;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions.AbilityBehaviors
{
    public class HeavyVibrobladeAbilityBehaviors : IAbilityBehaviorSource
    {
        private const string HeavyVibrobladeResref = "nw_wswgs001";

        [EngineTest("Heavy Vibroblade ability behaviors", Category = "AbilityBehavior", TimeoutSeconds = 1800f)]
        public static async Task Run(EngineTestContext ctx)
        {
            await AbilityBehaviorExecutor.RunAsync(ctx, new HeavyVibrobladeAbilityBehaviors().BuildCases());
        }

        public List<AbilityBehaviorCase> BuildCases()
        {
            return new List<AbilityBehaviorCase>
            {
                // AbsoluteDefenseAbilityDefinition - capstone party buff; solo caster falls back to self.
                new()
                {
                    Feat = FeatType.AbsoluteDefense1,
                    Target = AbilityTargetKind.Self,
                    ExpectedActivatorStatusEffects = new[] { typeof(AbsoluteDefenseStatusEffect) },
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Party.GetAllPartyMembers falls back to [activator] when solo, so the caster receives the status. Also grants temporary Knockdown/Dazed immunity (engine effect, not asserted).",
                },

                // BastionStanceAbilityDefinition - manually-built self stance toggle.
                new()
                {
                    Feat = FeatType.BastionStance1,
                    Target = AbilityTargetKind.Self,
                    ExpectedActivatorStatusEffects = new[] { typeof(BastionStanceStatusEffect) },
                    ExpectsRecast = true,
                },

                // BlazingSpikesAbilityDefinition - manually-built self stance toggle; no recast group declared.
                new()
                {
                    Feat = FeatType.BlazingSpikes1,
                    Target = AbilityTargetKind.Self,
                    ExpectedActivatorStatusEffects = new[] { typeof(BlazingSpikesStatusEffect) },
                    ExpectsSTMCost = true,
                    Notes = "No HasRecastDelay call in the definition, so no recast group is on cooldown after use.",
                },

                // EarthshatterAbilityDefinition - casted hostile line AoE centered on self.
                new()
                {
                    Feat = FeatType.Earthshatter1,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = HeavyVibrobladeResref,
                    ExpectedTargetStatusEffects = new[] { typeof(ForceDisruptionStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.Earthshatter2,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = HeavyVibrobladeResref,
                    ExpectedTargetStatusEffects = new[] { typeof(ForceDisruptionStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // FlashAbilityDefinition - casted hostile sphere AoE, no damage, disables target.
                new()
                {
                    Feat = FeatType.Flash1,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = HeavyVibrobladeResref,
                    ExpectedTargetStatusEffects = new[] { typeof(FlashStatusEffect) },
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Base damage is 0; this is a disable, not a damage ability.",
                },

                // FortressStrikeAbilityDefinition - weapon ability (IsWeaponAbility), hostile.
                new()
                {
                    Feat = FeatType.FortressStrike1,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = HeavyVibrobladeResref,
                    ExpectedActivatorStatusEffects = new[] { typeof(FortressStrikeStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.FortressStrike2,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = HeavyVibrobladeResref,
                    ExpectedActivatorStatusEffects = new[] { typeof(FortressStrikeStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.FortressStrike3,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = HeavyVibrobladeResref,
                    ExpectedActivatorStatusEffects = new[] { typeof(FortressStrikeStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // RampartAbilityDefinition - self/party buff (ApplyStatusToNearbyParty includeSelf=true).
                new()
                {
                    Feat = FeatType.Rampart1,
                    Target = AbilityTargetKind.Self,
                    ExpectedActivatorStatusEffects = new[] { typeof(RampartStatusEffect) },
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // SacrificialBladeAbilityDefinition - casted single-target hostile strike with HP cost.
                new()
                {
                    Feat = FeatType.SacrificialBlade1,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = HeavyVibrobladeResref,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Spends activator HP (SacrificeHitPoints) in addition to stamina; EssenceHunter rider requires a stat bonus not present on a fresh NPC, so no target status is asserted.",
                },

                // SoulBurstAbilityDefinition - casted hostile cone AoE with HP cost.
                new()
                {
                    Feat = FeatType.SoulBurst1,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = HeavyVibrobladeResref,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Spends activator HP (SacrificeHitPoints) in addition to stamina.",
                },

                // SoulDevourerAbilityDefinition - manually-built self stance toggle; no stamina cost.
                new()
                {
                    Feat = FeatType.SoulDevourer1,
                    Target = AbilityTargetKind.Self,
                    ExpectedActivatorStatusEffects = new[] { typeof(SoulDevourerStatusEffect) },
                    ExpectsRecast = true,
                },

                // SoulStormAbilityDefinition - self/party buff with HP cost, no hostile targeting.
                new()
                {
                    Feat = FeatType.SoulStorm1,
                    Target = AbilityTargetKind.Self,
                    ExpectedActivatorStatusEffects = new[] { typeof(SoulStormStatusEffect) },
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Spends activator HP (SacrificeHitPoints) in addition to stamina.",
                },

                // SoulStrikeAbilityDefinition - weapon ability (IsWeaponAbility), hostile.
                new()
                {
                    Feat = FeatType.SoulStrike1,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = HeavyVibrobladeResref,
                    ExpectsTargetDamage = true,
                    ExpectsActivatorHealing = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.SoulStrike2,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = HeavyVibrobladeResref,
                    ExpectsTargetDamage = true,
                    ExpectsActivatorHealing = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.SoulStrike3,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = HeavyVibrobladeResref,
                    ExpectsTargetDamage = true,
                    ExpectsActivatorHealing = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
            };
        }
    }
}
