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
    public class BiteAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            Bite();
            return _builder.Build();
        }

        private void Bite()
        {
            _builder.Create(FeatType.Bite, PerkType.Invalid)
                .Name("Bite")
                .HasActivationDelay(2f)
                .IsCastedAbility()
                .RequirementStamina(6)
                .UnaffectedByHeavyArmor()
                .HasRecastDelay(RecastGroup.Bite, 60f)
                .HasImpactAction((activator, target, level, location) =>
                {
                    ServiceContainer.GetService<IStatusEffectService>().Apply(activator, target, StatusEffectType.Bleed, 60f);

                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Com_Chunk_Red_Small), target);
                });
        }

    }
}
