using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Leadership
{
    public sealed class DecisiveCommandAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            DecisiveCommand1(builder);

            return builder.Build();
        }

        private static void DecisiveCommand1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.DecisiveCommand1, PerkType.DecisiveCommand)
                .Name("Decisive Command")
                .Level(1)
                .HasActivationDelay(1f)
                .HasRecastDelay(RecastGroup.Capstone, 1800f)
                .SkillType(SkillType.Leadership)
                .IsAreaAbility()
                .HasImpactAction(DecisiveCommand1ImpactAction)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementStamina(15);
        }

        private static void DecisiveCommand1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            var radius = LeadershipAbilityEffects.GetLeadershipCommandRadius(activator);
            var duration = LeadershipAbilityEffects.ApplyVanguardCommandDurationBonus(activator, 20f);

            foreach (var friendly in AbilityTargeting.GetFriendlyTargets(activator, target, true, radius))
            {
                StatusEffect.ApplyStatusEffect(activator, friendly, typeof(DecisiveCommand1StatusEffect), duration);
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Holy_Aid), friendly);
            }
        }


    }
}
