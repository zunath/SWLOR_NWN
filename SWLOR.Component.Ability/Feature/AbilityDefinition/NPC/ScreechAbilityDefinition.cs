using SWLOR.Component.Ability.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using SWLOR.Shared.Domain.Enums;
using SWLOR.Shared.Domain.Model;

namespace SWLOR.Component.Ability.Feature.AbilityDefinition.NPC
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
                .DisplaysVisualEffectWhenActivating(VisualEffect.Vfx_Dur_Aura_Blue)
                .HasRecastDelay(RecastGroup.Screech, 120f)
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
                            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Pulse_Negative), nearest);

                            SendMessageToPC(nearest, "The screech disorients you!");
                        }

                        count++;
                        nearest = GetNearestCreature(CreatureType.IsAlive, 1, activator, count);
                    }
                });
        }
    }
}
