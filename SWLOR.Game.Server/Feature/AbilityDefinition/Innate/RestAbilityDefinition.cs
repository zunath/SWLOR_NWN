using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Innate
{
    public class RestAbilityDefinition: IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            Rest(builder);

            return builder.Build();
        }

        /// <summary>
        /// When a player enters a rest trigger, flag them and notify them they can rest.
        /// This will only occur if they are inside a dungeon because they can rest anywhere they want outside of a dungeon.
        /// </summary>
        [NWNEventHandler("rest_trg_enter")]
        public static void EnterRestTrigger()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player) || GetIsDM(player)) return;

            SetLocalBool(player, "CAN_REST", true);
            SendMessageToPC(player, "This looks like a safe place to rest.");
        }

        /// <summary>
        /// When a player exits a rest trigger, unflag them and notify them they can no longer rest.
        /// This will only occur if they are inside a dungeon.
        /// </summary>
        [NWNEventHandler("rest_trg_exit")]
        public static void ExitRestTrigger()
        {
            var player = GetExitingObject();
            if (!GetIsPC(player) || GetIsDM(player)) return;

            DeleteLocalBool(player, "CAN_REST");
            SendMessageToPC(player, "You leave the safe location.");
        }

        private void Rest(AbilityBuilder builder)
        {
            builder.Create(FeatType.Rest, PerkType.Invalid)
                .Name("Rest")
                .IsCastedAbility()
                .HasRecastDelay(RecastGroup.Rest, 30f)
                .HasCustomValidation((activator, target, level) =>
                {
                    var area = GetArea(activator);
                    // Is the activator in a dungeon?
                    if (GetLocalBool(area, "IS_DUNGEON"))
                    {
                        // Are they inside a rest trigger?
                        if (!GetLocalBool(activator, "CAN_REST"))
                        {
                            return "It is not safe to rest here.";
                        }
                    }

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
