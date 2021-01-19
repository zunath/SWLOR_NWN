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
    public class ForcePushAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<Feat, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            ForcePush1(builder);
            ForcePush2(builder);
            ForcePush3(builder);
            ForcePush4(builder);

            return builder.Build();
        }

        private static void ForcePush1(AbilityBuilder builder)
        {
            builder.Create(Feat.ForceLeap1, PerkType.ForcePush)
                .Name("Force Push I")
                .HasRecastDelay(RecastGroup.ForcePush, 30f)
                .HasActivationDelay(2.0f)
                .RequirementFP(2)
                .IsCastedAbility()
                .DisplaysVisualEffectWhenActivating()
                .HasImpactAction((activator, target, level) =>
                {
                    if (!Ability.GetAbilityResisted(activator, target, AbilityType.Intelligence, AbilityType.Wisdom))
                    {
                        if (GetCreatureSize(target) == CreatureSize.Small)
                        {
                            ApplyEffectToObject(DurationType.Temporary, EffectKnockdown(), target, 6f);
                        }
                    }

                    Enmity.ModifyEnmityOnAll(activator, 1);
                    CombatPoint.AddCombatPointToAllTagged(activator, SkillType.Force, 3);
                });
        }

        private static void ForcePush2(AbilityBuilder builder)
        {
            builder.Create(Feat.ForceLeap1, PerkType.ForcePush)
                .Name("Force Push II")
                .HasRecastDelay(RecastGroup.ForcePush, 30f)
                .HasActivationDelay(2.0f)
                .RequirementFP(3)
                .IsCastedAbility()
                .DisplaysVisualEffectWhenActivating()
                .HasImpactAction((activator, target, level) =>
                {
                    if (!Ability.GetAbilityResisted(activator, target, AbilityType.Intelligence, AbilityType.Wisdom))
                    {
                        if (GetCreatureSize(target) == CreatureSize.Small ||
                            GetCreatureSize(target) == CreatureSize.Medium
                            )
                        {
                            ApplyEffectToObject(DurationType.Temporary, EffectKnockdown(), target, 6f);
                        }
                    }

                    Enmity.ModifyEnmityOnAll(activator, 1);
                    CombatPoint.AddCombatPointToAllTagged(activator, SkillType.Force, 3);
                });
        }

        private static void ForcePush3(AbilityBuilder builder)
        {
            builder.Create(Feat.ForceLeap1, PerkType.ForcePush)
                .Name("Force Push III")
                .HasRecastDelay(RecastGroup.ForcePush, 30f)
                .HasActivationDelay(2.0f)
                .RequirementFP(4)
                .IsCastedAbility()
                .DisplaysVisualEffectWhenActivating()
                .HasImpactAction((activator, target, level) =>
                {
                    if (!Ability.GetAbilityResisted(activator, target, AbilityType.Intelligence, AbilityType.Wisdom))
                    {
                        if (GetCreatureSize(target) == CreatureSize.Small ||
                            GetCreatureSize(target) == CreatureSize.Medium ||
                            GetCreatureSize(target) == CreatureSize.Large
                            )
                        {
                            ApplyEffectToObject(DurationType.Temporary, EffectKnockdown(), target, 6f);
                        }
                    }

                    Enmity.ModifyEnmityOnAll(activator, 1);
                    CombatPoint.AddCombatPointToAllTagged(activator, SkillType.Force, 3);
                });
        }

        private static void ForcePush4(AbilityBuilder builder)
        {
            builder.Create(Feat.ForceLeap1, PerkType.ForcePush)
                .Name("Force Push IV")
                .HasRecastDelay(RecastGroup.ForcePush, 30f)
                .HasActivationDelay(4.0f)
                .RequirementFP(5)
                .IsCastedAbility()
                .DisplaysVisualEffectWhenActivating()
                .HasImpactAction((activator, target, level) =>
                {
                    if (!Ability.GetAbilityResisted(activator, target, AbilityType.Intelligence, AbilityType.Wisdom))
                    {
                        ApplyEffectToObject(DurationType.Temporary, EffectKnockdown(), target, 6f);
                    }

                    Enmity.ModifyEnmityOnAll(activator, 1);
                    CombatPoint.AddCombatPointToAllTagged(activator, SkillType.Force, 3);
                });
        }
    }
}