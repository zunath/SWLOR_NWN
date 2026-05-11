using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.HeavyVibroblade
{
    public class BloodlustAbilityDefinition : HeavyVibrobladeActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            Bloodlust(builder);

            return builder.Build();
        }

        private static void Bloodlust(AbilityBuilder builder)
        {
            builder.Create(FeatType.Bloodlust1, PerkType.Bloodlust)
                .Name("Bloodlust")
                .Level(1)
                .HasActivationDelay(0f)
                .HasImpactAction((activator, target, level, targetLocation) =>
                {
                    SacrificeHitPoints(activator, 40, 10);
                    var restorePercent = Math.Min(80, 20 + Math.Max(0, GetAbilityModifier(AbilityType.Might, activator)));
                    var amount = (int)Math.Ceiling(Stat.GetMaxStamina(activator) * (restorePercent / 100f));
                    Stat.RestoreStamina(activator, amount);
                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Restoration), activator);
                })
                .IsCastedAbility()
                .BreaksStealth();
        }
    }
}
