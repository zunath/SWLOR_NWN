using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.NPC
{
    public class SpikesAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            Spikes();

            return _builder.Build();
        }

        private void Spikes()
        {
            _builder.Create(FeatType.Spikes, PerkType.Invalid)
                .Name("Spikes")
                .HasActivationDelay(3.5f)
                .HasRecastDelay(RecastGroup.Spikes, 20f)
                .IsCastedAbility()
                .RequirementStamina(8)
                .HasImpactAction((activator, target, level, location) =>
                {
                    var might = GetAbilityModifier(AbilityType.Might, activator);
                    var dmg = 2.5f;
                    var defense = Combat.CalculateDefense(target);
                    var vitality = GetAbilityModifier(AbilityType.Vitality, target);
                    var damage = Combat.CalculateDamage(dmg, might, defense, vitality, false);

                    ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Piercing), target);
                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Wallspike), target);
                    StatusEffect.Apply(activator, target, StatusEffectType.Bleed, 45f);
                });
        }

    }
}
