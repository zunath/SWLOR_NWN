using SWLOR.Core.NWScript.Enum;
using SWLOR.Core.NWScript.Enum.VisualEffect;
using SWLOR.Core.Service;
using SWLOR.Core.Service.AbilityService;
using SWLOR.Core.Service.PerkService;
using SWLOR.Core.Service.StatusEffectService;

namespace SWLOR.Core.Feature.AbilityDefinition.NPC
{
    public class BiteAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

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
                    StatusEffect.Apply(activator, target, StatusEffectType.Bleed, 60f);

                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Com_Chunk_Red_Small), target);
                });
        }

    }
}
