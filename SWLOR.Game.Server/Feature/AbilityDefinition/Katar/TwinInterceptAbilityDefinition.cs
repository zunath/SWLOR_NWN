using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Katar
{
    public class TwinInterceptAbilityDefinition : WeaponActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            builder
                .Create(FeatType.TwinIntercept1, PerkType.TwinIntercept)
                .Name("Twin Intercept")
                .Level(1)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.TwinIntercept, 120f)
                .SkillType(SkillType.Katar)
                .IsSingleTargetAbility()
                .RequiresTarget()
                .HasMaxRange(6f)
                .HasCustomValidation((activator, target, _, _) =>
                    AbilityTargeting.ValidateFriendlyTarget(activator, target, false))
                .HasImpactAction((activator, target, level, targetLocation) =>
                {
                    var shield = Math.Max(1, (int)Math.Ceiling(GetMaxHitPoints(activator) * 0.2f));
                    ApplyEffectToObject(DurationType.Temporary, EffectTemporaryHitpoints(shield), target, 8f);
                    StatusEffect.ApplyStatusEffect(activator, target, typeof(TwinInterceptStatusEffect), 8f);
                    ModifyEnmityNearAlly(activator, target, 450);
                })
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementStamina(10);

            return builder.Build();
        }

        private static void ModifyEnmityNearAlly(uint activator, uint ally, int amount)
        {
            const float Radius = 8f;

            var location = GetLocation(ally);
            var creature = GetFirstObjectInShape(Shape.Sphere, Radius, location, true);

            while (GetIsObjectValid(creature))
            {
                if (GetIsReactionTypeHostile(creature, activator))
                    Enmity.ModifyEnmity(activator, creature, amount);

                creature = GetNextObjectInShape(Shape.Sphere, Radius, location, true);
            }
        }
    }
}
