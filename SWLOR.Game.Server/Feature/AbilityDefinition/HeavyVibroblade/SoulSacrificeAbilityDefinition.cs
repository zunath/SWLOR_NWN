using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.HeavyVibroblade
{
    public class SoulSacrificeAbilityDefinition : HeavyVibrobladeActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            SoulSacrifice(builder);

            return builder.Build();
        }

        private static void SoulSacrifice(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.SoulSacrifice1, PerkType.SoulSacrifice)
                .Name("Soul Sacrifice")
                .Level(1)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.SoulSacrifice, 180f)
                .HasImpactAction((activator, target, level, targetLocation) =>
                {
                    SacrificeHitPoints(activator, 50, 20);
                    StatusEffect.ApplyStatusEffect(activator, activator, typeof(SoulSacrificeStatusEffect), 30f);
                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Head_Odd), activator);
                })
                .SkillType(SkillType.HeavyVibroblade)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementStamina(12);
        }
    }
}
