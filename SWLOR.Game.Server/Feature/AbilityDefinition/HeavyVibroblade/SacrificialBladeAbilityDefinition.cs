using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.HeavyVibroblade
{
    public class SacrificialBladeAbilityDefinition : HeavyVibrobladeActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            SacrificialBlade1(builder);

            return builder.Build();
        }

        private static void SacrificialBlade1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.SacrificialBlade1, PerkType.SacrificialBlade)
                .Name("Sacrificial Blade")
                .Level(1)
                .HasActivationDelay(0f)
                .UsesAnimation(Animation.DoubleStrike)
                .HasRecastDelay(RecastGroup.SacrificialBlade, 120f)
                .RequiresTarget()
                .HasImpactAction(SacrificialBlade1ImpactAction)
                .SkillType(SkillType.HeavyVibroblade)
                .IsCastedAbility()
                .IsHostileAbility()
                .IsSingleTargetAbility()
                .BreaksStealth()
                .RequirementStamina(6);
        }

        private static void SacrificialBlade1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            SacrificeHitPoints(activator, 8);
            var damage = Ability.ApplyCombatImpact(activator, target, targetLocation, SkillType.HeavyVibroblade, 25, 0, null, false);
            if (damage > 0)
            {
                ApplyEssenceHunter(activator, target);
            }
        }
    }
}
