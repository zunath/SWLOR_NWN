using System.Collections.Generic;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions.AbilityBehaviors
{
    /// <summary>
    /// Declarative engine-test coverage for the passive Mimicry trait feats
    /// (SWLOR.Game.Server/Feature/AbilityDefinition/Mimicry), built with
    /// AbilityBuilder.MimicryTrait + MimicryTraitStat/MimicryTraitResistance.
    ///
    /// A trait's ability definition declares only Create/Name/SkillType/Level/MimicryTrait(...)/
    /// MimicryTraitStat(...)/MimicryTraitResistance(...) - none of them call HasImpactAction,
    /// HasRecastDelay, RequirementStamina, or RequirementFP. This is by design, not an oversight:
    /// per Service/Mimicry.cs's GrantTechniqueFeat, a trait is deliberately never granted as a
    /// usable/hotbar feat for players - its stat/resistance bonuses are read directly off the
    /// player's equipped-technique list (Mimicry.GetStatBonus/GetResistanceBonus) for as long as it
    /// stays equipped, with no activation, cooldown, or resource cost of its own.
    ///
    /// UsePerkFeat.TryUseAbility and Ability.CanUseAbility have no such distinction, though: nothing
    /// stops a direct feat activation from succeeding (ability.ImpactAction is simply null, so
    /// ExecuteAbilityImpact's null-conditional invoke does nothing observable). Every case here
    /// therefore only asserts that the activation itself succeeds - no status effect, damage, cost,
    /// or recast is expected because the definition declares none.
    /// </summary>
    public class MimicryTraitAbilityBehaviors : IAbilityBehaviorSource
    {
        [EngineTest("Mimicry ability behaviors (traits)", Category = "AbilityBehavior", TimeoutSeconds = 1800f)]
        public static async Task Run(EngineTestContext ctx)
        {
            await AbilityBehaviorExecutor.RunAsync(ctx, new MimicryTraitAbilityBehaviors().BuildCases());
        }

        public List<AbilityBehaviorCase> BuildCases()
        {
            return new List<AbilityBehaviorCase>
            {
                // BonecrusherBiteTechniqueAbilityDefinition - passive trait (DamageDealtSunderChance).
                new()
                {
                    Feat = FeatType.BonecrusherBiteTechnique,
                    Target = AbilityTargetKind.Self,
                    Notes = "Passive Mimicry trait: no ImpactAction/cost/recast is declared. Its DamageDealtSunderChance stat bonus is read directly off the equipped-technique list (Mimicry.GetStatBonus), not granted by activation.",
                },

                // ChitinGuardTechniqueAbilityDefinition - passive trait (defense + resistances).
                new()
                {
                    Feat = FeatType.ChitinGuardTechnique,
                    Target = AbilityTargetKind.Self,
                    Notes = "Passive Mimicry trait: no ImpactAction/cost/recast is declared. Its stat/resistance bonuses are read directly off the equipped-technique list, not granted by activation.",
                },

                // CripplingTalonsTechniqueAbilityDefinition - passive trait (DamageDealtHemorrhageChance).
                new()
                {
                    Feat = FeatType.CripplingTalonsTechnique,
                    Target = AbilityTargetKind.Self,
                    Notes = "Passive Mimicry trait: no ImpactAction/cost/recast is declared.",
                },

                // EssenceScarTechniqueAbilityDefinition - passive trait (ForceAttackPercentAdjustment).
                new()
                {
                    Feat = FeatType.EssenceScarTechnique,
                    Target = AbilityTargetKind.Self,
                    Notes = "Passive Mimicry trait: no ImpactAction/cost/recast is declared.",
                },

                // ForceRendTechniqueAbilityDefinition - passive trait (ForceAttackPercentAdjustment).
                new()
                {
                    Feat = FeatType.ForceRendTechnique,
                    Target = AbilityTargetKind.Self,
                    Notes = "Passive Mimicry trait: no ImpactAction/cost/recast is declared.",
                },

                // ForceSunderTechniqueAbilityDefinition - passive trait (DamageDealtSunderChance).
                new()
                {
                    Feat = FeatType.ForceSunderTechnique,
                    Target = AbilityTargetKind.Self,
                    Notes = "Passive Mimicry trait: no ImpactAction/cost/recast is declared.",
                },

                // GlacialSlimeTechniqueAbilityDefinition - passive trait (DamageDealtPoisonChance).
                new()
                {
                    Feat = FeatType.GlacialSlimeTechnique,
                    Target = AbilityTargetKind.Self,
                    Notes = "Passive Mimicry trait: no ImpactAction/cost/recast is declared.",
                },

                // HoarfrostGlobTechniqueAbilityDefinition - passive trait (DamageDealtFreezingChance).
                new()
                {
                    Feat = FeatType.HoarfrostGlobTechnique,
                    Target = AbilityTargetKind.Self,
                    Notes = "Passive Mimicry trait: no ImpactAction/cost/recast is declared.",
                },

                // IronCarapaceTechniqueAbilityDefinition - passive trait (defense + resistances).
                new()
                {
                    Feat = FeatType.IronCarapaceTechnique,
                    Target = AbilityTargetKind.Self,
                    Notes = "Passive Mimicry trait: no ImpactAction/cost/recast is declared.",
                },

                // MaulingBiteTechniqueAbilityDefinition - passive trait (DamageDealtBleedChance).
                new()
                {
                    Feat = FeatType.MaulingBiteTechnique,
                    Target = AbilityTargetKind.Self,
                    Notes = "Passive Mimicry trait: no ImpactAction/cost/recast is declared.",
                },

                // MindSpikeTechniqueAbilityDefinition - passive trait (AccuracyPercentAdjustment).
                new()
                {
                    Feat = FeatType.MindSpikeTechnique,
                    Target = AbilityTargetKind.Self,
                    Notes = "Passive Mimicry trait: no ImpactAction/cost/recast is declared.",
                },

                // OpeningCutTechniqueAbilityDefinition - passive trait (DamageDealtBleedChance).
                new()
                {
                    Feat = FeatType.OpeningCutTechnique,
                    Target = AbilityTargetKind.Self,
                    Notes = "Passive Mimicry trait: no ImpactAction/cost/recast is declared.",
                },

                // OverloadShotTechniqueAbilityDefinition - passive trait (DamageDealtShockChance).
                new()
                {
                    Feat = FeatType.OverloadShotTechnique,
                    Target = AbilityTargetKind.Self,
                    Notes = "Passive Mimicry trait: no ImpactAction/cost/recast is declared.",
                },

                // PrecisionShotTechniqueAbilityDefinition - passive trait (CriticalRatePercentAdjustment).
                new()
                {
                    Feat = FeatType.PrecisionShotTechnique,
                    Target = AbilityTargetKind.Self,
                    Notes = "Passive Mimicry trait: no ImpactAction/cost/recast is declared.",
                },

                // RangefinderShotTechniqueAbilityDefinition - passive trait (AccuracyPercentAdjustment).
                new()
                {
                    Feat = FeatType.RangefinderShotTechnique,
                    Target = AbilityTargetKind.Self,
                    Notes = "Passive Mimicry trait: no ImpactAction/cost/recast is declared.",
                },

                // RendingBiteTechniqueAbilityDefinition - passive trait (DamageDealtBleedChance).
                new()
                {
                    Feat = FeatType.RendingBiteTechnique,
                    Target = AbilityTargetKind.Self,
                    Notes = "Passive Mimicry trait: no ImpactAction/cost/recast is declared.",
                },

                // RendingCarveTechniqueAbilityDefinition - passive trait (DamageDealtHemorrhageChance).
                new()
                {
                    Feat = FeatType.RendingCarveTechnique,
                    Target = AbilityTargetKind.Self,
                    Notes = "Passive Mimicry trait: no ImpactAction/cost/recast is declared.",
                },

                // RimePounceTechniqueAbilityDefinition - passive trait (DamageDealtFreezingChance).
                new()
                {
                    Feat = FeatType.RimePounceTechnique,
                    Target = AbilityTargetKind.Self,
                    Notes = "Passive Mimicry trait: no ImpactAction/cost/recast is declared.",
                },

                // SerratedSlashTechniqueAbilityDefinition - passive trait (DamageDealtBleedChance).
                new()
                {
                    Feat = FeatType.SerratedSlashTechnique,
                    Target = AbilityTargetKind.Self,
                    Notes = "Passive Mimicry trait: no ImpactAction/cost/recast is declared.",
                },

                // TacticalMarkTechniqueAbilityDefinition - passive trait (AttackPercentAdjustment).
                new()
                {
                    Feat = FeatType.TacticalMarkTechnique,
                    Target = AbilityTargetKind.Self,
                    Notes = "Passive Mimicry trait: no ImpactAction/cost/recast is declared.",
                },

                // TargetLockTechniqueAbilityDefinition - passive trait (AccuracyPercentAdjustment).
                new()
                {
                    Feat = FeatType.TargetLockTechnique,
                    Target = AbilityTargetKind.Self,
                    Notes = "Passive Mimicry trait: no ImpactAction/cost/recast is declared.",
                },
            };
        }
    }
}
