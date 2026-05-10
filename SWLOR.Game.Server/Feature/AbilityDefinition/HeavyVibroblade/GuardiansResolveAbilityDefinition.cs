using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.HeavyVibroblade
{
    public class GuardiansResolveAbilityDefinition : HeavyVibrobladeActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            GuardiansResolve(builder);

            return builder.Build();
        }

        private static void GuardiansResolve(AbilityBuilder builder)
        {
            builder.Create(FeatType.GuardiansResolve1, PerkType.GuardiansResolve)
                .Name("Guardian's Resolve")
                .Level(1)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.GuardiansResolve, 90f)
                .HasImpactAction((activator, target, level, targetLocation) =>
                {
                    var shield = Math.Max(1, (int)Math.Ceiling(GetMaxHitPoints(activator) * 0.3f));
                    ApplyEffectToObject(DurationType.Temporary, EffectTemporaryHitpoints(shield), activator, 30f);
                    StatusEffect.ApplyStatusEffect(activator, activator, typeof(GuardiansResolveStatusEffect), 30f);
                })
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementStamina(10);
        }
    }
}
