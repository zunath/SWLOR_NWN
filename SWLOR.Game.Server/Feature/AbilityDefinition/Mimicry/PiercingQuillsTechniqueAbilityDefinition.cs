using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition.NPC;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Mimicry
{
    public class PiercingQuillsTechniqueAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var profile = InnateAbilityProfile.Mimicry;

            _builder
                .Create(FeatType.PiercingQuillsTechnique, profile.PlayerPerkType)
                .Name("Piercing Quills")
                .HasActivationDelay(1.6f)
                .HasRecastDelay(RecastGroup.PiercingQuills, 18f)
                .UsesAnimation(Animation.ThrowGrenade)
                .IsCastedAbility()
                .IsAreaAbility()
                .IsHostileAbility()
                .RequirementStamina(5)
                .HasImpactAction((activator, target, level, location) =>
                {
                    Ability.ApplyTelegraphedCombatImpact(
                        activator,
                        target,
                        location,
                        InnateAbility.ResolveSkillType(activator, profile),
                        0,
                        30,
                        typeof(SunderStatusEffect),
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
                })
                .SkillType(SkillType.Mimicry)
                .Level(1)
                .CombatImpactDamageAbility(AbilityType.Perception)
                .MimicryTechnique(FeatType.PiercingQuills, 13, 2)
                .MimicryElement(CombatDamageType.Physical)
                .HasTargetingCone(
                    Spell.PiercingQuillsTechnique,
                    8f,
                    5f,
                    AbilityTargetingFlags.HarmsEnemies | AbilityTargetingFlags.OriginOnSelf | AbilityTargetingFlags.BackOffsetOrigin);

            return _builder.Build();
        }
    }
}
