using System.Collections.Generic;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Rifle
{
    public class ExposeWeakPointAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ExposeWeakPoint1(builder);

            return builder.Build();
        }

        private static void ExposeWeakPoint1(AbilityBuilder builder)
        {
            builder.Create(FeatType.ExposeWeakPoint1, PerkType.ExposeWeakPoint)
                .Name("Expose Weak Point")
                .Level(1)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.ExposeWeakPoint, 75f)
                .RequiresTarget()
                .HasImpactAction(ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(8);
        }

        private static void ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            switch (level)
            {
                case 1:
                    Ability.ApplyCombatImpact(activator, target, targetLocation, SkillType.Rifle, 20, 12, typeof(ExposeWeakPointStatusEffect), false);
                    break;
            }
        }
    }
}
