using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.HeavyVibroblade
{
    public class AngerStrikeAbilityDefinition : HeavyVibrobladeActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            AngerStrike(builder);

            return builder.Build();
        }

        private static void AngerStrike(AbilityBuilder builder)
        {
            builder.Create(FeatType.AngerStrike1, PerkType.AngerStrike)
                .Name("Anger Strike")
                .Level(1)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.AngerStrike, 45f)
                .HasImpactAction((activator, target, level, targetLocation) =>
                {
                    var damage = Ability.ApplyCombatImpact(activator, target, targetLocation, SkillType.HeavyVibroblade, 12, 0, null, false);
                    Enmity.ModifyEnmity(activator, target, 450 + damage);
                })
                .IsWeaponAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(4);
        }
    }
}
