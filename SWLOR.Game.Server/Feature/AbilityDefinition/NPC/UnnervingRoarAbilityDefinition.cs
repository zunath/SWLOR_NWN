using System.Collections.Generic;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.AIService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.NPC
{
    public class UnnervingRoarAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var profile = InnateAbilityProfile.CreaturePhysical;

            _builder
                .Create(FeatType.UnnervingRoar, profile.PlayerPerkType)
                .Name("Unnerving Roar")
                .HasActivationDelay(2.0f)
                .HasRecastDelay(RecastGroup.UnnervingRoar, 30f)
                .UsesAnimation(Animation.FireForgetTaunt)
                .IsCastedAbility()
                .HasMaxRange(8f)
                .IsAreaAbility()
                .IsHostileAbility()
                .RequirementStamina(6)
                .HasActivationTargetingSphere(
                    8f,
                    AbilityTargetingFlags.HarmsEnemies | AbilityTargetingFlags.OriginOnSelf)
                // Waits for the creature's own target to be within range and sight, not just any enemy nearby.
                .HasAITarget(AITarget.SelfWithEnemyWithinRange(8f))
                .HasImpactAction((activator, target, level, location) =>
                {
                    // No hit roll: Evasion can't dodge the debuff that lowers it. Leaving the area
                    // avoids it, and Mind resistance shortens it.
                    Ability.ApplyTelegraphedCombatImpact(
                        activator,
                        target,
                        location,
                        InnateAbility.ResolveSkillType(activator, profile),
                        0,
                        15,
                        typeof(UnnervedStatusEffect),
                        CombatImpactAreaShape.Sphere,
                        0f,
                        8f,
                        centerOnActivator: true,
                        damageType: CombatDamageType.Physical,
                        statusResistanceType: ResistanceType.Mind,
                        targetVisualEffect: VisualEffect.Vfx_Imp_Fear_S,
                        areaVisualEffect: VisualEffect.Vfx_Fnf_Howl_War_Cry,
                        enmityBonus: 8,
                        useNPCStatScaling: InnateAbility.ShouldUseNPCStatScaling(activator),
                        resolvesHit: false);
                });

            return _builder.Build();
        }
    }
}
