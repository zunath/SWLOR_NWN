using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.NPC
{
    public class ScorchingBreathAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var profile = InnateAbilityProfile.CreaturePhysical;

            _builder
                .Create(FeatType.ScorchingBreath, profile.PlayerPerkType)
                .Name("Scorching Breath")
                .HasActivationDelay(1.5f)
                .HasRecastDelay(RecastGroup.ScorchingBreath, 22f)
                .IsCastedAbility()
                .HasMaxRange(8f)
                .IsAreaAbility()
                .RequiresTarget()
                .IsHostileAbility()
                .RequirementStamina(5)
                .HasImpactAction((activator, target, level, location) =>
                {
                    Ability.ApplyTelegraphedCombatImpact(
                        activator,
                        target,
                        location,
                        InnateAbility.ResolveSkillType(activator, profile),
                        14,
                        18,
                        typeof(BurnStatusEffect),
                        CombatImpactAreaShape.Cone,
                        0f,
                        8f,
                        5f,
                        centerOnActivator: !GetIsObjectValid(target),
                        damageType: CombatDamageType.Fire,
                        statusResistanceType: ResistanceType.Fire,
                        targetVisualEffect: VisualEffect.Vfx_Com_Hit_Fire,
                        areaVisualEffect: VisualEffect.Vfx_Fnf_Gas_Explosion_Fire,
                        useNPCStatScaling: InnateAbility.ShouldUseNPCStatScaling(activator));
                });

            return _builder.Build();
        }
    }
}
