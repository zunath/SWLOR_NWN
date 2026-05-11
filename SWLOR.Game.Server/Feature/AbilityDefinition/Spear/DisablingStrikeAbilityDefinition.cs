using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Spear
{
    public class DisablingStrikeAbilityDefinition : SpearActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ConfigureWeapon(
                builder
                    .Create(FeatType.DisablingStrike1, PerkType.DisablingStrike)
                    .Name("Disabling Strike I")
                    .Level(1)
                    .HasRecastDelay(RecastGroup.DisablingStrike, 3f),
                SkillType.Spear,
                12,
                8,
                typeof(ForceDisruptionStatusEffect),
                4);
            ConfigureWeapon(
                builder
                    .Create(FeatType.DisablingStrike2, PerkType.DisablingStrike)
                    .Name("Disabling Strike II")
                    .Level(2)
                    .HasRecastDelay(RecastGroup.DisablingStrike, 3f),
                SkillType.Spear,
                18,
                8,
                typeof(ForceDisruptionStatusEffect),
                5);
            ConfigureWeapon(
                builder
                    .Create(FeatType.DisablingStrike3, PerkType.DisablingStrike)
                    .Name("Disabling Strike III")
                    .Level(3)
                    .HasRecastDelay(RecastGroup.DisablingStrike, 3f),
                SkillType.Spear,
                26,
                8,
                typeof(ForceDisruptionStatusEffect),
                6);

            return builder.Build();
        }
    }
}
