using System.Collections.Generic;
using SWLOR.Game.Server.Service;


using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Core.Enums;
using SWLOR.Shared.Core.Models;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.NPC
{
    public class IronShellAbilityDefinition: IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            IronShell();

            return _builder.Build();
        }

        private void IronShell()
        {
            _builder.Create(FeatType.IronShell, PerkType.Invalid)
                .Name("Iron Shell")
                .HasActivationDelay(2.0f)
                .HasRecastDelay(RecastGroup.IronShell, 60f)
                .IsCastedAbility()
                .RequirementStamina(5)
                .HasImpactAction((activator, target, level, location) =>
                {
                    ApplyEffectToObject(DurationType.Temporary, EffectVisualEffect(VisualEffect.Vfx_Dur_Aura_Magenta), activator, 1.0f);
                    ServiceContainer.GetService<IStatusEffectService>().Apply(activator, activator, StatusEffectType.IronShell, 45f);
                });
        }
    }
}
