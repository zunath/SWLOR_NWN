using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.NPC
{
    public class InfernoBlastAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var profile = InnateAbilityProfile.CreaturePhysical;

            _builder
                .Create(FeatType.InfernoBlast, profile.PlayerPerkType)
                .Name("Inferno Blast")
                .HasActivationDelay(2.5f)
                .HasRecastDelay(RecastGroup.InfernoBlast, 34f)
                .IsCastedAbility()
                .HasMaxRange(10f)
                .IsAreaAbility()
                .RequiresTarget()
                .IsHostileAbility()
                .RequirementStamina(8)
                .HasActivationTargetingCone(
                    10f,
                    7f,
                    AbilityTargetingFlags.HarmsEnemies | AbilityTargetingFlags.OriginOnSelf)
                .HasImpactAction((activator, target, level, location) =>
                {
                    Ability.ApplyTelegraphedCombatImpact(
                        activator,
                        target,
                        location,
                        InnateAbility.ResolveSkillType(activator, profile),
                        26,
                        24,
                        typeof(BurnStatusEffect),
                        CombatImpactAreaShape.Cone,
                        0.5f,
                        10f,
                        7f,
                        centerOnActivator: !GetIsObjectValid(target),
                        damageType: CombatDamageType.Fire,
                        statusResistanceType: ResistanceType.Fire,
                        targetVisualEffect: VisualEffect.Vfx_Com_Hit_Fire,
                        areaVisualEffect: VisualEffect.Fnf_Fireball,
                        maxTargets: 8,
                        useNPCStatScaling: InnateAbility.ShouldUseNPCStatScaling(activator));
                });

            return _builder.Build();
        }
    }
}
