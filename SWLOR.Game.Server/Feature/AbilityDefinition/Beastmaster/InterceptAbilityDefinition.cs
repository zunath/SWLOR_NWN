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
    public sealed class InterceptAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            Intercept1(builder);
            Intercept2(builder);

            return builder.Build();
        }

        private static void Intercept1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.Intercept1, PerkType.Intercept)
                .Name("Intercept I")
                .Level(1)
                .HasActivationDelay(0f)
                .UsesAnimation(Animation.FollowMe)
                .HasRecastDelay(RecastGroup.Intercept, 45f)
                .SkillType(SkillType.BeastMastery)
                .IsAreaAbility()
                .HasImpactAction(Intercept1ImpactAction)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementStamina(6);
        }

        private static void Intercept2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.Intercept2, PerkType.Intercept)
                .Name("Intercept II")
                .Level(2)
                .HasActivationDelay(0f)
                .UsesAnimation(Animation.FollowMe)
                .HasRecastDelay(RecastGroup.Intercept, 45f)
                .SkillType(SkillType.BeastMastery)
                .IsAreaAbility()
                .HasImpactAction(Intercept2ImpactAction)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementStamina(8);
        }

        private static void Intercept1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            foreach (var friendly in GetBeastMasterTargets(activator))
            {
                StatusEffect.ApplyStatusEffect(activator, friendly, typeof(Intercept1StatusEffect), 30f);
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Holy_Aid), friendly);
            }
        }

        private static void Intercept2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            foreach (var friendly in GetBeastMasterTargets(activator))
            {
                StatusEffect.ApplyStatusEffect(activator, friendly, typeof(Intercept2StatusEffect), 30f);
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Holy_Aid), friendly);
            }
        }


        private static IEnumerable<uint> GetBeastAndMasterTargets(uint activator)
        {
            yield return activator;

            var master = GetMaster(activator);
            if (GetIsObjectValid(master))
                yield return master;
        }

        private static IEnumerable<uint> GetBeastMasterTargets(uint activator)
        {
            var master = GetMaster(activator);
            yield return GetIsObjectValid(master) ? master : activator;
        }
    }
}
