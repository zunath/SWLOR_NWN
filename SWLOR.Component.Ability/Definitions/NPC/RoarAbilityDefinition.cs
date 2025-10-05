using SWLOR.Component.Ability.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Ability.Enums;
using SWLOR.Shared.Domain.Ability.ValueObjects;
using SWLOR.Shared.Domain.Perk.Enums;

namespace SWLOR.Component.Ability.Definitions.NPC
{
    public class RoarAbilityDefinition: IAbilityListDefinition
    {

        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            Roar(builder);
            return builder.Build();
        }

        private void Roar(IAbilityBuilder builder)
        {
            builder.Create(FeatType.Roar, PerkType.Invalid)
                .Name("Roar")
                .HasActivationDelay(2f)
                .IsCastedAbility()
                .RequirementStamina(6)
                .UnaffectedByHeavyArmor()
                .HasRecastDelay(RecastGroupType.Roar, 60f)
                .HasImpactAction((activator, target, level, location) =>
                {
                    const float Distance = 2.5f;
                    const int DecreaseBy = 3;

                    var count = 1;
                    var creature = GetNearestCreature(CreatureType.IsAlive, 1, activator, count);
                    while (GetIsObjectValid(creature) &&
                           GetDistanceBetween(creature, activator) <= Distance)
                    {
                        ApplyEffectToObject(DurationType.Temporary, EffectACDecrease(DecreaseBy, ItemPropertyArmorClassModiferType.Natural), creature, 20f);

                        count++;
                        creature = GetNearestCreature(CreatureType.IsAlive, 1, activator, count);
                    }

                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffectType.Vfx_Fnf_Howl_Mind), activator);
                });
        }

    }
}
