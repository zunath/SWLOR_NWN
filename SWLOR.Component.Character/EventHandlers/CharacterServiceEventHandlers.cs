using SWLOR.Component.Character.Contracts;
using SWLOR.Component.Character.Service;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Inventory.Contracts;
using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Events.Module;

namespace SWLOR.Component.Character.EventHandlers
{
    internal class CharacterServiceEventHandlers
    {
        private readonly IActivityService _activityService;
        private readonly ICurrencyService _currencyService;
        private readonly IFactionService _factionService;
        private readonly IPartyService _partyService;
        private readonly IPlayerInitializationService _playerInitializationService;
        private readonly IRaceService _raceService;
        private readonly ITargetingService _targetingService;
        private readonly IAchievementService _achievementService;

        public CharacterServiceEventHandlers(
            IActivityService activityService,
            ICurrencyService currencyService,
            IFactionService factionService,
            IPartyService partyService,
            IPlayerInitializationService playerInitializationService,
            IRaceService raceService,
            ITargetingService targetingService,
            IAchievementService achievementService)
        {
            _activityService = activityService;
            _currencyService = currencyService;
            _factionService = factionService;
            _partyService = partyService;
            _playerInitializationService = playerInitializationService;
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
    }
}
