using System.Collections.Generic;
using SWLOR.Game.Server.Service;

using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Core.Enums;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Core.Infrastructure;
using SWLOR.Shared.Core.Models;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Leadership
{
    public class SoldiersSpeedAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            SoldiersSpeed();

            return _builder.Build();
        }

        private void SoldiersSpeed()
        {
            _builder.Create(FeatType.SoldiersSpeed, PerkType.SoldiersSpeed)
                .Name("Soldier's Speed")
                .Level(1)
                .HasRecastDelay(RecastGroup.SoldiersSpeed, 60f)
                .HasActivationDelay(4f)
                .UnaffectedByHeavyArmor()
                .IsCastedAbility()
                .UsesAnimation(Animation.FireForgetTaunt)
                .HasActivationAction((activator, target, level, location) =>
                {
                    return _abilityService.ToggleAura(activator, StatusEffectType.SoldiersSpeed);
                })
                .HasImpactAction((activator, target, level, location) =>
                {
                    _abilityService.ApplyAura(activator, StatusEffectType.SoldiersSpeed, false, true, false);
                });
        }
    }
}
