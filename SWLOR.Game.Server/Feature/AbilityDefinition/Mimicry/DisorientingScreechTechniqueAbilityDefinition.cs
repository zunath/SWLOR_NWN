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
    public class DisorientingScreechTechniqueAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var profile = InnateAbilityProfile.Mimicry;

            _builder
                .Create(FeatType.DisorientingScreechTechnique, profile.PlayerPerkType)
                .Name("Disorienting Screech")
                .HasActivationDelay(2.0f)
                .HasRecastDelay(RecastGroup.DisorientingScreech, 24f)
                .UsesAnimation(Animation.FireForgetTaunt)
                .IsCastedAbility()
                .HasMaxRange(9f)
                .IsAreaAbility()
                .IsHostileAbility()
                .RequirementStamina(8)
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
                        0,
                        30,
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
                })
                .SkillType(SkillType.Mimicry)
                .Level(1)
                .CombatImpactDamageAbility(AbilityType.Willpower)
                .MimicryTechnique(FeatType.DisorientingScreech, 0, 3)
                .MimicryElement(CombatDamageType.Sonic)
                .HasTargetingSphere(
                    Spell.DisorientingScreechTechnique,
                    9f,
                    AbilityTargetingFlags.HarmsEnemies | AbilityTargetingFlags.OriginOnSelf);

            return _builder.Build();
        }
    }
}
