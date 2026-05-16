using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Spear
{
    public class ImprovedAttentivenessAbilityDefinition : SpearActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            builder
                .Create(FeatType.ImprovedAttentiveness1, PerkType.ImprovedAttentiveness)
                .Name("Improved Attentiveness")
                .Level(1)
                .SkillType(SkillType.Spear)
                .HasActivationDelay(2f)
                .HasRecastDelay(RecastGroup.ImprovedAttentiveness, 300f)
                .HasImpactAction((activator, target, level, targetLocation) =>
                {
                    foreach (var partyMember in Party.GetAllPartyMembers(activator))
                    {
                        if (partyMember == activator || !GetIsObjectValid(partyMember))
                            continue;

                        StatusEffect.ApplyStatusEffect(
                            activator,
                            partyMember,
                            typeof(ImprovedAttentivenessStatusEffect),
                            60f);
                    }
                })
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementStamina(8);

            return builder.Build();
        }
    }
}
