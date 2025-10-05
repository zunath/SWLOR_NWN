using SWLOR.Component.Character.Contracts;
using SWLOR.Component.Character.Service;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Character.Events;
using SWLOR.Shared.Domain.Inventory.Contracts;
using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Events.Module;
using SWLOR.Shared.Events.Events.NWNX;

namespace SWLOR.Component.Character.EventHandlers
{
    internal class CharacterServiceEventHandlers
    {
        private readonly IActivityService _activityService;
        private readonly IClientVersionCheck _clientVersionCheckService;
        private readonly ICurrencyService _currencyService;
        private readonly IFactionService _factionService;
        private readonly IPartyService _partyService;
        private readonly IPlayerInitializationService _playerInitializationService;
        private readonly PlayerRestService _playerRestService;
        private readonly IRaceService _raceService;
        private readonly ITargetingService _targetingService;
        private readonly IAchievementService _achievementService;

        public CharacterServiceEventHandlers(
            IActivityService activityService,
            IClientVersionCheck clientVersionCheckService,
            ICurrencyService currencyService,
            IFactionService factionService,
            IPartyService partyService,
            IPlayerInitializationService playerInitializationService,
            PlayerRestService playerRestService,
            IRaceService raceService,
            ITargetingService targetingService,
            IAchievementService achievementService)
        {
            _activityService = activityService;
            _clientVersionCheckService = clientVersionCheckService;
            _currencyService = currencyService;
            _factionService = factionService;
            _partyService = partyService;
            _playerInitializationService = playerInitializationService;
            _playerRestService = playerRestService;
            _raceService = raceService;
            _targetingService = targetingService;
            _achievementService = achievementService;
        }

        /// <summary>
        /// When a player enters the module, wipe their temporary "busy" status.
        /// </summary>
        [ScriptHandler<OnModuleEnter>]
        public void WipeStatusOnEntry()
        {
            _activityService.WipeStatusOnEntry();
        }

        /// <summary>
        /// Handles player initialization when they enter the module.
        /// </summary>
        [ScriptHandler<OnModuleEnter>]
        public void InitializePlayer()
        {
            _playerInitializationService.InitializePlayer();
        }

        /// <summary>
        /// When a player enters the server, apply the proper scaling to their character.
        /// </summary>
        [ScriptHandler<OnModuleEnter>]
        public void ApplyWookieeScaling()
        {
            _raceService.ApplyWookieeScaling();
        }

        /// <summary>
        /// When the module loads, cache all default race appearances.
        /// </summary>
        [ScriptHandler<OnModuleLoad>]
        public void LoadRaces()
        {
            _raceService.LoadRaces();
        }

        /// <summary>
        /// When the module loads, reserve GUI IDs for achievements.
        /// </summary>
        [ScriptHandler<OnModuleLoad>]
        public void ReserveGuiIds()
        {
            _achievementService.ReserveGuiIds();
        }

        /// <summary>
        /// When the module caches, cache all faction details into memory.
        /// </summary>
        [ScriptHandler<OnModuleCacheBefore>]
        public void LoadFactions()
        {
            _factionService.LoadFactions();
        }

        /// <summary>
        /// When the module caches, read all achievement types and store them into the cache.
        /// </summary>
        [ScriptHandler<OnModuleCacheBefore>]
        public void LoadAchievements()
        {
            _achievementService.LoadAchievements();
        }

        /// <summary>
        /// When the module caches, cache all currency details into memory.
        /// </summary>
        [ScriptHandler<OnModuleCacheBefore>]
        public void CacheCurrencies()
        {
            _currencyService.CacheCurrencies();
        }

        /// <summary>
        /// When a player leaves the server, remove them from the party caches.
        /// </summary>
        [ScriptHandler<OnModuleExit>]
        public void LeaveServer()
        {
            _partyService.LeaveServer();
        }

        /// <summary>
        /// When a player connects to the server, perform a version check on their client.
        /// </summary>
        [ScriptHandler<OnClientConnectBefore>]
        public void CheckVersion()
        {
            _clientVersionCheckService.CheckVersion();
        }

        /// <summary>
        /// When a player dies, wipe their temporary "busy" status.
        /// </summary>
        [ScriptHandler<OnModuleDeath>]
        public void WipeStatusOnDeath()
        {
            _activityService.WipeStatusOnDeath();
        }

        /// <summary>
        /// When a member of a party accepts an invitation, add them to the caches.
        /// </summary>
        [ScriptHandler<OnPartyAcceptInvitationBefore>]
        public void JoinParty()
        {
            _partyService.JoinParty();
        }

        /// <summary>
        /// When an associate (droid, pet, henchman, etc.) joins a party, add them to the caches.
        /// </summary>
        [ScriptHandler<OnAssociateAddBefore>]
        public void AssociateJoinParty()
        {
            _partyService.AssociateJoinParty();
        }

        /// <summary>
        /// When an associate (droid, pet, henchman, etc.) is removed from the party or leaves, remove them from the caches.
        /// </summary>
        [ScriptHandler<OnAssociateRemoveBefore>]
        public void AssociateLeaveParty()
        {
            _partyService.AssociateLeaveParty();
        }

        /// <summary>
        /// When a member of a party leaves, update the caches.
        /// </summary>
        [ScriptHandler<OnPartyLeaveBefore>]
        public void LeaveParty()
        {
            _partyService.LeaveParty();
        }

        /// <summary>
        /// When the leader of a party changes, update the caches.
        /// </summary>
        [ScriptHandler<OnPartyTransferLeadershipBefore>]
        public void TransferLeadership()
        {
            _partyService.TransferLeadership();
        }

        /// <summary>
        /// When a player targets an object, execute the assigned action.
        /// </summary>
        [ScriptHandler<OnModulePlayerTarget>]
        public void RunTargetedItemAction()
        {
            _targetingService.RunTargetedItemAction();
        }

        /// <summary>
        /// When a player rests, cancel the NWN resting mechanic and apply our custom Rest status effect
        /// which handles recovery of HP, FP, and STM.
        /// </summary>
        [ScriptHandler<OnModuleRest>]
        public void HandleRest()
        {
            _playerRestService.HandleRest();
        }

        /// <summary>
        /// When a player enters a rest trigger, flag them and notify them they can rest.
        /// </summary>
        [ScriptHandler<OnRestTriggerEnter>]
        public void EnterRestTrigger()
        {
            _playerRestService.EnterRestTrigger();
        }

        /// <summary>
        /// When a player exits a rest trigger, unflag them and notify them they can no longer rest.
        /// </summary>
        [ScriptHandler<OnRestTriggerExit>]
        public void ExitRestTrigger()
        {
            _playerRestService.ExitRestTrigger();
        }
    }
}
