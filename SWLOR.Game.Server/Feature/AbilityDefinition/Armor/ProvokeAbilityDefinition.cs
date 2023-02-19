using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.Creature;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Armor
{
    public class ProvokeAbilityDefinition: IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            Provoke();
            Provoke2();

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

        private void Impact(uint activator, uint target, int enmity)
        {
            if (!LineOfSightObject(activator, target))
                return;

            Enmity.ModifyEnmity(activator, target, enmity);
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Fnf_Howl_Odd), target);
        }

        private void Provoke()
        {
            _builder.Create(FeatType.Provoke1, PerkType.Provoke)
                .Name("Provoke")
                .Level(1)
                .HasRecastDelay(RecastGroup.Provoke, 20f)
                .HasActivationDelay(1f)
                .UsesAnimation(Animation.FireForgetTaunt)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasMaxRange(15f)
                .HasCustomValidation((_, target, _, _) => Validation(target))
                .HasImpactAction((activator, target, _, _) =>
                {
                    var enmityBonus = GetAbilityScore(activator, AbilityType.Vitality) * 50;
                    Impact(activator, target, 350 + enmityBonus);
                });
        }

        private void Provoke2()
        {
            _builder.Create(FeatType.Provoke2, PerkType.Provoke)
                .Name("Provoke II")
                .Level(2)
                .HasRecastDelay(RecastGroup.Provoke2, 40f)
                .HasActivationDelay(1f)
                .UsesAnimation(Animation.FireForgetTaunt)
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
