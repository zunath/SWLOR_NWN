using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Vibroblade
{
    public class HackingBladeAbilityDefinition : WeaponActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ConfigureWeapon(
                builder
                    .Create(FeatType.HackingBlade1, PerkType.HackingBlade)
                    .Name("Hacking Blade I")
                    .Level(1)
                    .HasRecastDelay(RecastGroup.HackingBlade, 30f),
                SkillType.Vibroblade,
                8,
                30,
                typeof(BleedStatusEffect),
                4);
            ConfigureWeapon(
                builder
                    .Create(FeatType.HackingBlade2, PerkType.HackingBlade)
                    .Name("Hacking Blade II")
                    .Level(2)
                    .HasRecastDelay(RecastGroup.HackingBlade, 30f),
                SkillType.Vibroblade,
                18,
                60,
                typeof(BleedStatusEffect),
                6);
            ConfigureWeapon(
                builder
                    .Create(FeatType.HackingBlade3, PerkType.HackingBlade)
                    .Name("Hacking Blade III")
                    .Level(3)
                    .HasRecastDelay(RecastGroup.HackingBlade, 30f),
                SkillType.Vibroblade,
                28,
                60,
                typeof(BleedStatusEffect),
                8);

            return builder.Build();
        }
    }
}
