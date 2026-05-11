using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Beasts
{
    public class AngerAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            Anger1();
            Anger2();

            return _builder.Build();
        }

        private string Validation(uint target)
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

            Enmity.ModifyEnmity(activator, target, baseEnmity);
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

        private void Anger1()
        {
            _builder
                .Create(FeatType.Anger1, PerkType.Anger)
                .Name("Anger I")
                .Level(1)
                .HasRecastDelay(RecastGroup.Anger, 30f)
                .HasActivationDelay(1f)
                .UsesAnimation(Animation.FireForgetTaunt)
                .IsCastedAbility()
                .HasMaxRange(30f)
                .HasCustomValidation((_, target, _, _) => Validation(target))
                .HasImpactAction((activator, target, _, _) =>
                {
                    var enmityBonus = GetAbilityScore(activator, AbilityType.Vitality) * 25;
                    ImpactSingle(activator, target, 200 + enmityBonus);
                });
        }
        private void Anger2()
        {
            _builder
                .Create(FeatType.Anger2, PerkType.Anger)
                .Name("Anger II")
                .Level(2)
                .HasRecastDelay(RecastGroup.Anger, 30f)
                .HasActivationDelay(1f)
                .UsesAnimation(Animation.FireForgetTaunt)
                .IsCastedAbility()
                .HasMaxRange(30f)
                .HasCustomValidation((_, target, _, _) => Validation(target))
                .HasImpactAction((activator, target, _, _) =>
                {
                    var enmityBonus = GetAbilityScore(activator, AbilityType.Vitality) * 25;
                    ImpactSingle(activator, target, 250 + enmityBonus);
                });
        }

    }
}
