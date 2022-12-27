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
        
        private static void ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            const float BaseDuration = 2f;
            int dc;

            switch (level)
            {
                default:
                case 1:
                    dc = 8;
                    break;
                case 2:
                    dc = 12;
                    break;
                case 3:
                    dc = 14;
                    break;
                case 4:
                    dc = 16;
                    break;
            }

            var willpowerBonus = 0.5f * GetAbilityModifier(AbilityType.Willpower, activator);
            var checkResult = FortitudeSave(target, dc, SavingThrowType.None, activator);
            var duration = BaseDuration + willpowerBonus;

            if (checkResult == SavingThrowResultType.Failed)
            {
                ApplyEffectToObject(DurationType.Temporary, EffectKnockdown(), target, duration);

                Ability.ApplyTemporaryImmunity(target, duration, ImmunityType.Knockdown);
            }
            else if (checkResult == SavingThrowResultType.Success)
            {
                ApplyEffectToObject(DurationType.Temporary, EffectSlow(), target, duration);
            }

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