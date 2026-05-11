using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Rifle
{
    public class CripplingShotAbilityDefinition : WeaponActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ConfigureWeapon(
                builder
                    .Create(FeatType.CripplingShot1, PerkType.CripplingShot)
                    .Name("Crippling Shot I")
                    .Level(1)
                    .HasRecastDelay(RecastGroup.CripplingShot, 30f),
                SkillType.Rifle,
                12,
                12,
                typeof(DisorientedStatusEffect),
                4);
            ConfigureWeapon(
                builder
                    .Create(FeatType.CripplingShot2, PerkType.CripplingShot)
                    .Name("Crippling Shot II")
                    .Level(2)
                    .HasRecastDelay(RecastGroup.CripplingShot, 30f),
                SkillType.Rifle,
                22,
                15,
                typeof(DisorientedStatusEffect),
                6);
            ConfigureWeapon(
                builder
                    .Create(FeatType.CripplingShot3, PerkType.CripplingShot)
                    .Name("Crippling Shot III")
                    .Level(3)
                    .HasRecastDelay(RecastGroup.CripplingShot, 30f),
                SkillType.Rifle,
                34,
                20,
                typeof(DisorientedStatusEffect),
                8);

            return builder.Build();
        }
    }
}
