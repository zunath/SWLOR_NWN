using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.NPC
{
    public class SeismicSlamAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var profile = InnateAbilityProfile.CreaturePhysical;

            _builder
                .Create(FeatType.SeismicSlam, profile.PlayerPerkType)
                .Name("Seismic Slam")
                .HasActivationDelay(2.0f)
                .HasRecastDelay(RecastGroup.SeismicSlam, 28f)
                .UsesAnimation(Animation.DoubleThrust)
                .IsCastedAbility()
                .HasMaxRange(6f)
                .IsAreaAbility()
                .IsHostileAbility()
                .RequirementStamina(6)
                .HasActivationTargetingSphere(
                    6f,
                    AbilityTargetingFlags.HarmsEnemies | AbilityTargetingFlags.OriginOnSelf)
                .HasImpactAction((activator, target, level, location) =>
                {
                    Ability.ApplyTelegraphedCombatImpact(
                        activator,
                        target,
                        location,
                        InnateAbility.ResolveSkillType(activator, profile),
                        10,
                        3,
                        typeof(KnockdownStatusEffect),
                        CombatImpactAreaShape.Sphere,
                        0.25f,
                        6f,
                        centerOnActivator: true,
                        damageType: CombatDamageType.Physical,
                        statusResistanceType: ResistanceType.Mobility,
                        targetVisualEffect: VisualEffect.Vfx_Imp_Pulse_Nature,
                        areaVisualEffect: VisualEffect.Vfx_Fnf_Screen_Shake,
                        useNPCStatScaling: InnateAbility.ShouldUseNPCStatScaling(activator));
                });

            return _builder.Build();
        }
    }
}
