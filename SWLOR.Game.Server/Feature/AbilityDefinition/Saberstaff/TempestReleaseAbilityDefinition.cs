using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Saberstaff
{
    public class TempestReleaseAbilityDefinition : IAbilityListDefinition
    {
        private const int BaseDamage = 20;
        private const int ForcePointStepSize = 10;
        private const int DamageBonusPerForcePointStep = 2;
        private const int MaximumForcePointDamageBonus = 20;

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            TempestRelease1(builder);

            return builder.Build();
        }

        private static void TempestRelease1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.TempestRelease1, PerkType.TempestRelease)
                .Name("Tempest Release")
                .Level(1)
                .SkillType(SkillType.Saberstaff)
                .IsAreaAbility()
                .HasActivationDelay(0f)
                .UsesAnimation(Animation.Whirlwind)
                .HasRecastDelay(RecastGroup.TempestRelease, 120f)
                .HasImpactAction(TempestRelease1ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(12);
        }

        private static void TempestRelease1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            var damage = BaseDamage + CalculateForcePointDamageBonus(Stat.GetCurrentFP(activator));
            Ability.ApplyCombatImpact(activator, target, targetLocation, SkillType.Saberstaff, damage, 0, null, true);
        }

        private static int CalculateForcePointDamageBonus(int currentFP)
        {
            if (currentFP <= 0)
                return 0;

            var bonus = currentFP / ForcePointStepSize * DamageBonusPerForcePointStep;
            return Math.Min(MaximumForcePointDamageBonus, bonus);
        }
    }
}
