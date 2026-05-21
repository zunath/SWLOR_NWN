using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.HeavyVibroblade
{
    public class AbsoluteDefenseAbilityDefinition : HeavyVibrobladeActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            AbsoluteDefense(builder);

            return builder.Build();
        }

        private static void AbsoluteDefense(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.AbsoluteDefense1, PerkType.AbsoluteDefense)
                .Name("Absolute Defense")
                .Level(1)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.Capstone, CapstoneAbility.RecastDelaySeconds)
                .HasImpactAction((activator, target, level, targetLocation) =>
                {
                    foreach (var partyMember in Party.GetAllPartyMembers(activator))
                    {
                        if (!GetIsObjectValid(partyMember))
                            continue;

                        StatusEffect.ApplyStatusEffect(activator, partyMember, typeof(AbsoluteDefenseStatusEffect), CapstoneAbility.ActiveDurationSeconds);
                        Ability.ApplyTemporaryImmunity(partyMember, CapstoneAbility.ActiveDurationSeconds, ImmunityType.Knockdown);
                        Ability.ApplyTemporaryImmunity(partyMember, CapstoneAbility.ActiveDurationSeconds, ImmunityType.Dazed);
                    }
                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Holy_Aid), activator);
                })
                .SkillType(SkillType.HeavyVibroblade)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementStamina(CapstoneAbility.StaminaCost);
        }
    }
}
