using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Force
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
                    StatusEffect.ApplyStatusEffect(activator, activator, typeof(ForceBody1StatusEffect), 60f);
                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Improve_Ability_Score), activator);
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
                    StatusEffect.ApplyStatusEffect(activator, activator, typeof(ForceBody2StatusEffect), 60f);
                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Improve_Ability_Score), activator);
                });
        }
    }
}
