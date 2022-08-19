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

        private void Impact(uint activator, uint target, int enmity)
        {
            if (!LineOfSightObject(activator, target))
                return;

            Enmity.ModifyEnmity(activator, target, enmity);
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Fnf_Howl_Odd), target);

            AI.ForceTargetSwap(target, activator);
        }

        private void Provoke()
        {
            _builder.Create(FeatType.Provoke, PerkType.Provoke)
                .Name("Provoke")
                .Level(1)
                .HasRecastDelay(RecastGroup.Provoke, 20f)
                .HasActivationDelay(1f)
                .UsesAnimation(Animation.FireForgetTaunt)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasMaxRange(15f)
                .HasImpactAction((activator, target, level, location) =>
                {
                    var enmityBonus = GetAbilityScore(activator, AbilityType.Social) * 50;
                    Impact(activator, target, 1200 + enmityBonus);
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
                .HasImpactAction((activator, target, level, location) =>
                {
                    var nth = 1;
                    var nearest = GetNearestCreatureToLocation(CreatureType.IsAlive, true, location, nth);

                    while (GetIsObjectValid(nearest))
                    {
                        if (GetDistanceBetweenLocations(GetLocation(nearest), location) > 8f)
                            break;

                        if (!GetIsPC(nearest))
                        {
                            var enmityBonus = GetAbilityScore(activator, AbilityType.Social) * 50;
                            Impact(activator, nearest, 1230 * enmityBonus);
                        }

                        nth++;
                        nearest = GetNearestCreatureToLocation(CreatureType.IsAlive, true, location, nth);
                    }

                });
        }
    }
}
