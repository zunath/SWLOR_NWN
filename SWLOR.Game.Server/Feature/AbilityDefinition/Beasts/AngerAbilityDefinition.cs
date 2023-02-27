using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.Creature;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Beasts
{
    public class AngerAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            Anger1();
            Anger2();
            Anger3();
            Anger4();
            Anger5();

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
            _builder.Create(FeatType.Anger1, PerkType.Anger)
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
        private void Anger2()
        {
            _builder.Create(FeatType.Anger2, PerkType.Anger)
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
        private void Anger3()
        {
            _builder.Create(FeatType.Anger3, PerkType.Anger)
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

        private void Anger4()
        {
            _builder.Create(FeatType.Anger4, PerkType.Anger)
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
        private void Anger5()
        {
            _builder.Create(FeatType.Anger5, PerkType.Anger)
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
