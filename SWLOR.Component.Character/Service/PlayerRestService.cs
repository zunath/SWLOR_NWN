using Microsoft.Extensions.DependencyInjection;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.StatusEffect.Contracts;
using SWLOR.Shared.Domain.StatusEffect.Enums;
using SWLOR.Shared.Events.Events.Player;

namespace SWLOR.Component.Character.Service
{
    public class PlayerRestService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IEventAggregator _eventAggregator;
        
        // Lazy-loaded services to break circular dependencies
        private readonly Lazy<IStatusEffectService> _statusEffectService;
        private readonly Lazy<IPartyService> _partyService;

        public PlayerRestService(IServiceProvider serviceProvider, IEventAggregator eventAggregator)
        {
            _serviceProvider = serviceProvider;
            _eventAggregator = eventAggregator;
            
            // Initialize lazy services
            _statusEffectService = new Lazy<IStatusEffectService>(() => _serviceProvider.GetRequiredService<IStatusEffectService>());
            _partyService = new Lazy<IPartyService>(() => _serviceProvider.GetRequiredService<IPartyService>());
        }
        
        // Lazy-loaded services to break circular dependencies
        private IStatusEffectService StatusEffectService => _statusEffectService.Value;
        private IPartyService PartyService => _partyService.Value;

        /// <summary>
        /// When a player rests, cancel the NWN resting mechanic and apply our custom Rest status effect
        /// which handles recovery of HP, FP, and STM.
        /// </summary>
        public void HandleRest()
        {
            var player = GetLastPCRested();

            string CanRest()
            {
                var area = GetArea(player);
                // Is the activator in a dungeon?
                if (GetLocalBool(area, "IS_DUNGEON"))
                {
                    // Are they inside a rest trigger?
                    if (!GetLocalBool(player, "CAN_REST"))
                    {
                        return "It is not safe to rest here.";
                    }
                }

                // Is activator in combat?
                if (GetIsInCombat(player))
                {
                    return "You cannot rest during combat.";
                }

                // Is an enemy nearby the activator?
                var nearestEnemy = GetNearestCreature(CreatureType.Reputation, (int)ReputationType.Enemy, player);
                if (GetIsObjectValid(nearestEnemy) && GetDistanceBetween(player, nearestEnemy) <= 20f)
                {
                    return "You cannot rest while enemies are nearby.";
                }

                // Are any of their party members in combat?
                foreach (var member in PartyService.GetAllPartyMembersWithinRange(player, 20f))
                {
                    if (GetIsInCombat(member))
                    {
                        return "You cannot rest during combat.";
                    }
                }

                return string.Empty;
            }

            var type = GetLastRestEventType();

            if (type != RestEventType.Started)
                return;

            AssignCommand(player, () => ClearAllActions());

            var errorMessage = CanRest();
            if (!string.IsNullOrWhiteSpace(errorMessage))
            {
                SendMessageToPC(player, errorMessage);
                return;
            }

            StatusEffectService.Apply(player, player, StatusEffectType.Rest, 0f);

            var henchman = GetAssociate(AssociateType.Henchman, player);
            if (GetIsObjectValid(henchman))
            {
                StatusEffectService.Apply(henchman, henchman, StatusEffectType.Rest, 0f);
            }

            _eventAggregator.Publish(new OnPlayerRestStarted(), player);
        }

        /// <summary>
        /// When a player enters a rest trigger, flag them and notify them they can rest.
        /// This will only occur if they are inside a dungeon because they can rest anywhere they want outside of a dungeon.
        /// </summary>
        public void EnterRestTrigger()
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
        public void ExitRestTrigger()
        {
            var player = GetExitingObject();
            if (!GetIsPC(player) || GetIsDM(player)) return;

            DeleteLocalBool(player, "CAN_REST");
            SendMessageToPC(player, "You leave the safe location.");
        }
    }
}
