using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Rifle
{
    public class OneShotAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            OneShot1(builder);

            return builder.Build();
        }

        private static void OneShot1(AbilityBuilder builder)
        {
            builder.Create(FeatType.OneShot1, PerkType.OneShot)
                .Name("One Shot")
                .Level(1)
                .HasActivationDelay(2f)
                .HasRecastDelay(RecastGroup.OneShot, 1800f)
                .RequiresTarget()
                .HasImpactAction(ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(25);
        }

        private static void ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            switch (level)
            {
                case 1:
                    Ability.ApplyCombatImpact(activator, target, targetLocation, SkillType.Rifle, 100, 15, 0, SavingThrow.Will, null, false);
                    break;
            }
        }
    }
}
