using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
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

            ConfigureWeapon(
                builder
                    .Create(FeatType.ToxicCoating1, PerkType.ToxicCoating)
                    .Name("Toxic Coating I")
                    .Level(1)
                    .HasRecastDelay(RecastGroup.ToxicCoating, 45f),
                SkillType.Vibroknife,
                10,
                30,
                typeof(ToxinStatusEffect),
                4,
                damageType: CombatDamageType.Poison);
            ConfigureWeapon(
                builder
                    .Create(FeatType.ToxicCoating2, PerkType.ToxicCoating)
                    .Name("Toxic Coating II")
                    .Level(2)
                    .HasRecastDelay(RecastGroup.ToxicCoating, 45f),
                SkillType.Vibroknife,
                22,
                30,
                typeof(ToxinStatusEffect),
                6,
                damageType: CombatDamageType.Poison);

            return builder.Build();
        }
    }
}
