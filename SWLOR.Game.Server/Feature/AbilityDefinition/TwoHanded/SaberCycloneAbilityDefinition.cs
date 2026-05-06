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
    public class SaberCycloneAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            SaberCyclone1(builder);

            return builder.Build();
        }

        private static void SaberCyclone1(AbilityBuilder builder)
        {
            builder.Create(FeatType.SaberCyclone1, PerkType.SaberCyclone)
                .Name("Saber Cyclone")
                .Level(1)
                .HasActivationDelay(0f)
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
                    Ability.ApplyCombatImpact(activator, target, targetLocation, SkillType.TwoHanded, 25, 6, 0, SavingThrow.Will, StatusEffectType.Invalid, AbilityControlEffect.None, true);
                    break;
            }
        }
    }
}
