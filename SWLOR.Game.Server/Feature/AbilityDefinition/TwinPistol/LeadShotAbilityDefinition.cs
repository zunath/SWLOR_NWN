using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.TwinPistol
{
    public sealed class LeadShotAbilityDefinition : TwinPistolAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            ConfigurePairAttack(builder, FeatType.LeadShot1, PerkType.LeadShot,
                "Lead Shot I", 1, RecastGroup.TwinVolley, 18f,
                new TwinPistolAbilitySpec
                {
                    FlatPower = 4,
                    StaminaCost = 3,
                    CadenceOnHit = 1,
                });

            ConfigurePairAttack(builder, FeatType.LeadShot2, PerkType.LeadShot,
                "Lead Shot II", 2, RecastGroup.TwinVolley, 18f,
                new TwinPistolAbilitySpec
                {
                    FlatPower = 8,
                    StaminaCost = 5,
                    CadenceOnHit = 1,
                });

            ConfigurePairAttack(builder, FeatType.LeadShot3, PerkType.LeadShot,
                "Lead Shot III", 3, RecastGroup.TwinVolley, 18f,
                new TwinPistolAbilitySpec
                {
                    FlatPower = 12,
                    StaminaCost = 8,
                    CadenceOnHit = 1,
                });

            ConfigurePairAttack(builder, FeatType.LeadShot4, PerkType.LeadShot,
                "Lead Shot IV", 4, RecastGroup.TwinVolley, 18f,
                new TwinPistolAbilitySpec
                {
                    FlatPower = 16,
                    StaminaCost = 12,
                    CadenceOnHit = 1,
                });
            return builder.Build();
        }
    }
}
