using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Vibroblade
{
    public class BerserkerStanceAbilityDefinition : WeaponActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ConfigureToggle(
                builder
                    .Create(FeatType.BerserkerStance1, PerkType.BerserkerStance)
                    .Name("Berserker Stance I")
                    .Level(1)
                    .HasRecastDelay(RecastGroup.BerserkerStance, 30f)
                    .UsesAnimation(Animation.OneHandedStance),
                typeof(BerserkerStanceStatusEffect),
                () => new BerserkerStanceStatusEffect(1));
            ConfigureToggle(
                builder
                    .Create(FeatType.BerserkerStance2, PerkType.BerserkerStance)
                    .Name("Berserker Stance II")
                    .Level(2)
                    .HasRecastDelay(RecastGroup.BerserkerStance, 30f)
                    .UsesAnimation(Animation.OneHandedStance),
                typeof(BerserkerStanceStatusEffect),
                () => new BerserkerStanceStatusEffect(2));

            return builder.Build();
        }
    }
}
