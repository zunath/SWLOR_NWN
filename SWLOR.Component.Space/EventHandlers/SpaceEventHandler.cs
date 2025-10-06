using SWLOR.Shared.Domain.Space.Contracts;
using SWLOR.Shared.Domain.Space.Events;
using SWLOR.Shared.Events.Events.Area;
using SWLOR.Shared.Events.Events.Creature;
using SWLOR.Shared.Events.Events.Module;
using SWLOR.Shared.Events.Events.NWNX;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Events.Events.Player;

namespace SWLOR.Component.Space.EventHandlers
{
    public class SpaceEventHandler
    {
        private readonly ISpaceService _spaceService;

        public SpaceEventHandler(
            ISpaceService spaceService,
            IEventAggregator eventAggregator)
        {
            _spaceService = spaceService;

            // Subscribe to events
            eventAggregator.Subscribe<OnModuleCacheBefore>(e => LoadSpaceSystem());
            eventAggregator.Subscribe<OnModuleEnter>(e => EnterServer());
            eventAggregator.Subscribe<OnModuleExit>(e => ExitServer());
            eventAggregator.Subscribe<OnModuleCacheBefore>(e => LoadLandingPoints());
            eventAggregator.Subscribe<OnSpaceTarget>(e => SelectTarget());
            eventAggregator.Subscribe<OnModuleHeartbeat>(e => UpdateSpacePosition());
            eventAggregator.Subscribe<OnAreaExit>(e => ClearTargetOnAreaExit());
            eventAggregator.Subscribe<OnUseShipComputer>(e => UseShipComputer());
            eventAggregator.Subscribe<OnExamineObjectBefore>(e => ExamineShipAndModuleItems());
            eventAggregator.Subscribe<OnUseFeatBefore>(e => HandleShipModuleFeats());
            eventAggregator.Subscribe<OnSpaceTarget>(e => TargetSpaceObject());
            eventAggregator.Subscribe<OnCreatureSpawnAfter>(e => CreatureSpawn());
            eventAggregator.Subscribe<OnCreatureDeathAfter>(e => CreatureDeath());
            eventAggregator.Subscribe<OnPlayerDeath>(e => ApplyDeath());
            eventAggregator.Subscribe<OnUseFeatBefore>(e => PreventSpaceStealth());
        }

        /// <summary>
        /// When the module loads, cache all space data into memory.
        /// </summary>
        public void LoadSpaceSystem()
        {
            _spaceService.LoadSpaceSystem();
        }

        /// <summary>
        /// When a player enters the server, reload TLK strings and warp player inside ship if needed.
        /// </summary>
        public void EnterServer()
        {
            _spaceService.EnterServer();
        }

        /// <summary>
        /// When a player exits the server, clone their ship if in space mode.
        /// </summary>
        public void ExitServer()
        {
            _spaceService.ExitServer();
        }

        /// <summary>
        /// When the module loads, register all landing points.
        /// </summary>
        public void LoadLandingPoints()
        {
            _spaceService.LoadLandingPoints();
        }

        /// <summary>
        /// Handles swapping a player's target to the object they attempted to attack using NWN's combat system.
        /// </summary>
        public void SelectTarget()
        {
            _spaceService.SelectTarget();
        }

        /// <summary>
        /// When a player enters a space area, update the property's space position.
        /// </summary>
        public void UpdateSpacePosition()
        {
            _spaceService.UpdateSpacePosition();
        }

        /// <summary>
        /// When a creature leaves an area, their current target is cleared.
        /// </summary>
        public void ClearTargetOnAreaExit()
        {
            _spaceService.ClearTargetOnAreaExit();
        }

        /// <summary>
        /// When the ship computer is used, check the user's permissions.
        /// If player has permission and the ship isn't currently being controlled by another player,
        /// send the player into space mode.
        /// </summary>
        public void UseShipComputer()
        {
            _spaceService.UseShipComputer();
        }

        /// <summary>
        /// When an item is examined, handle ship and ship module items by adding descriptions and prerequisites.
        /// This combines the functionality of ExamineShipModuleItem and ExamineShipItem.
        /// </summary>
        public void ExamineShipAndModuleItems()
        {
            _spaceService.ExamineShipModuleItem();
            _spaceService.ExamineShipItem();
        }

        /// <summary>
        /// When a ship module's feat is used, execute the currently equipped module's custom code.
        /// </summary>
        public void HandleShipModuleFeats()
        {
            _spaceService.HandleShipModuleFeats();
        }

        /// <summary>
        /// When a creature clicks on a space object, target that object.
        /// </summary>
        public void TargetSpaceObject()
        {
            _spaceService.TargetSpaceObject();
        }

        /// <summary>
        /// When a creature spawns, track it in the cache.
        /// </summary>
        public void CreatureSpawn()
        {
            _spaceService.CreatureSpawn();
        }

        /// <summary>
        /// When a creature dies, remove it from the cache.
        /// </summary>
        public void CreatureDeath()
        {
            _spaceService.CreatureDeath();
        }

        /// <summary>
        /// Applies death to a creature in space mode.
        /// </summary>
        public void ApplyDeath()
        {
            _spaceService.ApplyDeath();
        }

        /// <summary>
        /// When a player attempts to stealth while in space mode,
        /// exit the stealth mode and send an error message.
        /// </summary>
        public void PreventSpaceStealth()
        {
            _spaceService.PreventSpaceStealth();
        }
    }
}
