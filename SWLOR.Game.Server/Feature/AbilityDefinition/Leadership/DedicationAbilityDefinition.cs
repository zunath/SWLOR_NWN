using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.StatusEffectService;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Leadership
{
    public class DedicationAbilityDefinition: AuraBaseAbilityDefinition, IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            Dedication();

            return _builder.Build();
        }

        private void Dedication()
        {
            _builder.Create(FeatType.Dedication, PerkType.Dedication)
                .Name("Dedication")
                .Level(1)
                .HasRecastDelay(RecastGroup.Dedication, 60f)
                .HasActivationDelay(4f)
                .UnaffectedByHeavyArmor()
                .IsCastedAbility()
                .UsesAnimation(Animation.FireForgetTaunt)
                .HasActivationAction((activator, target, level, location) =>
                {
                    return OnAuraActivation(activator, StatusEffectType.Dedication);
                })
                .HasImpactAction((activator, target, level, location) =>
                {
                    ApplyAura(activator, StatusEffectType.Dedication, true, true, false);
                });
        }
    }
}
