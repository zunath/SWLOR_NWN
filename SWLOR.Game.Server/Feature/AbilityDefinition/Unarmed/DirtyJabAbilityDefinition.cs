using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Unarmed
{
    public class DirtyJabAbilityDefinition : UnarmedAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ConfigureStrike(
                builder.Create(FeatType.DirtyJab1, PerkType.DirtyJab)
                    .Name("Dirty Jab I")
                    .Level(1)
                    .HasRecastDelay(RecastGroup.UnarmedOpener, 18f)
                    .DisplaysVisualEffectOnDamage(VisualEffect.Vfx_Com_Blood_Spark_Small),
                new MeleeTechniqueDetail
                {
                    Line = MeleeTechniqueLine.Brawler,
                    CreatedStage = MeleeTechniqueStage.Distraction,
                    SetupDurationSeconds = 6,
                    SourceOwnedSetupStatusEffectFactory = () => new RattledStatusEffect(10),
                },
                8,
                3,
                activationDelay: 0f);

            ConfigureStrike(
                builder.Create(FeatType.DirtyJab2, PerkType.DirtyJab)
                    .Name("Dirty Jab II")
                    .Level(2)
                    .HasRecastDelay(RecastGroup.UnarmedOpener, 18f)
                    .DisplaysVisualEffectOnDamage(VisualEffect.Vfx_Com_Blood_Spark_Small),
                new MeleeTechniqueDetail
                {
                    Line = MeleeTechniqueLine.Brawler,
                    CreatedStage = MeleeTechniqueStage.Distraction,
                    SetupDurationSeconds = 6,
                    SourceOwnedSetupStatusEffectFactory = () => new RattledStatusEffect(10),
                },
                18,
                5,
                activationDelay: 0f);

            ConfigureStrike(
                builder.Create(FeatType.DirtyJab3, PerkType.DirtyJab)
                    .Name("Dirty Jab III")
                    .Level(3)
                    .HasRecastDelay(RecastGroup.UnarmedOpener, 18f)
                    .DisplaysVisualEffectOnDamage(VisualEffect.Vfx_Com_Blood_Spark_Small),
                new MeleeTechniqueDetail
                {
                    Line = MeleeTechniqueLine.Brawler,
                    CreatedStage = MeleeTechniqueStage.Distraction,
                    SetupDurationSeconds = 6,
                    SourceOwnedSetupStatusEffectFactory = () => new RattledStatusEffect(10),
                },
                28,
                8,
                activationDelay: 0f);

            ConfigureStrike(
                builder.Create(FeatType.DirtyJab4, PerkType.DirtyJab)
                    .Name("Dirty Jab IV")
                    .Level(4)
                    .HasRecastDelay(RecastGroup.UnarmedOpener, 18f)
                    .DisplaysVisualEffectOnDamage(VisualEffect.Vfx_Com_Blood_Spark_Small),
                new MeleeTechniqueDetail
                {
                    Line = MeleeTechniqueLine.Brawler,
                    CreatedStage = MeleeTechniqueStage.Distraction,
                    SetupDurationSeconds = 6,
                    SourceOwnedSetupStatusEffectFactory = () => new RattledStatusEffect(10),
                },
                38,
                12,
                activationDelay: 0f);

            return builder.Build();
        }
    }
}
