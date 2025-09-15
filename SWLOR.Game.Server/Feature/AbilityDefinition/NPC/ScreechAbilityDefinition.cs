using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.NPC
{
    public class ScreechAbilityDefinition: IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new();
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            Screech();

            return _builder.Build();
        }

        private void Screech()
        {
            _builder.Create(FeatType.Screech, PerkType.Invalid)
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
