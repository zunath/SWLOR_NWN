using System.Collections.Generic;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions.AbilityBehaviors
{
    /// <summary>
    /// Covers the second half (alphabetically Hasten - Warding Howl) of the Beastmaster tree's
    /// registered feats. See BeastmasterAbilityBehaviors for the first half and the shared
    /// beast-activation findings.
    /// </summary>
    public class BeastmasterAbilityBehaviors2 : IAbilityBehaviorSource
    {
        [EngineTest("Beastmaster ability behaviors (part 2)", Category = "AbilityBehavior", TimeoutSeconds = 1800f)]
        public static async Task Run(EngineTestContext ctx)
        {
            await AbilityBehaviorExecutor.RunAsync(ctx, new BeastmasterAbilityBehaviors2().BuildCases());
        }

        public List<AbilityBehaviorCase> BuildCases()
        {
            return new List<AbilityBehaviorCase>
            {
                // HastenAbilityDefinition - self buff, unconditional status.
                new()
                {
                    Feat = FeatType.Hasten1,
                    Target = AbilityTargetKind.Self,
                    ExpectedActivatorStatusEffects = new[] { typeof(Hasten1StatusEffect) },
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.Hasten2,
                    Target = AbilityTargetKind.Self,
                    ExpectedActivatorStatusEffects = new[] { typeof(Hasten2StatusEffect) },
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // IceBreathAbilityDefinition - telegraphed cone; no RequiresTarget, but a valid
                // hostile target aims the cone at it (cone origin is always the activator; the
                // target only sets facing). Damage + unconditional status per tier.
                new()
                {
                    Feat = FeatType.IceBreath1,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(HamstringStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Cone shape always originates at the activator; the harness's hostile target only supplies the aim direction.",
                },
                new()
                {
                    Feat = FeatType.IceBreath2,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(HamstringStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.IceBreath3,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(ImmobilizedStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // InnervateAbilityDefinition - friendly single-target heal (ValidateFriendlyTarget,
                // allowSelf true); HealPercent applies heal via GetFriendlyTargets(activator, target,
                // false), which resolves to the activator on a self-cast, via a raw EffectHeal.
                new()
                {
                    Feat = FeatType.Innervate1,
                    Target = AbilityTargetKind.Self,
                    ExpectsActivatorHealing = true,
                    ExpectsFPCost = true,
                    ExpectsRecast = true,
                    Notes = "Heals via HealPercent (raw EffectHeal) on the resolved friendly target, which is the activator itself on a self-cast.",
                },
                new()
                {
                    Feat = FeatType.Innervate2,
                    Target = AbilityTargetKind.Self,
                    ExpectsActivatorHealing = true,
                    ExpectsFPCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.Innervate3,
                    Target = AbilityTargetKind.Self,
                    ExpectsActivatorHealing = true,
                    ExpectsFPCost = true,
                    ExpectsRecast = true,
                },

                // InterceptAbilityDefinition - friendly area with no RequiresTarget; targets the
                // beast's master, falling back to the activator when no master exists.
                new()
                {
                    Feat = FeatType.Intercept1,
                    Target = AbilityTargetKind.Self,
                    ExpectedActivatorStatusEffects = new[] { typeof(Intercept1StatusEffect) },
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "GetBeastMasterTargets falls back to the activator itself when GetMaster is invalid.",
                },
                new()
                {
                    Feat = FeatType.Intercept2,
                    Target = AbilityTargetKind.Self,
                    ExpectedActivatorStatusEffects = new[] { typeof(Intercept2StatusEffect) },
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // IronHideAbilityDefinition - self buff, unconditional status.
                new()
                {
                    Feat = FeatType.IronHide1,
                    Target = AbilityTargetKind.Self,
                    ExpectedActivatorStatusEffects = new[] { typeof(IronHide1StatusEffect) },
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.IronHide2,
                    Target = AbilityTargetKind.Self,
                    ExpectedActivatorStatusEffects = new[] { typeof(IronHide2StatusEffect) },
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.IronHide3,
                    Target = AbilityTargetKind.Self,
                    ExpectedActivatorStatusEffects = new[] { typeof(IronHide3StatusEffect) },
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // PoisonBreathAbilityDefinition - telegraphed cone, same shape/target notes as
                // IceBreath. Damage + unconditional PoisonStatusEffect per tier.
                new()
                {
                    Feat = FeatType.PoisonBreath1,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(PoisonStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.PoisonBreath2,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(PoisonStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.PoisonBreath3,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(PoisonStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // PounceAbilityDefinition - hostile gap-closer + direct damage, no status.
                new()
                {
                    Feat = FeatType.Pounce1,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Also jumps the activator to the target and clears the target's action queue; not asserted.",
                },
                new()
                {
                    Feat = FeatType.Pounce2,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // PredatoryBondAbilityDefinition - PLAYER-only toggle: ValidateBeast requires
                // GetIsPC(activator) (fresh actor has no status effect yet, so the toggle-off
                // early-return doesn't apply) then BeastMastery.IsPlayerBeast on the associate.
                new()
                {
                    Feat = FeatType.PredatoryBond,
                    Target = AbilityTargetKind.Self,
                    SkipReason = "ValidateBeast requires GetIsPC(activator) plus a live player-beast associate (BeastMastery.IsPlayerBeast); unreachable for a plain spawned NPC.",
                },

                // PrimalOverrunAbilityDefinition - self-centered sphere (always centerOnActivator);
                // unconditional damage to the nearby hostile plus an unconditional self status.
                new()
                {
                    Feat = FeatType.PrimalOverrun1,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedActivatorStatusEffects = new[] { typeof(PrimalOverrun1StatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // PsychicCryAbilityDefinition - self-centered sphere, zero direct damage but an
                // unconditional status on hostiles caught in the shape.
                new()
                {
                    Feat = FeatType.PsychicCry1,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(PsychicCry1StatusEffect) },
                    ExpectsFPCost = true,
                    ExpectsRecast = true,
                    Notes = "Base damage is 0 for this tier; only the status effect is asserted.",
                },
                new()
                {
                    Feat = FeatType.PsychicCry2,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(PsychicCry2StatusEffect) },
                    ExpectsFPCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.PsychicCry3,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(PsychicCry3StatusEffect) },
                    ExpectsFPCost = true,
                    ExpectsRecast = true,
                },

                // RampageAbilityDefinition - self-centered sphere, unconditional damage, no status.
                new()
                {
                    Feat = FeatType.Rampage1,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.Rampage2,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // RampartHideAbilityDefinition - self buff, unconditional status.
                new()
                {
                    Feat = FeatType.RampartHide1,
                    Target = AbilityTargetKind.Self,
                    ExpectedActivatorStatusEffects = new[] { typeof(RampartHide1StatusEffect) },
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // RendingClawAbilityDefinition - hostile damage + unconditional BleedStatusEffect.
                new()
                {
                    Feat = FeatType.RendingClaw1,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(BleedStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.RendingClaw2,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(BleedStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.RendingClaw3,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(BleedStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // RewardAbilityDefinition - PLAYER-only: Validation requires GetIsPC(activator)
                // then a pet treat + a live player-beast associate.
                new()
                {
                    Feat = FeatType.Reward1,
                    Target = AbilityTargetKind.Self,
                    SkipReason = "Validation requires GetIsPC(activator), a pet-treat item, and BeastMastery.IsPlayerBeast on the associate; unreachable for a plain spawned NPC.",
                },
                new()
                {
                    Feat = FeatType.Reward2,
                    Target = AbilityTargetKind.Self,
                    SkipReason = "Same PLAYER-only gate as Reward1.",
                },
                new()
                {
                    Feat = FeatType.Reward3,
                    Target = AbilityTargetKind.Self,
                    SkipReason = "Same PLAYER-only gate as Reward1.",
                },

                // ReviveBeastAbilityDefinition - PLAYER-only: Validation requires GetIsPC(activator),
                // no active companion, and a dead Beast DB record tied to the player.
                new()
                {
                    Feat = FeatType.ReviveBeast1,
                    Target = AbilityTargetKind.Self,
                    SkipReason = "Validation requires GetIsPC(activator) and a Player DB record with a dead ActiveBeastId; unreachable for a plain spawned NPC.",
                },
                new()
                {
                    Feat = FeatType.ReviveBeast2,
                    Target = AbilityTargetKind.Self,
                    SkipReason = "Same PLAYER-only gate as ReviveBeast1.",
                },
                new()
                {
                    Feat = FeatType.ReviveBeast3,
                    Target = AbilityTargetKind.Self,
                    SkipReason = "Same PLAYER-only gate as ReviveBeast1.",
                },

                // SoothePetAbilityDefinition - PLAYER-only: CustomValidation requires
                // GetIsPC(activator) then BeastMastery.IsPlayerBeast on the associate.
                new()
                {
                    Feat = FeatType.SoothePet,
                    Target = AbilityTargetKind.Self,
                    SkipReason = "CustomValidation requires GetIsPC(activator) plus a live player-beast associate (BeastMastery.IsPlayerBeast); unreachable for a plain spawned NPC.",
                },

                // TameAbilityDefinition - PLAYER-only: CustomValidation requires GetIsPC(activator),
                // no existing active beast, and a valid tameable NPC target (BeastType != Invalid).
                new()
                {
                    Feat = FeatType.Tame,
                    Target = AbilityTargetKind.Self,
                    SkipReason = "CustomValidation requires GetIsPC(activator) and a Player DB record; unreachable for a plain spawned NPC. It also requires a distinct tameable target with a non-Invalid BeastType local, which the harness's shared caster/target creature doesn't provide.",
                },

                // UnbreakableBeastAbilityDefinition - no CustomValidation; unconditional self status.
                new()
                {
                    Feat = FeatType.UnbreakableBeast1,
                    Target = AbilityTargetKind.Self,
                    ExpectedActivatorStatusEffects = new[] { typeof(UnbreakableBeast1StatusEffect) },
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // UntouchableInstinctAbilityDefinition - self buff, unconditional status.
                new()
                {
                    Feat = FeatType.UntouchableInstinct1,
                    Target = AbilityTargetKind.Self,
                    ExpectedActivatorStatusEffects = new[] { typeof(UntouchableInstinct1StatusEffect) },
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // WardingHowlAbilityDefinition - party-friendly area with no RequiresTarget; falls
                // back to applying the status to the activator alone when not in a party.
                new()
                {
                    Feat = FeatType.WardingHowl1,
                    Target = AbilityTargetKind.Self,
                    ExpectedActivatorStatusEffects = new[] { typeof(WardingHowl1StatusEffect) },
                    ExpectsFPCost = true,
                    ExpectsRecast = true,
                    Notes = "GetFriendlyTargets(affectsParty:true) falls back to the activator alone outside a party.",
                },
                new()
                {
                    Feat = FeatType.WardingHowl2,
                    Target = AbilityTargetKind.Self,
                    ExpectedActivatorStatusEffects = new[] { typeof(WardingHowl2StatusEffect) },
                    ExpectsFPCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.WardingHowl3,
                    Target = AbilityTargetKind.Self,
                    ExpectedActivatorStatusEffects = new[] { typeof(WardingHowl3StatusEffect) },
                    ExpectsFPCost = true,
                    ExpectsRecast = true,
                },
            };
        }
    }
}
