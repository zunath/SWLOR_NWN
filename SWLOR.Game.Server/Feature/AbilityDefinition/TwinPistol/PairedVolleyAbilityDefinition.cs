using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.TwinPistol
{
    public sealed class PairedVolleyAbilityDefinition : TwinPistolAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            ConfigurePairAttack(builder, FeatType.PairedVolley1, PerkType.PairedVolley,
                "Paired Volley I", 1, RecastGroup.TwinVolley, 18f,
                new TwinPistolAbilitySpec
                {
                    FlatPower = 4,
                    StaminaCost = 3,
                });

            ConfigurePairAttack(builder, FeatType.PairedVolley2, PerkType.PairedVolley,
                "Paired Volley II", 2, RecastGroup.TwinVolley, 18f,
                new TwinPistolAbilitySpec
                {
                    FlatPower = 10,
                    StaminaCost = 5,
                });

            ConfigurePairAttack(builder, FeatType.PairedVolley3, PerkType.PairedVolley,
                "Paired Volley III", 3, RecastGroup.TwinVolley, 18f,
                new TwinPistolAbilitySpec
                {
                    FlatPower = 18,
                    StaminaCost = 8,
                });

            ConfigurePairAttack(builder, FeatType.PairedVolley4, PerkType.PairedVolley,
                "Paired Volley IV", 4, RecastGroup.TwinVolley, 18f,
                new TwinPistolAbilitySpec
                {
                    FlatPower = 26,
                    StaminaCost = 10,
                });
            return builder.Build();
        }
    }
}

