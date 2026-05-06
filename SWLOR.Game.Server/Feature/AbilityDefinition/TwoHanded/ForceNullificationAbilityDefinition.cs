using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.TwoHanded
{
    public class ForceNullificationAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ForceNullification1(builder);

            return builder.Build();
        }

        private static void ForceNullification1(AbilityBuilder builder)
        {
            builder.Create(FeatType.ForceNullification1, PerkType.ForceNullification)
                .Name("Force Nullification")
                .Level(1)
                .HasActivationDelay(0f)
                .RequiresTarget()
                .HasImpactAction(ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(12);
        }

        private static void ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            switch (level)
            {
                case 1:
                    Ability.ApplyCombatImpact(activator, target, targetLocation, SkillType.TwoHanded, 22, 8, 0, SavingThrow.Will, StatusEffectType.Invalid, AbilityControlEffect.None, false);
                    break;
            }
        }
    }
}
