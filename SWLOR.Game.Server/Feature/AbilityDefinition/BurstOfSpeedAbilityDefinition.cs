//using Random = SWLOR.Game.Server.Service.Random;
using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.Creature;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature.AbilityDefinition
{
    public class BurstOfSpeedAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<Feat, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            BurstOfSpeed1(builder);
            BurstOfSpeed2(builder);
            BurstOfSpeed3(builder);
            BurstOfSpeed4(builder);
            BurstOfSpeed5(builder);

            return builder.Build();
        }

        private static void BurstOfSpeed1(AbilityBuilder builder)
        {
            builder.Create(Feat.BurstOfSpeed1, PerkType.BurstOfSpeed)
                .Name("Burst of Speed I")
                .HasRecastDelay(RecastGroup.Invalid, 20f)
                .HasActivationDelay(2.0f)
                .RequirementFP(2)
                .IsConcentrationAbility(StatusEffectType.BurstOfSpeed1)
                .DisplaysVisualEffectWhenActivating()
                .HasImpactAction((activator, target, level) =>
                {
                    ApplyEffectToObject(DurationType.Temporary, EffectMovementSpeedIncrease(20), activator, 6f);

                    CombatPoint.AddCombatPointToAllTagged(activator, SkillType.Force, 3);
                });
        }
        private static void BurstOfSpeed2(AbilityBuilder builder)
        {
            builder.Create(Feat.BurstOfSpeed2, PerkType.BurstOfSpeed)
                .Name("Burst of Speed II")
                .HasRecastDelay(RecastGroup.Invalid, 20f)
                .HasActivationDelay(2.0f)
                .RequirementFP(3)
                .IsConcentrationAbility(StatusEffectType.BurstOfSpeed2)
                .DisplaysVisualEffectWhenActivating()
                .HasImpactAction((activator, target, level) =>
                {
                    ApplyEffectToObject(DurationType.Temporary, EffectMovementSpeedIncrease(30), activator, 6f);

                    CombatPoint.AddCombatPointToAllTagged(activator, SkillType.Force, 3);
                });
        }
        private static void BurstOfSpeed3(AbilityBuilder builder)
        {
            builder.Create(Feat.BurstOfSpeed3, PerkType.BurstOfSpeed)
                .Name("Burst of Speed III")
                .HasRecastDelay(RecastGroup.Invalid, 20f)
                .HasActivationDelay(2.0f)
                .RequirementFP(4)
                .IsConcentrationAbility(StatusEffectType.BurstOfSpeed3)
                .DisplaysVisualEffectWhenActivating()
                .HasImpactAction((activator, target, level) =>
                {
                    ApplyEffectToObject(DurationType.Temporary, EffectMovementSpeedIncrease(40), activator, 6f);

                    CombatPoint.AddCombatPointToAllTagged(activator, SkillType.Force, 3);
                });
        }
        private static void BurstOfSpeed4(AbilityBuilder builder)
        {
            builder.Create(Feat.BurstOfSpeed4, PerkType.BurstOfSpeed)
                .Name("Burst of Speed IV")
                .HasRecastDelay(RecastGroup.Invalid, 20f)
                .HasActivationDelay(2.0f)
                .RequirementFP(5)
                .IsConcentrationAbility(StatusEffectType.BurstOfSpeed4)
                .DisplaysVisualEffectWhenActivating()
                .HasImpactAction((activator, target, level) =>
                {
                    ApplyEffectToObject(DurationType.Temporary, EffectMovementSpeedIncrease(50), activator, 6f);

                    CombatPoint.AddCombatPointToAllTagged(activator, SkillType.Force, 3);
                });
        }
        private static void BurstOfSpeed5(AbilityBuilder builder)
        {
            builder.Create(Feat.BurstOfSpeed5, PerkType.BurstOfSpeed)
                .Name("Burst of Speed V")
                .HasRecastDelay(RecastGroup.Invalid, 20f)
                .HasActivationDelay(2.0f)
                .RequirementFP(6)
                .IsConcentrationAbility(StatusEffectType.BurstOfSpeed5)
                .DisplaysVisualEffectWhenActivating()
                .HasImpactAction((activator, target, level) =>
                {
                    ApplyEffectToObject(DurationType.Temporary, EffectMovementSpeedIncrease(60), activator, 6f);

                    CombatPoint.AddCombatPointToAllTagged(activator, SkillType.Force, 3);
                });
        }
    }
}