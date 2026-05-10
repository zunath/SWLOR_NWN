using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.HeavyVibroblade
{
    public class AbsoluteDefenseAbilityDefinition : HeavyVibrobladeActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            AbsoluteDefense(builder);

            return builder.Build();
        }

        private static void AbsoluteDefense(AbilityBuilder builder)
        {
            builder.Create(FeatType.AbsoluteDefense1, PerkType.AbsoluteDefense)
                .Name("Absolute Defense")
                .Level(1)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.AbsoluteDefense, 1800f)
                .HasImpactAction((activator, target, level, targetLocation) =>
                {
                    ApplyStatusToNearbyParty(activator, typeof(AbsoluteDefenseStatusEffect), 15f, false);
                    ApplyImmunityToNearbyParty(activator, ImmunityType.Knockdown, 15f, false);
                    ApplyImmunityToNearbyParty(activator, ImmunityType.Dazed, 15f, false);

                    var healAmount = (int)Math.Ceiling(GetMaxHitPoints(activator) * 0.25f);
                    ApplyEffectToObject(DurationType.Instant, EffectHeal(healAmount), activator);
                    Stat.RestoreStamina(activator, (int)Math.Ceiling(Stat.GetMaxStamina(activator) * 0.25f));
                    Stat.RestoreFP(activator, (int)Math.Ceiling(Stat.GetMaxFP(activator) * 0.25f));
                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Holy_Aid), activator);
                })
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementStamina(25);
        }
    }
}
