using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Unarmed
{
    public class PalmStrikeAbilityDefinition : UnarmedAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ConfigureStrike(
                builder.Create(FeatType.PalmStrike1, PerkType.PalmStrike)
                    .Name("Palm Strike I")
                    .Level(1)
                    .HasRecastDelay(RecastGroup.UnarmedResponse, 24f)
                    .DisplaysVisualEffectOnDamage(VisualEffect.Vfx_Com_Blood_Spark_Small),
                new MeleeTechniqueDetail
                {
                    Line = MeleeTechniqueLine.MartialArtist,
                    ConsumedStages = MeleeTechniqueStage.Opening,
                    CreatedStage = MeleeTechniqueStage.Finish,
                    CreatesStageOnlyWhenSequenced = true,
                    SetupDurationSeconds = 4,
                    OrdinaryAccuracyBonus = 3,
                },
                12,
                4,
                activationDelay: 0f);

            ConfigureStrike(
                builder.Create(FeatType.PalmStrike2, PerkType.PalmStrike)
                    .Name("Palm Strike II")
                    .Level(2)
                    .HasRecastDelay(RecastGroup.UnarmedResponse, 24f)
                    .DisplaysVisualEffectOnDamage(VisualEffect.Vfx_Com_Blood_Spark_Small),
                new MeleeTechniqueDetail
                {
                    Line = MeleeTechniqueLine.MartialArtist,
                    ConsumedStages = MeleeTechniqueStage.Opening,
                    CreatedStage = MeleeTechniqueStage.Finish,
                    CreatesStageOnlyWhenSequenced = true,
                    SetupDurationSeconds = 4,
                    OrdinaryAccuracyBonus = 3,
                },
                24,
                8,
                activationDelay: 0f);

            ConfigureStrike(
                builder.Create(FeatType.PalmStrike3, PerkType.PalmStrike)
                    .Name("Palm Strike III")
                    .Level(3)
                    .HasRecastDelay(RecastGroup.UnarmedResponse, 24f)
                    .DisplaysVisualEffectOnDamage(VisualEffect.Vfx_Com_Blood_Spark_Small),
                new MeleeTechniqueDetail
                {
                    Line = MeleeTechniqueLine.MartialArtist,
                    ConsumedStages = MeleeTechniqueStage.Opening,
                    CreatedStage = MeleeTechniqueStage.Finish,
                    CreatesStageOnlyWhenSequenced = true,
                    SetupDurationSeconds = 4,
                    OrdinaryAccuracyBonus = 3,
                },
                36,
                10,
                activationDelay: 0f);

            return builder.Build();
        }
    }
}
