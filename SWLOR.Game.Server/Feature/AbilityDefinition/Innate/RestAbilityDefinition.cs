using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Innate
{
    public class RestAbilityDefinition: IAbilityListDefinition
    {
        public Dictionary<Feat, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            Rest(builder);

            return builder.Build();
        }

        private void Rest(AbilityBuilder builder)
        {
            builder.Create(Feat.Rest, PerkType.Invalid)
                .Name("Rest")
                .IsCastedAbility()
                .HasRecastDelay(RecastGroup.Rest, 30f)
                .HasCustomValidation((activator, target, level) =>
                {
                    // Is activator in combat?
                    if (GetIsInCombat(activator))
                    {
                        return "You cannot rest during combat.";
                    }

                    // Are any of their party members in combat?
                    foreach (var member in Party.GetAllPartyMembersWithinRange(activator, 20f))
                    {
                        if (GetIsInCombat(member))
                        {
                            return "You cannot rest during combat.";
                        }
                    }

                    return string.Empty;
                })
                .HasImpactAction((activator, target, level) =>
                {
                    AssignCommand(activator, () =>
                    {
                        ActionPlayAnimation(Animation.LoopingSitCross, 1f, 9999f);
                    });

                    StatusEffect.Apply(activator, activator, StatusEffectType.Rest, 0f);
                });
        }
    }
}
