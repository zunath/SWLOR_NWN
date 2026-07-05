using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Vibroblade
{
    public class RiotBladeAbilityDefinition : WeaponActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ConfigureRiotBlade(builder, FeatType.RiotBlade1, "Riot Blade I", 1, 10, 3);
            ConfigureRiotBlade(builder, FeatType.RiotBlade2, "Riot Blade II", 2, 15, 4);
            ConfigureRiotBlade(builder, FeatType.RiotBlade3, "Riot Blade III", 3, 20, 5);
            ConfigureRiotBlade(builder, FeatType.RiotBlade4, "Riot Blade IV", 4, 25, 6);

            return builder.Build();
        }

        private static void ConfigureRiotBlade(
            AbilityBuilder builder,
            FeatType featType,
            string name,
            int level,
            int baseDamage,
            int stamina)
        {
            ConfigureWeapon(
                builder
                    .Create(featType, PerkType.RiotBlade)
                    .Name(name)
                    .Level(level)
                    .HasRecastDelay(RecastGroup.RiotBlade, 18f)
                    .UsesAnimation(Animation.RiotBlade),
                SkillType.Vibroblade,
                baseDamage,
                0,
                null,
                stamina);
        }
    }
}
