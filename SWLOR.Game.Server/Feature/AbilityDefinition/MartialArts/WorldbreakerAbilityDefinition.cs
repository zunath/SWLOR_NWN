using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.MartialArts
{
    public class WorldbreakerAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            Worldbreaker1(builder);

            return builder.Build();
        }

        private static void Worldbreaker1(AbilityBuilder builder)
        {
            builder.Create(FeatType.Worldbreaker1, PerkType.Worldbreaker)
                .Name("Worldbreaker")
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
                    Ability.ApplyCombatImpact(activator, target, targetLocation, SkillType.MartialArts, 45, 4, 18, SavingThrow.Reflex, StatusEffectType.Invalid, AbilityControlEffect.Knockdown, true);
                    break;
            }
        }
    }
}
