using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Unarmed
{
    public class HaymakerAbilityDefinition : UnarmedAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ConfigureStrike(
                builder.Create(FeatType.Haymaker1, PerkType.Haymaker)
                    .Name("Haymaker")
                    .Level(1)
                    .HasRecastDelay(RecastGroup.Capstone, 90f)
                    .DisplaysVisualEffectOnDamage(VisualEffect.Vfx_Com_Blood_Spark_Large),
                new MeleeTechniqueDetail
                {
                    Line = MeleeTechniqueLine.Brawler,
                    PreservesMatchingSetup = true,
                    UsesAbilityPower = true,
                    ConsumesRetaliation = true,
                    RefundEligible = true,
                    RefundRequiresSetup = false,
                },
                50,
                15,
                activationDelay: 2f);

            return builder.Build();
        }
    }
}
