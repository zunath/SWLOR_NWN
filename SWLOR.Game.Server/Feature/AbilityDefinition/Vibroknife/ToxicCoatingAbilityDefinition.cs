using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Vibroknife
{
    public class ToxicCoatingAbilityDefinition : WeaponActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ConfigureWeapon(builder.Create(FeatType.ToxicCoating1, PerkType.ToxicCoating).Name("Toxic Coating I").Level(1), SkillType.Vibroknife, 10, 30, 10, SavingThrow.Fortitude, typeof(ToxinStatusEffect), 4);
            ConfigureWeapon(builder.Create(FeatType.ToxicCoating2, PerkType.ToxicCoating).Name("Toxic Coating II").Level(2), SkillType.Vibroknife, 22, 30, 15, SavingThrow.Fortitude, typeof(ToxinStatusEffect), 6);

            return builder.Build();
        }
    }
}
