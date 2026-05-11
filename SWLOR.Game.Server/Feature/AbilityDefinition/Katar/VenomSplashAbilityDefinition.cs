using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Katar
{
    public class VenomSplashAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            VenomSplash1(builder);

            return builder.Build();
        }

        private static void VenomSplash1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.VenomSplash1, PerkType.VenomSplash)
                .Name("Venom Splash")
                .Level(1)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.VenomSplash, 60f)
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
                    Ability.ApplyTelegraphedCombatImpact(activator, target, targetLocation, SkillType.Katar, 18, 30, typeof(PoisonStatusEffect), CombatImpactAreaShape.Cone, 0.25f, 5f, 5f, damageType: CombatDamageType.Poison);
                    break;
            }
        }
    }
}
