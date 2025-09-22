using SWLOR.Component.Ability.Contracts;
using SWLOR.Component.Ability.Enums;
using SWLOR.Component.Ability.Model;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Component.Ability.Feature.AbilityDefinition.Force
{
    public class ForceRageAbilityDefinition : IAbilityListDefinition
    {
        private readonly ICombatPointService _combatPointService;
        private readonly IEnmityService _enmityService;
        private readonly IStatusEffectService _statusEffectService;

        public ForceRageAbilityDefinition(ICombatPointService combatPointService, IEnmityService enmityService, IStatusEffectService statusEffectService)
        {
            _combatPointService = combatPointService;
            _enmityService = enmityService;
            _statusEffectService = statusEffectService;
        }

        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            ForceRage1(builder);
            ForceRage2(builder);

            return builder.Build();
        }

        private void ForceRage1(IAbilityBuilder builder)
        {
            builder.Create(FeatType.ForceRage1, PerkType.ForceRage)
                .Name("Force Rage I")
                .Level(1)
                .HasRecastDelay(RecastGroup.ForceRage, 30f)
                .RequirementFP(4)
                .IsCastedAbility()
                .HasMaxRange(10f)
                .UsesAnimation(Animation.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating()
                .HasImpactAction((activator, target, level, location) =>
                {
                    var willpowerBonus = GetAbilityModifier(AbilityType.Willpower, activator) * 30f;

                    var statusEffectService = _statusEffectService;
                    var combatPointService = _combatPointService;
                    var enmityService = _enmityService;

                    statusEffectService.Apply(activator, target, StatusEffectType.ForceRage1, 60f * 15f + willpowerBonus);
                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Negative_Energy), target);

                    combatPointService.AddCombatPointToAllTagged(activator, SkillType.Force, 3);
                    enmityService.ModifyEnmityOnAll(activator, 250 * level);
                });
        }

        private void ForceRage2(IAbilityBuilder builder)
        {
            builder.Create(FeatType.ForceRage2, PerkType.ForceRage)
                .Name("Force Rage II")
                .Level(2)
                .HasRecastDelay(RecastGroup.ForceRage, 30f)
                .RequirementFP(6)
                .IsCastedAbility()
                .HasMaxRange(10f)
                .UsesAnimation(Animation.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating()
                .HasImpactAction((activator, target, level, location) =>
                {
                    var willpowerBonus = GetAbilityModifier(AbilityType.Willpower, activator) * 30f;

                    var statusEffectService = _statusEffectService;
                    var combatPointService = _combatPointService;
                    var enmityService = _enmityService;

                    statusEffectService.Apply(activator, target, StatusEffectType.ForceRage2, 60f * 15f + willpowerBonus);
                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Negative_Energy), target);

                    combatPointService.AddCombatPointToAllTagged(activator, SkillType.Force, 3);
                    enmityService.ModifyEnmityOnAll(activator, 250 * level);
                });
        }
    }
}
