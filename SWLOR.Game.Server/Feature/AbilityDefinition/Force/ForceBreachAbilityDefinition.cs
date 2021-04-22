//using Random = SWLOR.Game.Server.Service.Random;

using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Force
{
    public class ForceBreachAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            ForceBreach1(builder);
            ForceBreach2(builder);
            ForceBreach3(builder);
            ForceBreach4(builder);

            return builder.Build();
        }

        private static void ImpactAction(uint activator, uint target, int level)
        {
            int damage;

            switch (level)
            {
                case 1:
                    damage = 10 + GetAbilityModifier(AbilityType.Wisdom);
                    break;
                case 2:
                    damage = 15 + GetAbilityModifier(AbilityType.Wisdom);
                    break;
                case 3:
                    damage = (int)(20 + GetAbilityModifier(AbilityType.Wisdom) * 1.5);
                    break;
                case 4:
                    damage = (int)(25 + GetAbilityModifier(AbilityType.Wisdom) * 1.75);
                    break;
                default:
                    damage = 0;
                    break;
            }

            if (!Ability.GetAbilityResisted(activator, target, AbilityType.Intelligence, AbilityType.Wisdom))
            {
                ApplyEffectToObject(DurationType.Instant, EffectDamage(damage), target);
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Silence), target);
            }

            Enmity.ModifyEnmityOnAll(activator, 1);
            CombatPoint.AddCombatPointToAllTagged(activator, SkillType.Force, 3);
        }

        private static void ForceBreach1(AbilityBuilder builder)
        {
            builder.Create(FeatType.ForceBreach1, PerkType.ForceBreach)
                .Name("Force Breach I")
                .HasRecastDelay(RecastGroup.ForceBreach, 30f)
                .HasActivationDelay(2.0f)
                .RequirementFP(2)
                .IsCastedAbility()
                .DisplaysVisualEffectWhenActivating()
                .HasImpactAction((activator, target, level) =>
                {
                    ImpactAction(activator, target, level);
                });
        }

        private static void ForceBreach2(AbilityBuilder builder)
        {
            builder.Create(FeatType.ForceBreach2, PerkType.ForceBreach)
                .Name("Force Breach II")
                .HasRecastDelay(RecastGroup.ForceBreach, 30f)
                .HasActivationDelay(2.0f)
                .RequirementFP(3)
                .IsCastedAbility()
                .DisplaysVisualEffectWhenActivating()
                .HasImpactAction((activator, target, level) =>
                {
                    ImpactAction(activator, target, level);
                });
        }

        private static void ForceBreach3(AbilityBuilder builder)
        {
            builder.Create(FeatType.ForceBreach3, PerkType.ForceBreach)
                .Name("Force Breach III")
                .HasRecastDelay(RecastGroup.ForceBreach, 30f)
                .HasActivationDelay(2.0f)
                .RequirementFP(4)
                .IsCastedAbility()
                .DisplaysVisualEffectWhenActivating()
                .HasImpactAction((activator, target, level) =>
                {
                    ImpactAction(activator, target, level);
                });
        }

        private static void ForceBreach4(AbilityBuilder builder)
        {
            builder.Create(FeatType.ForceBreach4, PerkType.ForceBreach)
                .Name("Force Breach IV")
                .HasRecastDelay(RecastGroup.ForceBreach, 30f)
                .HasActivationDelay(4.0f)
                .RequirementFP(5)
                .IsCastedAbility()
                .DisplaysVisualEffectWhenActivating()
                .HasImpactAction((activator, target, level) =>
                {
                    ImpactAction(activator, target, level);
                });
        }
    }
}