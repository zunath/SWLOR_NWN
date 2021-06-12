using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.Creature;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Force
{
    public class ForcePushAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            ForcePush1(builder);
            ForcePush2(builder);
            ForcePush3(builder);
            ForcePush4(builder);

            return builder.Build();
        }

        private static string Validation(uint activator, uint target, int level)
        {
            var size = GetCreatureSize(target);
            var maxSize = CreatureSize.Invalid;
            switch (level)
            {
                case 1:
                    maxSize = CreatureSize.Small;
                    break;
                case 2:
                    maxSize = CreatureSize.Medium;
                    break;
                case 3:
                    maxSize = CreatureSize.Large;
                    break;
                case 4:
                    maxSize = CreatureSize.Huge;
                    break;
            }

            if (size > maxSize)
                return "Your target is too large to force push.";

            return string.Empty;
        }

        private static void ImpactAction(uint activator, uint target, int level)
        {
            if (!Ability.GetAbilityResisted(activator, target, AbilityType.Intelligence, AbilityType.Wisdom))
            {
                ApplyEffectToObject(DurationType.Temporary, EffectKnockdown(), target, 6f);
            }
            else ApplyEffectToObject(DurationType.Temporary, EffectSlow(), target, 6.0f);

            Enmity.ModifyEnmityOnAll(activator, 1);
            CombatPoint.AddCombatPointToAllTagged(activator, SkillType.Force, 3);
        }

        private static void ForcePush1(AbilityBuilder builder)
        {
            builder.Create(FeatType.ForcePush1, PerkType.ForcePush)
                .Name("Force Push I")
                .HasRecastDelay(RecastGroup.ForcePush, 30f)
                .HasActivationDelay(2.0f)
                .RequirementFP(2)
                .IsCastedAbility()
                .DisplaysVisualEffectWhenActivating()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }

        private static void ForcePush2(AbilityBuilder builder)
        {
            builder.Create(FeatType.ForcePush2, PerkType.ForcePush)
                .Name("Force Push II")
                .HasRecastDelay(RecastGroup.ForcePush, 30f)
                .HasActivationDelay(2.0f)
                .RequirementFP(3)
                .IsCastedAbility()
                .DisplaysVisualEffectWhenActivating()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }

        private static void ForcePush3(AbilityBuilder builder)
        {
            builder.Create(FeatType.ForcePush3, PerkType.ForcePush)
                .Name("Force Push III")
                .HasRecastDelay(RecastGroup.ForcePush, 30f)
                .HasActivationDelay(2.0f)
                .RequirementFP(4)
                .IsCastedAbility()
                .DisplaysVisualEffectWhenActivating()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }

        private static void ForcePush4(AbilityBuilder builder)
        {
            builder.Create(FeatType.ForcePush4, PerkType.ForcePush)
                .Name("Force Push IV")
                .HasRecastDelay(RecastGroup.ForcePush, 30f)
                .HasActivationDelay(4.0f)
                .RequirementFP(5)
                .IsCastedAbility()
                .DisplaysVisualEffectWhenActivating()
                .HasImpactAction(ImpactAction);
        }
    }
}