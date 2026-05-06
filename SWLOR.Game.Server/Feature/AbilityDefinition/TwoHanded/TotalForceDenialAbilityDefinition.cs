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
    public class TotalForceDenialAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            TotalForceDenial1(builder);

            return builder.Build();
        }

        private static void TotalForceDenial1(AbilityBuilder builder)
        {
            builder.Create(FeatType.TotalForceDenial1, PerkType.TotalForceDenial)
                .Name("Total Force Denial")
                .Level(1)
                .HasActivationDelay(2f)
                .HasImpactAction(ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(14);
        }

        private static void ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            switch (level)
            {
                case 1:
                    Ability.ApplyCombatImpact(activator, target, targetLocation, SkillType.TwoHanded, 28, 12, 16, SavingThrow.Will, StatusEffectType.Invalid, AbilityControlEffect.None, true);
                    break;
            }
        }
    }
}
