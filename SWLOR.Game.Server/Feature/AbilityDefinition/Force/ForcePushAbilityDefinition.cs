using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.Creature;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;

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

        private static string Validation(uint activator, uint target, int level, Location targetLocation)
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

        private static void ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            var willpowerBonus = 0.5f * GetAbilityModifier(AbilityType.Willpower, activator);
            if (!Ability.GetAbilityResisted(activator, target, "Force Push", AbilityType.Willpower))
            {
                ApplyEffectToObject(DurationType.Temporary, EffectKnockdown(), target, 6f + willpowerBonus);
            }
            else ApplyEffectToObject(DurationType.Temporary, EffectSlow(), target, 6.0f + willpowerBonus);

            Enmity.ModifyEnmityOnAll(activator, level * 150);

            CombatPoint.AddCombatPoint(activator, target, SkillType.Force, 3);
        }

        private static void ForcePush1(AbilityBuilder builder)
        {
            builder.Create(FeatType.ForcePush1, PerkType.ForcePush)
                .Name("Force Push I")
                .Level(1)
                .HasRecastDelay(RecastGroup.ForcePush, 30f)
                .HasMaxRange(15.0f)
                .RequirementFP(1)
                .IsCastedAbility()
                .IsHostileAbility()
                .UsesAnimation(Animation.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }

        private static void ForcePush2(AbilityBuilder builder)
        {
            builder.Create(FeatType.ForcePush2, PerkType.ForcePush)
                .Name("Force Push II")
                .Level(2)
                .HasRecastDelay(RecastGroup.ForcePush, 30f)
                .HasMaxRange(15.0f)
                .RequirementFP(2)
                .IsCastedAbility()
                .IsHostileAbility()
                .UsesAnimation(Animation.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }

        private static void ForcePush3(AbilityBuilder builder)
        {
            builder.Create(FeatType.ForcePush3, PerkType.ForcePush)
                .Name("Force Push III")
                .Level(3)
                .HasRecastDelay(RecastGroup.ForcePush, 30f)
                .HasMaxRange(15.0f)
                .RequirementFP(3)
                .IsCastedAbility()
                .IsHostileAbility()
                .UsesAnimation(Animation.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }

        private static void ForcePush4(AbilityBuilder builder)
        {
            builder.Create(FeatType.ForcePush4, PerkType.ForcePush)
                .Name("Force Push IV")
                .Level(4)
                .HasRecastDelay(RecastGroup.ForcePush, 30f)
                .HasMaxRange(15.0f)
                .RequirementFP(4)
                .IsCastedAbility()
                .IsHostileAbility()
                .UsesAnimation(Animation.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating()
                .HasImpactAction(ImpactAction);
        }
    }
}