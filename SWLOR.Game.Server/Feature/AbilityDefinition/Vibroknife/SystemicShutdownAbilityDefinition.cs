using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Vibroknife
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
                .HasRecastDelay(RecastGroup.SystemicShutdown, 90f)
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
                    Ability.ApplyTelegraphedCombatImpact(activator, target, targetLocation, SkillType.Vibroknife, 15, 15, typeof(ToxinStatusEffect), CombatImpactAreaShape.Sphere, 0.25f, 5f, additionalStatusEffects: new[] { typeof(WeakenedStatusEffect), typeof(HamstringStatusEffect), typeof(ExhaustedStatusEffect), typeof(DisorientedStatusEffect) }, damageType: CombatDamageType.Poison);
                    break;
            }
        }
    }
}
