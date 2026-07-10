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
    public class CripplingTalonsTechniqueAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var profile = InnateAbilityProfile.Mimicry;

            _builder
                .Create(FeatType.CripplingTalonsTechnique, profile.PlayerPerkType)
                .Name("Crippling Talons Technique")
                .HasActivationDelay(1.0f)
                .IsCastedAbility()
                .RequirementStamina(3)
                .HasRecastDelay(RecastGroup.CripplingTalons, 12f)
                .UsesAnimation(Animation.CrossCut)
                .IsSingleTargetAbility()
                .RequiresTarget()
                .IsHostileAbility()
                .HasImpactAction((activator, target, level, location) =>
                {
                    Ability.ApplyCombatImpact(
                        activator,
                        target,
                        location,
                        InnateAbility.ResolveSkillType(activator, profile),
                        10,
                        18,
                        null,
                        false,
                        statusEffectFactory: () => new VulnerableStatusEffect(8),
                        damageType: CombatDamageType.Physical,
                        statusResistanceType: ResistanceType.Trauma,
                        targetVisualEffect: VisualEffect.Vfx_Com_Blood_Crt_Red,
                        useNPCStatScaling: InnateAbility.ShouldUseNPCStatScaling(activator));
                })
                .SkillType(SkillType.Mimicry)
                .Level(1)
                .CombatImpactDamageAbility(AbilityType.Might)
                .MimicryTechnique(FeatType.CripplingTalons, 1, 1);

            return _builder.Build();
        }
    }
}
