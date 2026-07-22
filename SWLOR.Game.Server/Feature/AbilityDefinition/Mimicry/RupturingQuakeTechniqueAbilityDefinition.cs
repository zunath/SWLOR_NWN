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
    public class RupturingQuakeTechniqueAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var profile = InnateAbilityProfile.Mimicry;

            _builder
                .Create(FeatType.RupturingQuakeTechnique, profile.PlayerPerkType)
                .Name("Rupturing Quake")
                .HasActivationDelay(3.0f)
                .HasRecastDelay(RecastGroup.RupturingQuake, 30f)
                .UsesAnimation(Animation.DoubleThrust)
                .IsCastedAbility()
                .HasMaxRange(9f)
                .IsAreaAbility()
                .IsHostileAbility()
                .RequirementStamina(10)
                .HasImpactAction((activator, target, level, location) =>
                {
                    Ability.ApplyTelegraphedCombatImpact(
                        activator,
                        target,
                        location,
                        InnateAbility.ResolveSkillType(activator, profile),
                        40,
                        6,
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
                        afterSuccessfulHit: hitTarget =>
                            StatusEffect.ApplyStatusEffect<SunderStatusEffect>(activator, hitTarget, 30f, CombatDamageType.Physical),
                        useNPCStatScaling: InnateAbility.ShouldUseNPCStatScaling(activator));
                })
                .SkillType(SkillType.Mimicry)
                .Level(1)
                .CombatImpactDamageAbility(AbilityType.Might)
                .MimicryTechnique(FeatType.RupturingQuake, 26, 3)
                .MimicryElement(CombatDamageType.Physical)
                .HasTargetingSphere(
                    Spell.RupturingQuakeTechnique,
                    9f,
                    AbilityTargetingFlags.HarmsEnemies | AbilityTargetingFlags.OriginOnSelf);

            return _builder.Build();
        }
    }
}
