using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.NPC
{
    public class DisorientingScreechAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var profile = InnateAbilityProfile.CreaturePhysical;

            _builder
                .Create(FeatType.DisorientingScreech, profile.PlayerPerkType)
                .Name("Disorienting Screech")
                .HasActivationDelay(2.0f)
                .HasRecastDelay(RecastGroup.DisorientingScreech, 36f)
                .UsesAnimation(Animation.FireForgetTaunt)
                .IsCastedAbility()
                .HasMaxRange(9f)
                .IsAreaAbility()
                .IsHostileAbility()
                .RequirementStamina(7)
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
                        8,
                        24,
                        typeof(DisorientedStatusEffect),
                        CombatImpactAreaShape.Sphere,
                        0.25f,
                        9f,
                        centerOnActivator: true,
                        damageType: CombatDamageType.Sonic,
                        statusResistanceType: ResistanceType.Mind,
                        targetVisualEffect: VisualEffect.Vfx_Imp_Dazed_S,
                        areaVisualEffect: VisualEffect.Vfx_Fnf_Howl_Odd,
                        maxTargets: 10,
                        useNPCStatScaling: InnateAbility.ShouldUseNPCStatScaling(activator));
                });

            return _builder.Build();
        }
    }
}
