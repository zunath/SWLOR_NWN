using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Rifle
{
    public class NeutralizingShotAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            NeutralizingShot1(builder);

            return builder.Build();
        }

        private static void NeutralizingShot1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.NeutralizingShot1, PerkType.NeutralizingShot)
                .Name("Neutralizing Shot")
                .Level(1)
                .HasActivationDelay(0f)
                .SkillType(SkillType.Rifle)
                .UsesImpactAnimation(Animation.PointPistol)
                .HasMaxRange(RifleAbilityRange.Standard)
                .IsSingleTargetAbility()
                .HasRecastDelay(RecastGroup.NeutralizingShot, 90f)
                .RequiresTarget()
                .HasImpactAction(NeutralizingShot1ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(10);
        }

        private static void NeutralizingShot1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.Rifle,
                30,
                12,
                typeof(DisorientedStatusEffect),
                false,
                afterSuccessfulHit: hitTarget => StatusEffect.RemoveFirstBeneficialCombatStatusEffect(hitTarget, false));
        }
    }
}
