using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
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
            builder
                .Create(FeatType.SerpentsEclipse1, PerkType.SerpentsEclipse)
                .Name("Serpent's Eclipse")
                .Level(1)
                .HasActivationDelay(2f)
                .UsesAnimation(Animation.CrossCut)
                .HasRecastDelay(RecastGroup.Capstone, CapstoneAbility.RecastDelaySeconds)
                .HasImpactAction(SerpentsEclipse1ImpactAction)
                .HasTargetingSphere(
                    Spell.SerpentsEclipse1,
                    5f,
                    AbilityTargetingFlags.HarmsEnemies)
                .SkillType(SkillType.Katar)
                .IsAreaAbility()
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(CapstoneAbility.StaminaCost);
        }

        private static void SerpentsEclipse1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.Katar,
                20,
                45,
                typeof(PoisonStatusEffect),
                true,
                additionalStatusEffects: new[] { typeof(DisorientedStatusEffect) },
                damageType: CombatDamageType.Poison,
                baseDamageAdjustment: creature => IsPoisonedOrDisoriented(creature) ? 15 : 0);
        }

        private static bool IsPoisonedOrDisoriented(uint target)
        {
            return StatusEffect.HasStatusEffect(target, typeof(PoisonStatusEffect)) ||
                   StatusEffect.HasStatusEffect(target, typeof(DisorientedStatusEffect));
        }
    }
}
