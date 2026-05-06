using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.OneHanded
{
    public class SystemicShutdownAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            SystemicShutdown1(builder);

            return builder.Build();
        }

        private static void SystemicShutdown1(AbilityBuilder builder)
        {
            builder.Create(FeatType.SystemicShutdown1, PerkType.SystemicShutdown)
                .Name("Systemic Shutdown")
                .Level(1)
                .HasActivationDelay(3f)
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
                    Ability.ApplyCombatImpact(activator, target, targetLocation, SkillType.OneHanded, 15, 0, 0, SavingThrow.Will, StatusEffectType.Invalid, AbilityControlEffect.None, true);
                    break;
            }
        }
    }
}
