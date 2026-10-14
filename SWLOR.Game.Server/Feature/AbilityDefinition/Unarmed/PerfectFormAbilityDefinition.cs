using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Unarmed
{
    public class PerfectFormAbilityDefinition : UnarmedAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ConfigureStrike(
                builder.Create(FeatType.PerfectForm1, PerkType.PerfectForm)
                    .Name("Perfect Form")
                    .Level(1)
                    .HasRecastDelay(RecastGroup.Capstone, 90f)
                    .DisplaysVisualEffectOnDamage(VisualEffect.Vfx_Com_Blood_Spark_Large),
                new MeleeTechniqueDetail
                {
                    Line = MeleeTechniqueLine.MartialArtist,
                    ConsumedStages = MeleeTechniqueStage.Finish,
                    FinishPower = 15,
                    RefundEligible = true,
                },
                45,
                15,
                activationDelay: 2f);

            return builder.Build();
        }
    }
}
