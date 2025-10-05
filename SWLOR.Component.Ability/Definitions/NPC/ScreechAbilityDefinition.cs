using SWLOR.Component.Ability.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Ability.Enums;
using SWLOR.Shared.Domain.Ability.ValueObjects;
using SWLOR.Shared.Domain.Perk.Enums;

namespace SWLOR.Component.Ability.Definitions.NPC
{
    public class ScreechAbilityDefinition: IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            Screech(builder);

            return builder.Build();
        }

        private void Screech(IAbilityBuilder builder)
        {
            builder.Create(FeatType.Screech, PerkType.Invalid)
                .Name("Screech")
                .HasActivationDelay(4.0f)
                .DisplaysVisualEffectWhenActivating(VisualEffectType.Vfx_Dur_Aura_Blue)
                .HasRecastDelay(RecastGroupType.Screech, 120f)
                .IsCastedAbility()
                .RequirementStamina(10)
                .HasImpactAction((activator, target, level, location) =>
                {
                    var count = 1;
                    var nearest = GetNearestCreature(CreatureType.IsAlive, 1, activator, count);
                    while (GetIsObjectValid(nearest) && GetDistanceBetween(activator, nearest) <= 30f)
                    {
                        if (GetIsEnemy(nearest, activator))
                        {
                            const float Duration = 90f;

                            ApplyEffectToObject(DurationType.Temporary, EffectACDecrease(10), nearest, Duration);
                            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffectType.Vfx_Imp_Pulse_Negative), nearest);

                            SendMessageToPC(nearest, "The screech disorients you!");
                        }

                        count++;
                        nearest = GetNearestCreature(CreatureType.IsAlive, 1, activator, count);
                    }
                });
        }
    }
}
