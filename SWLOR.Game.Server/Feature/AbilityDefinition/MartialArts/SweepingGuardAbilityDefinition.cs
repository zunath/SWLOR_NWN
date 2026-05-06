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
    public class SweepingGuardAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            SweepingGuard1(builder);

            return builder.Build();
        }

        private static void SweepingGuard1(AbilityBuilder builder)
        {
            builder.Create(FeatType.SweepingGuard1, PerkType.SweepingGuard)
                .Name("Sweeping Guard")
                .Level(1)
                .HasActivationDelay(0f)
                .HasImpactAction(ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(10);
        }

        private static void ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            switch (level)
            {
                case 1:
                    Ability.ApplyCombatImpact(activator, target, targetLocation, SkillType.MartialArts, 18, 2, 16, SavingThrow.Reflex, StatusEffectType.Invalid, AbilityControlEffect.Knockdown, true);
                    break;
            }
        }
    }
}
