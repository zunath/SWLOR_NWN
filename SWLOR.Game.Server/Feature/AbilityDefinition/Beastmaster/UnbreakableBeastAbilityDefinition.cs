using System;
using System.Collections.Generic;
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

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Beastmaster
{
    public sealed class UnbreakableBeastAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            UnbreakableBeast1(builder);

            return builder.Build();
        }

        private static void UnbreakableBeast1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.UnbreakableBeast1, PerkType.UnbreakableBeast)
                .Name("Unbreakable Beast")
                .Level(1)
                .HasActivationDelay(0f)
                .UsesAnimation(Animation.ShieldWall)
                .HasRecastDelay(RecastGroup.UnbreakableBeast, 180f)
                .SkillType(SkillType.BeastMastery)
                .IsSingleTargetAbility()
                .HasImpactAction(UnbreakableBeast1ImpactAction)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementStamina(12);
        }

        private static void UnbreakableBeast1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            StatusEffect.ApplyStatusEffect(activator, activator, typeof(UnbreakableBeast1StatusEffect), 30f);
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Holy_Aid), activator);
        }


    }
}
