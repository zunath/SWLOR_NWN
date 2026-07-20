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
    public class SeismicSlamTechniqueAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var profile = InnateAbilityProfile.Mimicry;

            _builder
                .Create(FeatType.SeismicSlamTechnique, profile.PlayerPerkType)
                .Name("Seismic Slam")
                .HasActivationDelay(2.0f)
                .HasRecastDelay(RecastGroup.SeismicSlam, 24f)
                .UsesAnimation(Animation.DoubleThrust)
                .IsCastedAbility()
                .HasMaxRange(6f)
                .IsAreaAbility()
                .IsHostileAbility()
                .RequirementStamina(8)
                .HasImpactAction((activator, target, level, location) =>
                {
                    Ability.ApplyTelegraphedCombatImpact(
                        activator,
                        target,
                        location,
                        InnateAbility.ResolveSkillType(activator, profile),
                        28,
                        6,
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
                })
                .SkillType(SkillType.Mimicry)
                .Level(1)
                .CombatImpactDamageAbility(AbilityType.Might)
                .MimicryTechnique(FeatType.SeismicSlam, 27, 3)
                .MimicryElement(CombatDamageType.Physical)
                .HasTargetingSphere(
                    Spell.SeismicSlamTechnique,
                    6f,
                    AbilityTargetingFlags.HarmsEnemies | AbilityTargetingFlags.OriginOnSelf);

            return _builder.Build();
        }
    }
}
