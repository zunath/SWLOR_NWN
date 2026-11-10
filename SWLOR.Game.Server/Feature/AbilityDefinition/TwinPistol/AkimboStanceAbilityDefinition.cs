using System.Collections.Generic;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.TwinPistol
{
    public sealed class AkimboStanceAbilityDefinition : TwinPistolAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            ConfigurePairStance(builder, FeatType.AkimboStance, PerkType.AkimboStance, "Akimbo Stance",
                RecastGroup.AkimboStance, typeof(AkimboStanceStatusEffect));
            return builder.Build();
        }
    }
}

