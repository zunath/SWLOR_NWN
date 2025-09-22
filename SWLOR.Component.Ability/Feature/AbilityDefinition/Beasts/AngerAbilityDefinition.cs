using SWLOR.Component.Ability.Contracts;
using SWLOR.Component.Ability.Enums;
using SWLOR.Component.Ability.Model;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using SWLOR.Shared.Domain.Enums;

namespace SWLOR.Component.Ability.Feature.AbilityDefinition.Beasts
{
    public class AngerAbilityDefinition : IAbilityListDefinition
    {
        private readonly IEnmityService _enmityService;

        public AngerAbilityDefinition(IEnmityService enmityService)
        {
            _enmityService = enmityService;
        }

        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            Anger1(builder);
            Anger2(builder);
            Anger3(builder);
            Anger4(builder);
            Anger5(builder);

            return builder.Build();
        }

        private static string Validation(uint target)
        {
            if (GetIsPC(target))
            {
                return "This ability cannot be used on players.";
            }

            return string.Empty;
        }

        private void ImpactSingle(uint activator, uint target, int baseEnmity)
        {
            if (!LineOfSightObject(activator, target))
                return;

            _enmityService.ModifyEnmity(activator, target, baseEnmity);
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Fnf_Howl_Odd), target);
        }

        private void ImpactAOE(uint activator, Location location, int baseEnmity)
        {
            var nth = 1;
            var nearest = GetNearestCreatureToLocation(CreatureType.IsAlive, true, location, nth);
            var enmityBonus = GetAbilityScore(activator, AbilityType.Vitality) * 50;

            while (GetIsObjectValid(nearest))
            {
                if (GetDistanceBetweenLocations(GetLocation(nearest), location) > 8f)
                    break;

                if (!GetIsPC(nearest))
                {
                    ImpactSingle(activator, nearest, baseEnmity + enmityBonus);
                }

                nth++;
                nearest = GetNearestCreatureToLocation(CreatureType.IsAlive, true, location, nth);
            }
        }

        private void Anger1(IAbilityBuilder builder)
        {
            builder.Create(FeatType.Anger1, PerkType.Anger)
                .Name("Anger I")
                .Level(1)
                .HasRecastDelay(RecastGroup.Anger, 30f)
                .HasActivationDelay(1f)
                .UsesAnimation(Animation.FireForgetTaunt)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasMaxRange(30f)
                .HasCustomValidation((_, target, _, _) => Validation(target))
                .HasImpactAction((activator, target, _, _) =>
                {
                    var enmityBonus = GetAbilityScore(activator, AbilityType.Vitality) * 25;
                    ImpactSingle(activator, target, 200 + enmityBonus);
                });
        }
        private void Anger2(IAbilityBuilder builder)
        {
            builder.Create(FeatType.Anger2, PerkType.Anger)
                .Name("Anger II")
                .Level(2)
                .HasRecastDelay(RecastGroup.Anger, 30f)
                .HasActivationDelay(1f)
                .UsesAnimation(Animation.FireForgetTaunt)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasMaxRange(30f)
                .HasCustomValidation((_, target, _, _) => Validation(target))
                .HasImpactAction((activator, target, _, _) =>
                {
                    var enmityBonus = GetAbilityScore(activator, AbilityType.Vitality) * 25;
                    ImpactSingle(activator, target, 250 + enmityBonus);
                });
        }
        private void Anger3(IAbilityBuilder builder)
        {
            builder.Create(FeatType.Anger3, PerkType.Anger)
                .Name("Anger III")
                .Level(3)
                .HasRecastDelay(RecastGroup.Anger, 30f)
                .HasActivationDelay(1f)
                .UsesAnimation(Animation.FireForgetTaunt)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasMaxRange(30f)
                .HasCustomValidation((_, target, _, _) => Validation(target))
                .HasImpactAction((activator, target, _, _) =>
                {
                    var enmityBonus = GetAbilityScore(activator, AbilityType.Vitality) * 50;
                    ImpactSingle(activator, target, 300 + enmityBonus);
                });
        }

        private void Anger4(IAbilityBuilder builder)
        {
            builder.Create(FeatType.Anger4, PerkType.Anger)
                .Name("Anger IV")
                .Level(4)
                .HasRecastDelay(RecastGroup.AOEAnger, 40f)
                .HasActivationDelay(1f)
                .UsesAnimation(Animation.FireForgetTaunt)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasMaxRange(15f)
                .HasCustomValidation((_, target, _, _) => Validation(target))
                .HasImpactAction((activator, _, _, location) =>
                {
                    ImpactAOE(activator, location, 300);
                });
        }
        private void Anger5(IAbilityBuilder builder)
        {
            builder.Create(FeatType.Anger5, PerkType.Anger)
                .Name("Anger V")
                .Level(5)
                .HasRecastDelay(RecastGroup.AOEAnger, 40f)
                .HasActivationDelay(1f)
                .UsesAnimation(Animation.FireForgetTaunt)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasMaxRange(15f)
                .HasCustomValidation((_, target, _, _) => Validation(target))
                .HasImpactAction((activator, _, _, location) =>
                {
                    ImpactAOE(activator, location, 350);
                });
        }
    }
}
