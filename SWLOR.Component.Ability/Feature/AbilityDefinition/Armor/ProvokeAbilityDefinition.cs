using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Ability.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Ability.Enums;
using SWLOR.Shared.Domain.Ability.ValueObjects;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Perk.Enums;

namespace SWLOR.Component.Ability.Feature.AbilityDefinition.Armor
{
    public class ProvokeAbilityDefinition: IAbilityListDefinition
    {
        private readonly IServiceProvider _serviceProvider;

        public ProvokeAbilityDefinition(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        // Lazy-loaded services to break circular dependencies
        private IEnmityService EnmityService => _serviceProvider.GetRequiredService<IEnmityService>();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            Provoke(builder);
            Provoke2(builder);

            return builder.Build();
        }

        private string Validation(uint target)
        {
            if (GetIsPC(target))
            {
                return "This ability cannot be used on players.";
            }

            return string.Empty;
        }

        private void Impact(uint activator, uint target, int enmity)
        {
            if (!LineOfSightObject(activator, target))
                return;

            EnmityService.ModifyEnmity(activator, target, enmity);
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffectType.Vfx_Fnf_Howl_Odd), target);
        }

        private void Provoke(IAbilityBuilder builder)
        {
            builder.Create(FeatType.Provoke1, PerkType.Provoke)
                .Name("Provoke")
                .Level(1)
                .HasRecastDelay(RecastGroupType.Provoke, 10f)
                .HasActivationDelay(1f)
                .UsesAnimation(AnimationType.FireForgetTaunt)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasMaxRange(15f)
                .HasCustomValidation((_, target, _, _) => Validation(target))
                .HasImpactAction((activator, target, _, _) =>
                {
                    var enmityBonus = GetAbilityScore(activator, AbilityType.Vitality) * 50;
                    Impact(activator, target, 700 + enmityBonus);
                });
        }

        private void Provoke2(IAbilityBuilder builder)
        {
            builder.Create(FeatType.Provoke2, PerkType.Provoke)
                .Name("Provoke II")
                .Level(2)
                .HasRecastDelay(RecastGroupType.Provoke2, 20f)
                .HasActivationDelay(1f)
                .UsesAnimation(AnimationType.FireForgetTaunt)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasMaxRange(15f)
                .HasCustomValidation((_, target, _, _) => Validation(target))
                .HasImpactAction((activator, _, _, location) =>
                {
                    var nth = 1;
                    var nearest = GetNearestCreatureToLocation(CreatureType.IsAlive, true, location, nth);

                    while (GetIsObjectValid(nearest))
                    {
                        if (GetDistanceBetweenLocations(GetLocation(nearest), location) > 8f)
                            break;

                        if (!GetIsPC(nearest))
                        {
                            var enmityBonus = GetAbilityScore(activator, AbilityType.Vitality) * 50;
                            Impact(activator, nearest, 400 + enmityBonus);
                        }

                        nth++;
                        nearest = GetNearestCreatureToLocation(CreatureType.IsAlive, true, location, nth);
                    }

                });
        }
    }
}
