using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Unarmed
{
    public class UppercutAbilityDefinition : UnarmedAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ConfigureStrike(
                builder.Create(FeatType.Uppercut1, PerkType.Uppercut)
                    .Name("Uppercut I")
                    .Level(1)
                    .HasRecastDelay(RecastGroup.UnarmedResponse, 24f)
                    .DisplaysVisualEffectOnDamage(VisualEffect.Vfx_Com_Blood_Spark_Medium),
                new MeleeTechniqueDetail
                {
                    Line = MeleeTechniqueLine.Brawler,
                    PreservesMatchingSetup = true,
                    UsesAbilityPower = true,
                    ConsumesRetaliation = true,
                    RefundEligible = true,
                    RefundRequiresSetup = false,
                },
                12,
                4);

            ConfigureStrike(
                builder.Create(FeatType.Uppercut2, PerkType.Uppercut)
                    .Name("Uppercut II")
                    .Level(2)
                    .HasRecastDelay(RecastGroup.UnarmedResponse, 24f)
                    .DisplaysVisualEffectOnDamage(VisualEffect.Vfx_Com_Blood_Spark_Medium),
                new MeleeTechniqueDetail
                {
                    Line = MeleeTechniqueLine.Brawler,
                    PreservesMatchingSetup = true,
                    UsesAbilityPower = true,
                    ConsumesRetaliation = true,
                    RefundEligible = true,
                    RefundRequiresSetup = false,
                },
                24,
                8);

            ConfigureStrike(
                builder.Create(FeatType.Uppercut3, PerkType.Uppercut)
                    .Name("Uppercut III")
                    .Level(3)
                    .HasRecastDelay(RecastGroup.UnarmedResponse, 24f)
                    .DisplaysVisualEffectOnDamage(VisualEffect.Vfx_Com_Blood_Spark_Medium),
                new MeleeTechniqueDetail
                {
                    Line = MeleeTechniqueLine.Brawler,
                    PreservesMatchingSetup = true,
                    UsesAbilityPower = true,
                    ConsumesRetaliation = true,
                    RefundEligible = true,
                    RefundRequiresSetup = false,
                },
                36,
                10);

            return builder.Build();
        }
    }
}
