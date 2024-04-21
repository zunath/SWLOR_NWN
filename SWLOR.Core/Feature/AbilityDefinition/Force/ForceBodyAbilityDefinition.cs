using SWLOR.Core.NWScript.Enum;
using SWLOR.Core.Service;
using SWLOR.Core.Service.AbilityService;
using SWLOR.Core.Service.PerkService;
using SWLOR.Core.Service.StatusEffectService;

namespace SWLOR.Core.Feature.AbilityDefinition.Force
{
    public class ForceBodyAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            ForceBody1();
            ForceBody2();

            return _builder.Build();
        }

        private void ForceBody1()
        {
            _builder.Create(FeatType.ForceBody1, PerkType.ForceBody)
                .Name("Force Body I")
                .Level(1)
                .HasRecastDelay(RecastGroup.ForceRestore, 60f * 3f)
                .IsCastedAbility()
                .UsesAnimation(Animation.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating()
                .HasImpactAction((activator, target, level, location) =>
                {
                    StatusEffect.Apply(activator, activator, StatusEffectType.ForceBody1, 60f);
                    ApplyEffectToObject(DurationType.Temporary, EffectAbilityDecrease(AbilityType.Vitality, 2), activator, 60f);
                });
        }
        private void ForceBody2()
        {
            _builder.Create(FeatType.ForceBody2, PerkType.ForceBody)
                .Name("Force Body II")
                .Level(2)
                .HasRecastDelay(RecastGroup.ForceRestore, 60f * 3f)
                .IsCastedAbility()
                .UsesAnimation(Animation.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating()
                .HasImpactAction((activator, target, level, location) =>
                {
                    StatusEffect.Apply(activator, activator, StatusEffectType.ForceBody2, 60f);
                    ApplyEffectToObject(DurationType.Temporary, EffectAbilityDecrease(AbilityType.Vitality, 4), activator, 60f);
                });
        }
    }
}