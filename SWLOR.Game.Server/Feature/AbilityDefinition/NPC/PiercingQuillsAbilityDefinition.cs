using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.NPC
{
    public class PiercingQuillsAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var profile = InnateAbilityProfile.CreaturePhysical;

            _builder
                .Create(FeatType.PiercingQuills, profile.PlayerPerkType)
                .Name("Piercing Quills")
                .HasActivationDelay(1.6f)
                .HasRecastDelay(RecastGroup.PiercingQuills, 18f)
                .UsesAnimation(Animation.ThrowGrenade)
                .IsCastedAbility()
                .HasMaxRange(8f)
                .IsAreaAbility()
                .RequiresTarget()
                .IsHostileAbility()
                .RequirementStamina(5)
                .HasActivationTargetingCone(
                    8f,
                    5f,
                    AbilityTargetingFlags.HarmsEnemies | AbilityTargetingFlags.OriginOnSelf)
                .HasImpactAction((activator, target, level, location) =>
                {
                    Ability.ApplyTelegraphedCombatImpact(
                        activator,
                        target,
                        location,
                        InnateAbility.ResolveSkillType(activator, profile),
                        16,
                        18,
                        typeof(BleedStatusEffect),
                        CombatImpactAreaShape.Cone,
                        0f,
                        8f,
                        5f,
                        centerOnActivator: !GetIsObjectValid(target),
                        damageType: CombatDamageType.Physical,
                        statusResistanceType: ResistanceType.Trauma,
                        targetVisualEffect: VisualEffect.Vfx_Imp_Wallspike,
                        areaVisualEffect: VisualEffect.Vfx_Fnf_Screen_Bump,
                        useNPCStatScaling: InnateAbility.ShouldUseNPCStatScaling(activator));
                });

            return _builder.Build();
        }
    }
}
