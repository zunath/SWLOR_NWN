using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Unarmed
{
    public class LowKickAbilityDefinition : UnarmedAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ConfigureStrike(
                builder.Create(FeatType.LowKick1, PerkType.LowKick)
                    .Name("Low Kick I")
                    .Level(1)
                    .HasRecastDelay(RecastGroup.UnarmedTechnique, 30f)
                    .DisplaysVisualEffectOnDamage(VisualEffect.Vfx_Com_Blood_Spark_Small),
                new MeleeTechniqueDetail
                {
                    Line = MeleeTechniqueLine.Brawler,
                    PreservesMatchingSetup = true,
                },
                16,
                6,
                statusDuration: 6,
                statusFactory: () => new LowKickStatusEffect(10));

            ConfigureStrike(
                builder.Create(FeatType.LowKick2, PerkType.LowKick)
                    .Name("Low Kick II")
                    .Level(2)
                    .HasRecastDelay(RecastGroup.UnarmedTechnique, 30f)
                    .DisplaysVisualEffectOnDamage(VisualEffect.Vfx_Com_Blood_Spark_Small),
                new MeleeTechniqueDetail
                {
                    Line = MeleeTechniqueLine.Brawler,
                    PreservesMatchingSetup = true,
                },
                30,
                9,
                statusDuration: 8,
                statusFactory: () => new LowKickStatusEffect(10));

            return builder.Build();
        }
    }
}
