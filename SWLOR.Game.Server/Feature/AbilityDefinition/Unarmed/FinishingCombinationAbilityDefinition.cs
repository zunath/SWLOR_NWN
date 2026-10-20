using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Unarmed
{
    public class FinishingCombinationAbilityDefinition : UnarmedAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ConfigureStrike(
                builder.Create(FeatType.FinishingCombination1, PerkType.FinishingCombination)
                    .Name("Finishing Combination I")
                    .Level(1)
                    .HasRecastDelay(RecastGroup.UnarmedTechnique, 30f)
                    .DisplaysVisualEffectOnDamage(VisualEffect.Vfx_Com_Blood_Spark_Small),
                new MeleeTechniqueDetail
                {
                    Line = MeleeTechniqueLine.MartialArtist,
                    ConsumedStages = MeleeTechniqueStage.Opening | MeleeTechniqueStage.Finish,
                    OpeningPower = 6,
                    FinishPower = 12,
                    ImpactCount = 2,
                    OpeningSweepRadiusMeters = 5f,
                    WeaponDamagePercent = 50,
                    RefundEligible = true,
                },
                8,
                6,
                activationDelay: 0f);

            ConfigureStrike(
                builder.Create(FeatType.FinishingCombination2, PerkType.FinishingCombination)
                    .Name("Finishing Combination II")
                    .Level(2)
                    .HasRecastDelay(RecastGroup.UnarmedTechnique, 30f)
                    .DisplaysVisualEffectOnDamage(VisualEffect.Vfx_Com_Blood_Spark_Small),
                new MeleeTechniqueDetail
                {
                    Line = MeleeTechniqueLine.MartialArtist,
                    ConsumedStages = MeleeTechniqueStage.Opening | MeleeTechniqueStage.Finish,
                    OpeningPower = 8,
                    FinishPower = 16,
                    ImpactCount = 2,
                    OpeningSweepRadiusMeters = 5f,
                    WeaponDamagePercent = 50,
                    RefundEligible = true,
                },
                14,
                9,
                activationDelay: 0f);

            return builder.Build();
        }
    }
}
