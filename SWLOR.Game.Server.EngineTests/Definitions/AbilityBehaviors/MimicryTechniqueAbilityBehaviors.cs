using System.Collections.Generic;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions.AbilityBehaviors
{
    /// <summary>
    /// Declarative engine-test coverage for the damage/control Mimicry technique feats
    /// (SWLOR.Game.Server/Feature/AbilityDefinition/Mimicry). These are learned copies of an NPC's
    /// innate ability, built with InnateAbility.BuildSingleTarget/BuildArea (or an equivalent
    /// hand-written HasImpactAction using Ability.ApplyCombatImpact/ApplyTelegraphedCombatImpact).
    ///
    /// None of these validate a specific equipped weapon (SkillType.Mimicry is not weapon-gated),
    /// so no EquipMainHandResref is set anywhere in this file. All of them cost Stamina, never FP.
    ///
    /// A recurring pattern: several techniques pass baseDamage 0 into the shared combat-impact
    /// pipeline. Combat.IsWeaponSkillType(SkillType.Mimicry) is false, so
    /// Ability.CalculateNPCCombatImpactDamage's "baseDamage &lt;= 0 &amp;&amp; !IsWeaponSkillType"
    /// guard returns 0 unconditionally - these are genuine control-only techniques with no damage,
    /// not an oversight. ExpectsTargetDamage is only set true where baseDamage is actually positive.
    ///
    /// Mimicry-wide activation note (see also MimicryUtilityAbilityBehaviors and
    /// MimicryTraitAbilityBehaviors): a technique's FeatType has no CustomValidation, RequirementItem,
    /// or other gate tied to Service/Mimicry.cs's learned/equipped/slot-budget state - that DB
    /// bookkeeping only gates the player-facing Equip/Unequip UI flow (Mimicry.CanEquip). Once a
    /// creature holds the feat (or, as here, TryUseAbility is invoked directly without ever granting
    /// it - CanUseAbility never checks GetHasFeat), it activates exactly like any other perk-active
    /// ability. NPC perk level also defaults to max (Perk.GetPerkLevel's creature branch), so no
    /// ctx.SetNPCPerkLevel(caster, PerkType.CombatAnalyzer, ...) call is needed for the Level(1)
    /// requirement every technique declares.
    /// </summary>
    public class MimicryTechniqueAbilityBehaviors : IAbilityBehaviorSource
    {
        [EngineTest("Mimicry ability behaviors (techniques)", Category = "AbilityBehavior", TimeoutSeconds = 1800f)]
        public static async Task Run(EngineTestContext ctx)
        {
            await AbilityBehaviorExecutor.RunAsync(ctx, new MimicryTechniqueAbilityBehaviors().BuildCases());
        }

        public List<AbilityBehaviorCase> BuildCases()
        {
            return new List<AbilityBehaviorCase>
            {
                // ArcPulseTechniqueAbilityDefinition - self-centered sphere, 0 base damage (control-only),
                // unconditional Shock.
                new()
                {
                    Feat = FeatType.ArcPulseTechnique,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(ShockStatusEffect) },
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Deals 0 base damage by design (control-only); SkillType.Mimicry isn't a weapon skill type so the shared impact pipeline short-circuits base damage to 0.",
                },

                // BarbedVolleyTechniqueAbilityDefinition - cone AoE, positive base damage + unconditional Bleed.
                new()
                {
                    Feat = FeatType.BarbedVolleyTechnique,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(BleedStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // BloodFrenzyFlurryTechniqueAbilityDefinition - cone AoE, positive damage + unconditional Bleed.
                new()
                {
                    Feat = FeatType.BloodFrenzyFlurryTechnique,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(BleedStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // BraceBreakerTechniqueAbilityDefinition - single target, 0 base damage, unconditional Dazed.
                new()
                {
                    Feat = FeatType.BraceBreakerTechnique,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(DazedStatusEffect) },
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Deals 0 base damage by design (control-only).",
                },

                // BrutalBashTechniqueAbilityDefinition - single target, positive damage + unconditional Knockdown.
                new()
                {
                    Feat = FeatType.BrutalBashTechnique,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(KnockdownStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // CapacitorSurgeTechniqueAbilityDefinition - self-centered sphere, 0 base damage, unconditional Shock.
                new()
                {
                    Feat = FeatType.CapacitorSurgeTechnique,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(ShockStatusEffect) },
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Deals 0 base damage by design (control-only).",
                },

                // ConcussiveChallengeTechniqueAbilityDefinition - self-centered sphere, 0 base damage, unconditional Dazed.
                new()
                {
                    Feat = FeatType.ConcussiveChallengeTechnique,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(DazedStatusEffect) },
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Deals 0 base damage by design (control-only taunt).",
                },

                // CrossfireDrillTechniqueAbilityDefinition - cone, 0 base damage, unconditional Suppression.
                new()
                {
                    Feat = FeatType.CrossfireDrillTechnique,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(SuppressionStatusEffect) },
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Deals 0 base damage by design (control-only).",
                },

                // CryoBileTechniqueAbilityDefinition - cone, 0 base damage, unconditional Freezing + Immobilized.
                new()
                {
                    Feat = FeatType.CryoBileTechnique,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(FreezingStatusEffect), typeof(ImmobilizedStatusEffect) },
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Deals 0 base damage by design (control-only); Immobilized rides in as an additionalStatusEffect.",
                },

                // DarkShockTechniqueAbilityDefinition - sphere requiring a target, 0 base damage, unconditional
                // ForceSuppression.
                new()
                {
                    Feat = FeatType.DarkShockTechnique,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(ForceSuppressionStatusEffect) },
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Deals 0 base damage by design (control-only).",
                },

                // DisorientingScreechTechniqueAbilityDefinition - hand-written self-centered sphere impact,
                // 0 base damage, unconditional Disoriented.
                new()
                {
                    Feat = FeatType.DisorientingScreechTechnique,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(DisorientedStatusEffect) },
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Deals 0 base damage by design (control-only); MimicrySkillRequirement is 0.",
                },

                // DreadWaveTechniqueAbilityDefinition - self-centered sphere, positive damage + unconditional Weakened.
                new()
                {
                    Feat = FeatType.DreadWaveTechnique,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(WeakenedStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // FinalEclipseTechniqueAbilityDefinition - line AoE, positive damage + unconditional
                // ForceDisruption. Also restores FP on hit (raw stat effect, not asserted).
                new()
                {
                    Feat = FeatType.FinalEclipseTechnique,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(ForceDisruptionStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "afterSuccessfulHit restores the activator's FP; not asserted since Mimicry only ever declares a Stamina requirement.",
                },

                // FinalLineTechniqueAbilityDefinition - line AoE, positive damage + unconditional Exposed.
                new()
                {
                    Feat = FeatType.FinalLineTechnique,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(ExposedStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "damagePercentAdjustment (MissingHpRamp) is a conditional bonus on top of the unconditional base hit; not asserted.",
                },

                // FinalSuppressionTechniqueAbilityDefinition - line AoE, 0 base damage, unconditional Stunned.
                new()
                {
                    Feat = FeatType.FinalSuppressionTechnique,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(StunnedStatusEffect) },
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Deals 0 base damage by design (control-only).",
                },

                // FrostSpitTechniqueAbilityDefinition - single target, 0 base damage, unconditional Hamstring.
                new()
                {
                    Feat = FeatType.FrostSpitTechnique,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(HamstringStatusEffect) },
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Deals 0 base damage by design (control-only).",
                },

                // GoringChargeTechniqueAbilityDefinition - line AoE, positive damage + unconditional Bleed.
                new()
                {
                    Feat = FeatType.GoringChargeTechnique,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(BleedStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // GrenadeBurstTechniqueAbilityDefinition - sphere requiring a target, positive damage +
                // unconditional Burn.
                new()
                {
                    Feat = FeatType.GrenadeBurstTechnique,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(BurnStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // HoldfastSlamTechniqueAbilityDefinition - single target, 0 base damage, unconditional
                // Sunder + Exposed.
                new()
                {
                    Feat = FeatType.HoldfastSlamTechnique,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(SunderStatusEffect), typeof(ExposedStatusEffect) },
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Deals 0 base damage by design (control-only); Exposed rides in as an additionalStatusEffect.",
                },

                // InfernoBlastTechniqueAbilityDefinition - hand-written cone impact, positive damage +
                // unconditional Burn.
                new()
                {
                    Feat = FeatType.InfernoBlastTechnique,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(BurnStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "damagePercentAdjustment (ComboBonus on Burn) is a conditional bonus on top of the unconditional base hit; not asserted.",
                },

                // InnerCircleBindTechniqueAbilityDefinition - single target, 0 base damage, unconditional
                // WeaponJam1 + Immobilized.
                new()
                {
                    Feat = FeatType.InnerCircleBindTechnique,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(WeaponJam1StatusEffect), typeof(ImmobilizedStatusEffect) },
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Deals 0 base damage by design (control-only); Immobilized rides in as an additionalStatusEffect.",
                },

                // InnerCirclePounceTechniqueAbilityDefinition - single target, positive damage + unconditional Exposed.
                new()
                {
                    Feat = FeatType.InnerCirclePounceTechnique,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(ExposedStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // InnerCircleSurgeTechniqueAbilityDefinition - single target, positive damage + unconditional
                // Exposed. Also chains a reduced-damage arc to nearby hostiles; not asserted.
                new()
                {
                    Feat = FeatType.InnerCircleSurgeTechnique,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(ExposedStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // InnerCircleVolleyTechniqueAbilityDefinition - single target, positive damage + unconditional Disoriented.
                new()
                {
                    Feat = FeatType.InnerCircleVolleyTechnique,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(DisorientedStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // InnerRingFlurryTechniqueAbilityDefinition - single target, 0 base damage, unconditional Bleed.
                // Also restores some Stamina on hit, but net cost still leaves the pool lower than before.
                new()
                {
                    Feat = FeatType.InnerRingFlurryTechnique,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(BleedStatusEffect) },
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    ImpactRefundsCosts = true,
                    Notes = "Deals 0 base damage by design (control-only); afterSuccessfulHit refunds 4 STM per hit (InnateAbility.RestoreStaminaOnHit), so only the net stamina dip is observable.",
                },

                // InnerVoidTechniqueAbilityDefinition - single target, positive damage + unconditional Weakened.
                new()
                {
                    Feat = FeatType.InnerVoidTechnique,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(WeakenedStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // IonBurstTechniqueAbilityDefinition - cone, 0 base damage, unconditional Disoriented.
                new()
                {
                    Feat = FeatType.IonBurstTechnique,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(DisorientedStatusEffect) },
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Deals 0 base damage by design (control-only).",
                },

                // LockstepCrushTechniqueAbilityDefinition - cone, positive damage + unconditional Knockdown;
                // afterSuccessfulHit unconditionally applies Sunder as well (no gating condition).
                new()
                {
                    Feat = FeatType.LockstepCrushTechnique,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(KnockdownStatusEffect), typeof(SunderStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "afterSuccessfulHit unconditionally applies Sunder in addition to the declared Knockdown status.",
                },

                // MercilessAngleTechniqueAbilityDefinition - cone, positive damage, no declared status type
                // (null); afterSuccessfulHit applies Hemorrhage whenever the target isn't already
                // Bleeding/Hemorrhaging, which is always true on a fresh target.
                new()
                {
                    Feat = FeatType.MercilessAngleTechnique,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(HemorrhageStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "No statusEffect type is declared (null); afterSuccessfulHit's ResolveHemorrhage applies Hemorrhage on the 'not already bleeding' branch, which is guaranteed for a fresh target.",
                },

                // NullShockTechniqueAbilityDefinition - sphere with no OriginOnSelf flag (requires a real
                // target), 0 base damage, unconditional ForceSuppression. Also drains target FP/Stamina
                // on hit; not asserted.
                new()
                {
                    Feat = FeatType.NullShockTechnique,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(ForceSuppressionStatusEffect) },
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Deals 0 base damage by design (control-only); targeting has no OriginOnSelf flag so RequiresTarget is set and a real hostile target is required.",
                },

                // PackHarrierTechniqueAbilityDefinition - single target, 0 base damage, unconditional Hobble.
                new()
                {
                    Feat = FeatType.PackHarrierTechnique,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(HobbleStatusEffect) },
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Deals 0 base damage by design (control-only).",
                },

                // PermafrostRuptureTechniqueAbilityDefinition - self-centered sphere, 0 base damage,
                // unconditional Freezing.
                new()
                {
                    Feat = FeatType.PermafrostRuptureTechnique,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(FreezingStatusEffect) },
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Deals 0 base damage by design (control-only).",
                },

                // PiercingQuillsTechniqueAbilityDefinition - hand-written cone impact, 0 base damage,
                // unconditional Sunder.
                new()
                {
                    Feat = FeatType.PiercingQuillsTechnique,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(SunderStatusEffect) },
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Deals 0 base damage by design (control-only).",
                },

                // PouncingStrikeTechniqueAbilityDefinition - single target, positive damage + unconditional Knockdown.
                new()
                {
                    Feat = FeatType.PouncingStrikeTechnique,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(KnockdownStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // PressureLockTechniqueAbilityDefinition - cone, 0 base damage, unconditional Immobilized.
                new()
                {
                    Feat = FeatType.PressureLockTechnique,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(ImmobilizedStatusEffect) },
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Deals 0 base damage by design (control-only).",
                },

                // RakingClawsTechniqueAbilityDefinition - single target, positive damage + unconditional Hamstring.
                new()
                {
                    Feat = FeatType.RakingClawsTechnique,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(HamstringStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // RallyBreakerTechniqueAbilityDefinition - single target, 0 base damage, unconditional Marked.
                new()
                {
                    Feat = FeatType.RallyBreakerTechnique,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(MarkedStatusEffect) },
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Deals 0 base damage by design (control-only taunt).",
                },

                // RupturingQuakeTechniqueAbilityDefinition - hand-written self-centered sphere impact,
                // positive damage + unconditional Knockdown; afterSuccessfulHit unconditionally applies Sunder too.
                new()
                {
                    Feat = FeatType.RupturingQuakeTechnique,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(KnockdownStatusEffect), typeof(SunderStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "afterSuccessfulHit unconditionally applies Sunder in addition to the declared Knockdown status.",
                },

                // SavageRoarTechniqueAbilityDefinition - self-centered sphere, 0 base damage, unconditional Weakened.
                new()
                {
                    Feat = FeatType.SavageRoarTechnique,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(WeakenedStatusEffect) },
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Deals 0 base damage by design (control-only taunt).",
                },

                // ScorchingBreathTechniqueAbilityDefinition - cone, positive damage, unconditional Burn + Weakened.
                new()
                {
                    Feat = FeatType.ScorchingBreathTechnique,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(BurnStatusEffect), typeof(WeakenedStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Weakened rides in as an additionalStatusEffect alongside the declared Burn.",
                },

                // SeismicSlamTechniqueAbilityDefinition - hand-written self-centered sphere impact, positive
                // damage + unconditional Knockdown.
                new()
                {
                    Feat = FeatType.SeismicSlamTechnique,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(KnockdownStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // ShrapnelBurstTechniqueAbilityDefinition - cone, positive damage + unconditional Sunder.
                new()
                {
                    Feat = FeatType.ShrapnelBurstTechnique,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(SunderStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // SignalSnareTechniqueAbilityDefinition - single target, 0 base damage, unconditional Disoriented.
                new()
                {
                    Feat = FeatType.SignalSnareTechnique,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(DisorientedStatusEffect) },
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Deals 0 base damage by design (control-only taunt).",
                },

                // SonicShriekTechniqueAbilityDefinition - cone, positive damage + unconditional Disoriented.
                new()
                {
                    Feat = FeatType.SonicShriekTechnique,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(DisorientedStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "MimicrySkillRequirement is 0.",
                },

                // StaticBurstTechniqueAbilityDefinition - self-centered sphere, positive damage + unconditional
                // Shock. Also chains a reduced-damage arc to nearby hostiles; not asserted.
                new()
                {
                    Feat = FeatType.StaticBurstTechnique,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(ShockStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // StaticWebTechniqueAbilityDefinition - self-centered sphere, 0 base damage, unconditional Shock.
                new()
                {
                    Feat = FeatType.StaticWebTechnique,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(ShockStatusEffect) },
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Deals 0 base damage by design (control-only).",
                },

                // SuppressingShotTechniqueAbilityDefinition - line AoE, 0 base damage, unconditional Dazed.
                new()
                {
                    Feat = FeatType.SuppressingShotTechnique,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(DazedStatusEffect) },
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Deals 0 base damage by design (control-only).",
                },

                // TailSweepTechniqueAbilityDefinition - self-centered sphere, positive damage + unconditional Dazed.
                new()
                {
                    Feat = FeatType.TailSweepTechnique,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(DazedStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // TerrifyingBellowTechniqueAbilityDefinition - self-centered sphere, 0 base damage,
                // unconditional Dazed.
                new()
                {
                    Feat = FeatType.TerrifyingBellowTechnique,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(DazedStatusEffect) },
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Deals 0 base damage by design (control-only taunt).",
                },

                // ToxicCloudTechniqueAbilityDefinition - sphere with no OriginOnSelf flag (requires a real
                // target), positive damage + unconditional Toxin.
                new()
                {
                    Feat = FeatType.ToxicCloudTechnique,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(ToxinStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // ToxicSpitTechniqueAbilityDefinition - single target, positive damage + unconditional Poison.
                new()
                {
                    Feat = FeatType.ToxicSpitTechnique,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(PoisonStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // VenomSprayTechniqueAbilityDefinition - cone, positive damage + unconditional Poison.
                new()
                {
                    Feat = FeatType.VenomSprayTechnique,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(PoisonStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // WardenClampTechniqueAbilityDefinition - self-centered sphere, 0 base damage, unconditional Dazed.
                new()
                {
                    Feat = FeatType.WardenClampTechnique,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(DazedStatusEffect) },
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Deals 0 base damage by design (control-only taunt).",
                },

                // WardenMarkTechniqueAbilityDefinition - self-centered sphere, 0 base damage, unconditional Marked.
                new()
                {
                    Feat = FeatType.WardenMarkTechnique,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(MarkedStatusEffect) },
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Deals 0 base damage by design (control-only taunt).",
                },

                // WardenMaulTechniqueAbilityDefinition - self-centered sphere, 0 base damage, unconditional
                // Knockdown. Also pulls the target adjacent on hit; not asserted.
                new()
                {
                    Feat = FeatType.WardenMaulTechnique,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(KnockdownStatusEffect) },
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Deals 0 base damage by design (control-only).",
                },

                // WardenRendTechniqueAbilityDefinition - self-centered sphere, 0 base damage, unconditional Weakened.
                new()
                {
                    Feat = FeatType.WardenRendTechnique,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(WeakenedStatusEffect) },
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Deals 0 base damage by design (control-only).",
                },

                // WillFractureTechniqueAbilityDefinition - cone, 0 base damage, unconditional FoggyMind.
                new()
                {
                    Feat = FeatType.WillFractureTechnique,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(FoggyMindStatusEffect) },
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Deals 0 base damage by design (control-only).",
                },
            };
        }
    }
}
