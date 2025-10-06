using SWLOR.Shared.Domain.Space.Contracts;
using SWLOR.Shared.Domain.Space.Events;
using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Events.Area;
using SWLOR.Shared.Events.Events.Creature;
using SWLOR.Shared.Events.Events.Module;
using SWLOR.Shared.Events.Events.NWNX;

namespace SWLOR.Component.Space.EventHandlers
{
    public class SpaceEventHandler
    {
        private readonly ISpaceService _spaceService;

        public SpaceEventHandler(ISpaceService spaceService)
        {
            _spaceService = spaceService;
        }

        /// <summary>
        /// When the module loads, cache all space data into memory.
        /// </summary>
        [ScriptHandler<OnModuleCacheBefore>]
        public void LoadSpaceSystem()
        {
            _spaceService.LoadSpaceSystem();
        }

        /// <summary>
        /// When a player enters the server, reload TLK strings and warp player inside ship if needed.
        /// </summary>
        [ScriptHandler<OnModuleEnter>]
        public void EnterServer()
        {
            _spaceService.EnterServer();
        }

        /// <summary>
        /// When a player exits the server, clone their ship if in space mode.
        /// </summary>
        [ScriptHandler<OnModuleExit>]
        public void ExitServer()
        {
            _spaceService.ExitServer();
        }

        /// <summary>
        /// When the module loads, register all landing points.
        /// </summary>
        [ScriptHandler<OnModuleLoad>]
        public void LoadLandingPoints()
        {
            _spaceService.LoadLandingPoints();
        }

        /// <summary>
        /// Handles swapping a player's target to the object they attempted to attack using NWN's combat system.
        /// </summary>
        [ScriptHandler<OnInputAttackObjectBefore>]
        public void SelectTarget()
        {
            _spaceService.SelectTarget();
        }

        /// <summary>
        /// When a player enters a space area, update the property's space position.
        /// </summary>
        [ScriptHandler<OnAreaEnter>]
        public void UpdateSpacePosition()
        {
            _spaceService.UpdateSpacePosition();
        }

        /// <summary>
        /// When a creature leaves an area, their current target is cleared.
        /// </summary>
        [ScriptHandler<OnAreaExit>]
        public void ClearTargetOnAreaExit()
        {
            _spaceService.ClearTargetOnAreaExit();
        }

        /// <summary>
        /// When the ship computer is used, check the user's permissions.
        /// If player has permission and the ship isn't currently being controlled by another player,
        /// send the player into space mode.
        /// </summary>
        [ScriptHandler<OnUseShipComputer>]
        public void UseShipComputer()
        {
            _spaceService.UseShipComputer();
        }

        /// <summary>
        /// When an item is examined, handle ship and ship module items by adding descriptions and prerequisites.
        /// This combines the functionality of ExamineShipModuleItem and ExamineShipItem.
        /// </summary>
        [ScriptHandler<OnExamineObjectBefore>]
        public void ExamineShipAndModuleItems()
        {
            _spaceService.ExamineShipModuleItem();
            _spaceService.ExamineShipItem();
        }

        /// <summary>
        /// When a ship module's feat is used, execute the currently equipped module's custom code.
        /// </summary>
        [ScriptHandler<OnFeatUseBefore>]
        public void HandleShipModuleFeats()
        {
            _spaceService.HandleShipModuleFeats();
        }

        /// <summary>
        /// When a creature clicks on a space object, target that object.
        /// </summary>
        [ScriptHandler<OnSpaceTarget>]
        public void TargetSpaceObject()
        {
            _spaceService.TargetSpaceObject();
        }

        /// <summary>
        /// When a creature spawns, track it in the cache.
        /// </summary>
        [ScriptHandler<OnCreatureSpawnBefore>]
        public void CreatureSpawn()
        {
            _spaceService.CreatureSpawn();
        }

        /// <summary>
        /// When a creature dies, remove it from the cache.
        /// </summary>
        [ScriptHandler<OnCreatureDeathAfter>]
        public void CreatureDeath()
        {
            _spaceService.CreatureDeath();
        }

        /// <summary>
        /// Applies death to a creature in space mode.
        /// </summary>
        [ScriptHandler<OnModuleDeath>]
        public void ApplyDeath()
        {
            _spaceService.ApplyDeath();
        }

        /// <summary>
        /// When a player attempts to stealth while in space mode,
        /// exit the stealth mode and send an error message.
        /// </summary>
        [ScriptHandler<OnStealthEnterBefore>]
        public void PreventSpaceStealth()
        {
            _spaceService.PreventSpaceStealth();
        }
    }
}
