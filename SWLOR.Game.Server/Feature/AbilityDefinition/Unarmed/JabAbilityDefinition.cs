using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Unarmed
{
    public class JabAbilityDefinition : UnarmedAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ConfigureStrike(
                builder.Create(FeatType.Jab1, PerkType.Jab)
                    .Name("Jab I")
                    .Level(1)
                    .HasRecastDelay(RecastGroup.UnarmedOpener, 18f)
                    .DisplaysVisualEffectOnDamage(VisualEffect.Vfx_Com_Blood_Spark_Small),
                new MeleeTechniqueDetail
                {
                    Line = MeleeTechniqueLine.MartialArtist,
                    CreatedStage = MeleeTechniqueStage.Opening,
                    SetupDurationSeconds = 4,
                    OrdinaryAccuracyBonus = 3,
                },
                8,
                3,
                activationDelay: 0f);

            ConfigureStrike(
                builder.Create(FeatType.Jab2, PerkType.Jab)
                    .Name("Jab II")
                    .Level(2)
                    .HasRecastDelay(RecastGroup.UnarmedOpener, 18f)
                    .DisplaysVisualEffectOnDamage(VisualEffect.Vfx_Com_Blood_Spark_Small),
                new MeleeTechniqueDetail
                {
                    Line = MeleeTechniqueLine.MartialArtist,
                    CreatedStage = MeleeTechniqueStage.Opening,
                    SetupDurationSeconds = 4,
                    OrdinaryAccuracyBonus = 3,
                },
                18,
                5,
                activationDelay: 0f);

            ConfigureStrike(
                builder.Create(FeatType.Jab3, PerkType.Jab)
                    .Name("Jab III")
                    .Level(3)
                    .HasRecastDelay(RecastGroup.UnarmedOpener, 18f)
                    .DisplaysVisualEffectOnDamage(VisualEffect.Vfx_Com_Blood_Spark_Small),
                new MeleeTechniqueDetail
                {
                    Line = MeleeTechniqueLine.MartialArtist,
                    CreatedStage = MeleeTechniqueStage.Opening,
                    SetupDurationSeconds = 4,
                    OrdinaryAccuracyBonus = 3,
                },
                28,
                8,
                activationDelay: 0f);

            ConfigureStrike(
                builder.Create(FeatType.Jab4, PerkType.Jab)
                    .Name("Jab IV")
                    .Level(4)
                    .HasRecastDelay(RecastGroup.UnarmedOpener, 18f)
                    .DisplaysVisualEffectOnDamage(VisualEffect.Vfx_Com_Blood_Spark_Small),
                new MeleeTechniqueDetail
                {
                    Line = MeleeTechniqueLine.MartialArtist,
                    CreatedStage = MeleeTechniqueStage.Opening,
                    SetupDurationSeconds = 4,
                    OrdinaryAccuracyBonus = 3,
                },
                38,
                12,
                activationDelay: 0f);

            return builder.Build();
        }
    }
}
