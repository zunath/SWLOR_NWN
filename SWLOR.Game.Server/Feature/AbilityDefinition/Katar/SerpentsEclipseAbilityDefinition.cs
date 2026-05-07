using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Katar
{
    public class SerpentsEclipseAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            SerpentsEclipse1(builder);

            return builder.Build();
        }

        private static void SerpentsEclipse1(AbilityBuilder builder)
        {
            builder.Create(FeatType.SerpentsEclipse1, PerkType.SerpentsEclipse)
                .Name("Serpent's Eclipse")
                .Level(1)
                .HasActivationDelay(2f)
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
                    Ability.ApplyCombatImpact(activator, target, targetLocation, SkillType.Katar, 25, 0, 18, SavingThrow.Fortitude, StatusEffectType.Poison, AbilityControlEffect.None, true);
                    break;
            }
        }
    }
}
