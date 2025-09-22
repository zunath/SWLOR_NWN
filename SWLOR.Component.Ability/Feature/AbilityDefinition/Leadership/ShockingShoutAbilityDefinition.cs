using SWLOR.Component.Ability.Contracts;
using SWLOR.Component.Ability.Enums;
using SWLOR.Component.Ability.Model;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using SWLOR.Shared.Domain.Enums;

namespace SWLOR.Component.Ability.Feature.AbilityDefinition.Leadership
{
    public class ShockingShoutAbilityDefinition : IAbilityListDefinition
    {
        private readonly ICombatService _combatService;
        private readonly IAbilityService _abilityService;
        private readonly ICombatPointService _combatPointService;
        private readonly IEnmityService _enmityService;

        public ShockingShoutAbilityDefinition(ICombatService combatService, IAbilityService abilityService, ICombatPointService combatPointService, IEnmityService enmityService)
        {
            _combatService = combatService;
            _abilityService = abilityService;
            _combatPointService = combatPointService;
            _enmityService = enmityService;
        }

        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            ShockingShout(builder);

            return builder.Build();
        }

        private void ShockingShout(IAbilityBuilder builder)
        {
            builder.Create(FeatType.ShockingShout, PerkType.ShockingShout)
                .Name("Shocking Shout")
                .Level(1)
                .HasRecastDelay(RecastGroup.ShockingShout, 180f)
                .HasActivationDelay(4f)
                .UnaffectedByHeavyArmor()
                .IsCastedAbility()
                .UsesAnimation(Animation.FireForgetTaunt)
                .HasImpactAction((activator, target, level, location) =>
                {
                    const float RangeMeters = 10f;
                    const int MaxTargets = 6;
                    var nth = 1;
                    var count = 0;

                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Fnf_Sound_Burst), activator);

                    var nearest = GetNearestCreature(CreatureType.IsAlive, 1, activator, nth);
                    while (GetIsObjectValid(nearest))
                    {

                        if (GetDistanceBetween(activator, nearest) > RangeMeters)
                        {
                            break;
                        }

                        if (GetIsReactionTypeHostile(nearest, activator))
                        {
                            count++;

                            var dc = _combatService.CalculateSavingThrowDC(activator, 14, 0, 0);
                            const float BaseDuration = 2f;
                            var bonusDuration = GetAbilityModifier(AbilityType.Social, activator) * 0.5f;
                            var duration = BaseDuration + bonusDuration;

                            var checkResult = WillSave(nearest, dc, SavingThrowType.None, activator);
                            if (checkResult == SavingThrowResultType.Failed)
                            {
                                ApplyEffectToObject(DurationType.Temporary, EffectStunned(), nearest, duration);
                                _abilityService.ApplyTemporaryImmunity(target, duration, ImmunityType.Stun);
                                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Head_Sonic), nearest);
                            }

                            _combatPointService.AddCombatPoint(activator, nearest, SkillType.Leadership, 3);
                            _enmityService.ModifyEnmity(activator, target, 650);
                        }

                        if (count > MaxTargets)
                        {
                            break;
                        }

                        nth++;
                        nearest = GetNearestCreature(CreatureType.IsAlive, 1, activator, nth);
                    }

                });
        }
    }
}
