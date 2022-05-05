using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.Creature;
using SWLOR.Game.Server.Core.NWScript.Enum.Item.Property;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.NPC
{
    public class RoarAbilityDefinition: IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            Roar();
            return _builder.Build();
        }

        private void Roar()
        {
            _builder.Create(FeatType.Roar, PerkType.Invalid)
                .Name("Roar")
                .HasActivationDelay(2f)
                .IsCastedAbility()
                .RequirementStamina(6)
                .UnaffectedByHeavyArmor()
                .HasRecastDelay(RecastGroup.Roar, 60f)
                .HasImpactAction((activator, target, level, location) =>
                {
                    const float Distance = 2.5f;
                    const int DecreaseBy = 3;

                    var count = 1;
                    var creature = GetNearestCreature(CreatureType.IsAlive, 1, activator, count);
                    while (GetIsObjectValid(creature) &&
                           GetDistanceBetween(creature, activator) <= Distance)
                    {
                        ApplyEffectToObject(DurationType.Temporary, EffectACDecrease(DecreaseBy, ArmorClassModiferType.Natural), creature, 20f);

                        count++;
                        creature = GetNearestCreature(CreatureType.IsAlive, 1, activator, count);
                    }

                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Fnf_Howl_Mind), activator);
                });
        }

    }
}
