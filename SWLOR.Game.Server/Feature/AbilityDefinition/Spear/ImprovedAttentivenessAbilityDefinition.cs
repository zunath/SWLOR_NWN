using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Spear
{
    public class ImprovedAttentivenessAbilityDefinition : SpearActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ConfigurePartyStatus(
                builder
                    .Create(FeatType.ImprovedAttentiveness1, PerkType.ImprovedAttentiveness)
                    .Name("Improved Attentiveness")
                    .Level(1)
                    .HasRecastDelay(RecastGroup.ImprovedAttentiveness, 300f),
                typeof(ImprovedAttentivenessStatusEffect),
                60f,
                25,
                false);

            return builder.Build();
        }
    }
}
