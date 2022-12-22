using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.StatusEffectService;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Leadership
{
    public class ChargeAbilityDefinition: AuraBaseAbilityDefinition, IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            Charge();

            return _builder.Build();
        }

        private void Charge()
        {
            _builder.Create(FeatType.Charge, PerkType.Charge)
                .Name("Charge")
                .Level(1)
                .HasRecastDelay(RecastGroup.Charge, 60f)
                .HasActivationDelay(4f)
                .UnaffectedByHeavyArmor()
                .IsCastedAbility()
                .UsesAnimation(Animation.FireForgetTaunt)
                .HasActivationAction((activator, target, level, location) =>
                {
                    return OnAuraActivation(activator, StatusEffectType.Charge);
                })
                .HasImpactAction((activator, target, level, location) =>
                {
                    ApplyAura(activator, StatusEffectType.Charge, true, true, false);
                });
        }
    }
}
