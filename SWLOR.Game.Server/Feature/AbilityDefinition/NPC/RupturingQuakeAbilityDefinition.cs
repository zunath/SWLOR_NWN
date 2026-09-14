using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.NPC
{
    public class RupturingQuakeAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var profile = InnateAbilityProfile.CreaturePhysical;

            _builder
                .Create(FeatType.RupturingQuake, profile.PlayerPerkType)
                .Name("Rupturing Quake")
                .HasActivationDelay(3.0f)
                .HasRecastDelay(RecastGroup.RupturingQuake, 48f)
                .UsesAnimation(Animation.DoubleThrust)
                .IsCastedAbility()
                .HasMaxRange(9f)
                .IsAreaAbility()
                .IsHostileAbility()
                .RequirementStamina(10)
                .HasActivationTargetingSphere(
                    9f,
                    AbilityTargetingFlags.HarmsEnemies | AbilityTargetingFlags.OriginOnSelf)
                .HasImpactAction((activator, target, level, location) =>
                {
                    Ability.ApplyTelegraphedCombatImpact(
                        activator,
                        target,
                        location,
                        InnateAbility.ResolveSkillType(activator, profile),
                        22,
                        5,
                        typeof(KnockdownStatusEffect),
                        CombatImpactAreaShape.Sphere,
                        0.75f,
                        9f,
                        centerOnActivator: true,
                        damageType: CombatDamageType.Physical,
                        statusResistanceType: ResistanceType.Mobility,
                        targetVisualEffect: VisualEffect.Vfx_Imp_Pulse_Nature,
                        areaVisualEffect: VisualEffect.Vfx_Fnf_Screen_Shake,
                        maxTargets: 12,
                        useNPCStatScaling: InnateAbility.ShouldUseNPCStatScaling(activator));
                });

            return _builder.Build();
        }
    }
}
