using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition.NPC;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Mimicry
{
    public class InfernoBlastTechniqueAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var profile = InnateAbilityProfile.Mimicry;

            _builder
                .Create(FeatType.InfernoBlastTechnique, profile.PlayerPerkType)
                .Name("Inferno Blast")
                .HasActivationDelay(2.5f)
                .HasRecastDelay(RecastGroup.InfernoBlast, 30f)
                .UsesAnimation(Animation.CastOutAnimation)
                .IsCastedAbility()
                .HasMaxRange(10f)
                .IsAreaAbility()
                .IsHostileAbility()
                .RequirementStamina(10)
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
                        40,
                        30,
                        typeof(BurnStatusEffect),
                        CombatImpactAreaShape.Cone,
                        0.5f,
                        10f,
                        7f,
                        centerOnActivator: !GetIsObjectValid(target),
                        damageType: CombatDamageType.Fire,
                        statusResistanceType: ResistanceType.Fire,
                        damagePercentAdjustment: InnateAbility.ComboBonus(50, typeof(BurnStatusEffect)),
                        targetVisualEffect: VisualEffect.Vfx_Com_Hit_Fire,
                        areaVisualEffect: VisualEffect.Fnf_Fireball,
                        maxTargets: 8,
                        useNPCStatScaling: InnateAbility.ShouldUseNPCStatScaling(activator));
                })
                .SkillType(SkillType.Mimicry)
                .Level(1)
                .CombatImpactDamageAbility(AbilityType.Might)
                .MimicryTechnique(FeatType.InfernoBlast, 15, 3)
                .MimicryElement(CombatDamageType.Fire)
                .HasTargetingCone(
                    Spell.InfernoBlastTechnique,
                    10f,
                    7f,
                    AbilityTargetingFlags.HarmsEnemies | AbilityTargetingFlags.OriginOnSelf);

            return _builder.Build();
        }
    }
}
