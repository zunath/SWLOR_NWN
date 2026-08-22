using System.Collections.Generic;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions.AbilityBehaviors
{
    /// <summary>
    /// Covers the first half (alphabetically Alpha Rhythm - Guarding Roar) of the Beastmaster
    /// tree's registered feats. The tree is split across two files/classes because it exceeds
    /// ~60 cases; see BeastmasterAbilityBehaviors2 for the remainder.
    ///
    /// Beast-activation findings (see engine test task report for the full write-up):
    /// - Most Beastmaster feats (Bite, Claw, Assault, etc.) are granted directly to the BEAST
    ///   creature and carry no player/beast-identity gate at all, so the harness's plain spawned
    ///   nw_rat001 can activate them exactly like any other creature-granted ability.
    /// - A minority (CallBeast, GuardingBond, PredatoryBond here; SoothePet/Tame/ReviveBeast/
    ///   Reward in part 2) have CustomValidation that requires GetIsPC(activator) - these are
    ///   PLAYER abilities that operate on the player's associate beast via
    ///   GetAssociate(AssociateType.Henchman, activator) + BeastMastery.IsPlayerBeast, which in
    ///   turn require a live Player/Beast DB record (dbPlayer.ActiveBeastId). The harness has no
    ///   way to spawn a controllable PC, so these are unreachable here; no local variable (e.g.
    ///   BEAST_TYPE) can substitute because the outer GetIsPC(activator) check on the ACTIVATOR
    ///   (the would-be player, not the beast) fails first.
    /// - BeastMastery.IsPlayerBeast reads a "BEAST_TYPE" local plus GetMaster() being a valid PC;
    ///   Perk.GetPerkLevel falls back to the generic-creature branch (PERK_LEVEL_{id} local, or
    ///   the perk's max level if unset) for any creature that isn't a PC/droid/player-beast, so a
    ///   plain spawned NPC automatically gets max perk level for every Beastmaster perk without
    ///   needing any locals set.
    /// </summary>
    public class BeastmasterAbilityBehaviors : IAbilityBehaviorSource
    {
        [EngineTest("Beastmaster ability behaviors", Category = "AbilityBehavior", TimeoutSeconds = 1800f)]
        public static async Task Run(EngineTestContext ctx)
        {
            await AbilityBehaviorExecutor.RunAsync(ctx, new BeastmasterAbilityBehaviors().BuildCases());
        }

        public List<AbilityBehaviorCase> BuildCases()
        {
            return new List<AbilityBehaviorCase>
            {
                // AlphaRhythmAbilityDefinition - no CustomValidation; unconditional self status,
                // plus an unasserted rider on the beast's master (none present here).
                new()
                {
                    Feat = FeatType.AlphaRhythm1,
                    Target = AbilityTargetKind.Self,
                    ExpectedActivatorStatusEffects = new[] { typeof(AlphaRhythm1BeastStatusEffect) },
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Also applies AlphaRhythm1StatusEffect to GetMaster(activator) when valid; not asserted since the spawned actor has no master.",
                },

                // AngerAbilityDefinition - hostile taunt; enmity/temp-HP riders aren't tracked
                // status effects, so only cost/recast are asserted.
                new()
                {
                    Feat = FeatType.Anger1,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    OutcomeAssertionWaiverReason = "The impact only changes the private enmity table and plays a VFX; the harness has no read-only enmity observation seam.",
                    Notes = "ApplyGoad only modifies enmity + plays a VFX; no trackable status effect or damage.",
                },
                new()
                {
                    Feat = FeatType.Anger2,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectsActivatorTemporaryHP = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Asserts the rank-II raw EffectTemporaryHitpoints grant; its goad remains enmity-only.",
                },

                // ApexBiteAbilityDefinition - hostile direct damage, no status.
                new()
                {
                    Feat = FeatType.ApexBite1,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // AssaultAbilityDefinition - hostile damage + unconditional self status per tier.
                new()
                {
                    Feat = FeatType.Assault1,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedActivatorStatusEffects = new[] { typeof(Assault1StatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.Assault2,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedActivatorStatusEffects = new[] { typeof(Assault2StatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.Assault3,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedActivatorStatusEffects = new[] { typeof(Assault3StatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // BiteAbilityDefinition - hostile direct damage, no status.
                new()
                {
                    Feat = FeatType.Bite1,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.Bite2,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.Bite3,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // BolsterAttackAbilityDefinition - self buff, unconditional status.
                new()
                {
                    Feat = FeatType.BolsterAttack1,
                    Target = AbilityTargetKind.Self,
                    ExpectedActivatorStatusEffects = new[] { typeof(BolsterAttack1StatusEffect) },
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.BolsterAttack2,
                    Target = AbilityTargetKind.Self,
                    ExpectedActivatorStatusEffects = new[] { typeof(BolsterAttack2StatusEffect) },
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.BolsterAttack3,
                    Target = AbilityTargetKind.Self,
                    ExpectedActivatorStatusEffects = new[] { typeof(BolsterAttack3StatusEffect) },
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // CallBeastAbilityDefinition - PLAYER-only: CustomValidation requires GetIsPC(activator)
                // and reads dbPlayer.ActiveBeastId directly. See class notes above.
                new()
                {
                    Feat = FeatType.CallBeast,
                    Target = AbilityTargetKind.Self,
                    SkipReason = "CustomValidation requires GetIsPC(activator) and a Player DB record with ActiveBeastId set; the harness spawns a plain NPC, not a controllable PC.",
                },

                // ClawAbilityDefinition - hostile damage + unconditional BleedStatusEffect.
                new()
                {
                    Feat = FeatType.Claw1,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(BleedStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.Claw2,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(BleedStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.Claw3,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(BleedStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // CoordinatedStrikeAbilityDefinition - hostile direct damage, no status; the
                // "recent master damage" bonus is a conditional percent adjustment, not asserted.
                new()
                {
                    Feat = FeatType.CoordinatedStrike1,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.CoordinatedStrike2,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // CrushingSlamAbilityDefinition - self-centered sphere (always centerOnActivator);
                // unconditional damage + DazedStatusEffect on hostiles caught in the shape.
                new()
                {
                    Feat = FeatType.CrushingSlam1,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(DazedStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.CrushingSlam2,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(DazedStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.CrushingSlam3,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(DazedStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // DistractingFeintAbilityDefinition - IsWeaponAbility (queued), no CustomValidation
                // (no equipped-weapon requirement). Extra enmity is unasserted; cost/recast apply
                // at queue time per the executor's weapon-ability branch.
                new()
                {
                    Feat = FeatType.DistractingFeint1,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(DistractingFeint1StatusEffect) },
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Weapon-queued; the shared executor lands the consuming hit and asserts its status rider.",
                },
                new()
                {
                    Feat = FeatType.DistractingFeint2,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(DistractingFeint2StatusEffect) },
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.DistractingFeint3,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(DistractingFeint3StatusEffect) },
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // EvasiveChallengeAbilityDefinition - tier 1 is single-target+hostile (RequiresTarget);
                // tier 2 is a self-centered area with no RequiresTarget. Both apply an unconditional
                // self status; goad/enmity on hostiles is not asserted.
                new()
                {
                    Feat = FeatType.EvasiveChallenge1,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedActivatorStatusEffects = new[] { typeof(EvasiveChallenge1SelfStatusEffect) },
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.EvasiveChallenge2,
                    Target = AbilityTargetKind.Self,
                    ExpectedActivatorStatusEffects = new[] { typeof(EvasiveChallenge2SelfStatusEffect) },
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Area centered on the activator regardless of target; goad on nearby hostiles is not asserted.",
                },

                // EvasiveManeuverAbilityDefinition - self buff, unconditional status.
                new()
                {
                    Feat = FeatType.EvasiveManeuver1,
                    Target = AbilityTargetKind.Self,
                    ExpectedActivatorStatusEffects = new[] { typeof(EvasiveManeuver1StatusEffect) },
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.EvasiveManeuver2,
                    Target = AbilityTargetKind.Self,
                    ExpectedActivatorStatusEffects = new[] { typeof(EvasiveManeuver2StatusEffect) },
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.EvasiveManeuver3,
                    Target = AbilityTargetKind.Self,
                    ExpectedActivatorStatusEffects = new[] { typeof(EvasiveManeuver3StatusEffect) },
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // ExecutePreyAbilityDefinition - hostile direct damage; the low-HP bonus is
                // conditional (target spawns at full HP), so only the base hit is asserted.
                new()
                {
                    Feat = FeatType.ExecutePrey1,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "damagePercentAdjustment's low-HP bonus doesn't trigger against a full-HP target; base damage still lands.",
                },

                // ExposePreyAbilityDefinition - hostile damage + unconditional ExposedStatusEffect.
                new()
                {
                    Feat = FeatType.ExposePrey1,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(ExposedStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // ForceBondedBeastAbilityDefinition - no CustomValidation; unconditional self status
                // (and an unasserted rider on the beast's master, none present here).
                new()
                {
                    Feat = FeatType.ForceBondedBeast1,
                    Target = AbilityTargetKind.Self,
                    ExpectedActivatorStatusEffects = new[] { typeof(ForceBondedBeast1StatusEffect) },
                    ExpectsFPCost = true,
                    ExpectsRecast = true,
                    Notes = "Also applies to GetMaster(activator) when valid; not asserted since the spawned actor has no master.",
                },

                // ForceTouchAbilityDefinition - hostile direct Force damage, no status.
                new()
                {
                    Feat = FeatType.ForceTouch1,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectsTargetDamage = true,
                    ExpectsFPCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.ForceTouch2,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectsTargetDamage = true,
                    ExpectsFPCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.ForceTouch3,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectsTargetDamage = true,
                    ExpectsFPCost = true,
                    ExpectsRecast = true,
                },

                // GuardedBiteAbilityDefinition - hostile damage + unconditional self status.
                new()
                {
                    Feat = FeatType.GuardedBite1,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedActivatorStatusEffects = new[] { typeof(GuardedBite1SelfStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.GuardedBite2,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedActivatorStatusEffects = new[] { typeof(GuardedBite2SelfStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.GuardedBite3,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedActivatorStatusEffects = new[] { typeof(GuardedBite3SelfStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // GuardingBondAbilityDefinition - PLAYER-only toggle: ValidateBeast requires
                // GetIsPC(activator) (fresh actor has no status effect yet, so the toggle-off
                // early-return doesn't apply) then BeastMastery.IsPlayerBeast on the associate.
                new()
                {
                    Feat = FeatType.GuardingBond,
                    Target = AbilityTargetKind.Self,
                    SkipReason = "ValidateBeast requires GetIsPC(activator) plus a live player-beast associate (BeastMastery.IsPlayerBeast); unreachable for a plain spawned NPC.",
                },

                // GuardingRoarAbilityDefinition - self-centered area (always centerOnActivator);
                // unconditional self status. Goad on nearby hostiles is not asserted.
                new()
                {
                    Feat = FeatType.GuardingRoar1,
                    Target = AbilityTargetKind.Self,
                    ExpectedActivatorStatusEffects = new[] { typeof(GuardingRoar1SelfStatusEffect) },
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.GuardingRoar2,
                    Target = AbilityTargetKind.Self,
                    ExpectedActivatorStatusEffects = new[] { typeof(GuardingRoar2SelfStatusEffect) },
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.GuardingRoar3,
                    Target = AbilityTargetKind.Self,
                    ExpectedActivatorStatusEffects = new[] { typeof(GuardingRoar3SelfStatusEffect) },
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
            };
        }
    }
}
