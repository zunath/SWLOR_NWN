using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.StatusEffectService;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Leadership
{
    public class SoldiersPrecisionAbilityDefinition : AuraBaseAbilityDefinition, IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            SoldiersPrecision();

            return _builder.Build();
        }

        private void SoldiersPrecision()
        {
            _builder.Create(FeatType.SoldiersPrecision, PerkType.SoldiersPrecision)
                .Name("Soldier's Precision")
                .Level(1)
                .HasRecastDelay(RecastGroup.SoldiersPrecision, 60f)
                .HasActivationDelay(4f)
                .UnaffectedByHeavyArmor()
                .IsCastedAbility()
                .UsesAnimation(Animation.FireForgetTaunt)
                .HasActivationAction((activator, target, level, location) =>
                {
                    return OnAuraActivation(activator, StatusEffectType.SoldiersPrecision);
                })
                .HasImpactAction((activator, target, level, location) =>
                {
                    ApplyAura(activator, StatusEffectType.SoldiersPrecision, false, true, false);
                });
        }
    }
}
