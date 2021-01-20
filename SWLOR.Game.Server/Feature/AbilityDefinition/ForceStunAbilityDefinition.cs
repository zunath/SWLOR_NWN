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
    public class ForceStunAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<Feat, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            ForceStun1(builder);
            ForceStun2(builder);
            ForceStun3(builder);

            return builder.Build();
        }

        private static void ForceStun1(AbilityBuilder builder)
        {
            builder.Create(Feat.ForceStun1, PerkType.ForceStun)
                .Name("Force Stun I")
                .HasRecastDelay(RecastGroup.ForceStun, 60f * 5f)
                .HasActivationDelay(2.0f)
                .RequirementFP(4)
                .IsConcentrationAbility(StatusEffectType.ForceStun1)
                .DisplaysVisualEffectWhenActivating()
                .HasImpactAction((activator, target, level) =>
                {
                    if (!Ability.GetAbilityResisted(activator, target, AbilityType.Intelligence, AbilityType.Wisdom))
                    {
                        ApplyEffectToObject(DurationType.Temporary, EffectKnockdown(), target, 6f);
                    }
                    else ApplyEffectToObject(DurationType.Temporary, EffectSlow(), target, 6.0f);

                    Enmity.ModifyEnmityOnAll(activator, 1);
                    CombatPoint.AddCombatPointToAllTagged(activator, SkillType.Force, 3);
                }); 
        }

        private static void ForceStun2(AbilityBuilder builder)
        {
            builder.Create(Feat.ForceStun2, PerkType.ForceStun)
                .Name("Force Stun II")
                .HasRecastDelay(RecastGroup.ForceStun, 60f * 5f)
                .HasActivationDelay(2.0f)
                .RequirementFP(6)
                .IsConcentrationAbility(StatusEffectType.ForceStun1)
                .IsConcentrationAbility(StatusEffectType.Bleed)
                .DisplaysVisualEffectWhenActivating();
        }

        private static void ForceStun3(AbilityBuilder builder)
        {
            builder.Create(Feat.ForceStun3, PerkType.ForceStun)
                .Name("Force Stun III")
                .HasRecastDelay(RecastGroup.ForceStun, 60f * 5f)
                .HasActivationDelay(2.0f)
                .RequirementFP(8)
                .IsConcentrationAbility(StatusEffectType.ForceStun1)
                .DisplaysVisualEffectWhenActivating();
        }
    }
}