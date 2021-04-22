//using Random = SWLOR.Game.Server.Service.Random;

using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Force
{
    public class ForceLightningAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            ForceLightning1(builder);
            ForceLightning2(builder);
            ForceLightning3(builder);
            ForceLightning4(builder);

            return builder.Build();
        }

        private static void ImpactAction(uint activator, uint target, int level)
        {
            int damage;

            switch (level)
            {
                case 1:
                    damage = 10 + GetAbilityModifier(AbilityType.Intelligence);
                    break;
                case 2:
                    damage = 15 + GetAbilityModifier(AbilityType.Intelligence);
                    break;
                case 3:
                    damage = (int)(20 + GetAbilityModifier(AbilityType.Intelligence) * 1.5);
                    break;
                case 4:
                    damage = (int)(25 + GetAbilityModifier(AbilityType.Intelligence) * 1.75);
                    break;
                default:
                    damage = 0;
                    break;
            }

            if (!Ability.GetAbilityResisted(activator, target, AbilityType.Intelligence, AbilityType.Wisdom))
            {
                ApplyEffectToObject(DurationType.Instant, EffectDamage(damage), target);
                ActionCastFakeSpellAtObject((int)Spell.LightningBolt, target);
            }

            Enmity.ModifyEnmityOnAll(activator, 1);
            CombatPoint.AddCombatPointToAllTagged(activator, SkillType.Force, 3);
        }

        private static void ForceLightning1(AbilityBuilder builder)
        {
            builder.Create(FeatType.ForceLightning1, PerkType.ForceLightning)
                .Name("Force Lightning I")
                .HasRecastDelay(RecastGroup.ForceLightning, 30f)
                .HasActivationDelay(2.0f)
                .RequirementFP(2)
                .IsCastedAbility()
                .DisplaysVisualEffectWhenActivating()
                .HasImpactAction((activator, target, level) =>
                {
                    ImpactAction(activator, target, level);
                });
        }

        private static void ForceLightning2(AbilityBuilder builder)
        {
            builder.Create(FeatType.ForceLightning2, PerkType.ForceLightning)
                .Name("Force Lightning II")
                .HasRecastDelay(RecastGroup.ForceLightning, 30f)
                .HasActivationDelay(2.0f)
                .RequirementFP(3)
                .IsCastedAbility()
                .DisplaysVisualEffectWhenActivating()
                .HasImpactAction((activator, target, level) =>
                {
                    ImpactAction(activator, target, level);
                });
        }

        private static void ForceLightning3(AbilityBuilder builder)
        {
            builder.Create(FeatType.ForceLightning3, PerkType.ForceLightning)
                .Name("Force Lightning III")
                .HasRecastDelay(RecastGroup.ForceLightning, 30f)
                .HasActivationDelay(2.0f)
                .RequirementFP(4)
                .IsCastedAbility()
                .DisplaysVisualEffectWhenActivating()
                .HasImpactAction((activator, target, level) =>
                {
                    ImpactAction(activator, target, level);
                });
        }

        private static void ForceLightning4(AbilityBuilder builder)
        {
            builder.Create(FeatType.ForceLightning4, PerkType.ForceLightning)
                .Name("Force Lightning IV")
                .HasRecastDelay(RecastGroup.ForceLightning, 30f)
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