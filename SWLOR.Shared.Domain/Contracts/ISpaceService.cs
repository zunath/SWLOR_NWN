using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Enums;
using SWLOR.Shared.Domain.Model;

namespace SWLOR.Shared.Domain.Contracts
{

    public interface ISpaceService
    {
        Dictionary<FeatType, ShipModuleFeat> ShipModuleFeats { get; }

        /// <summary>
        /// When the module loads, cache all space data into memory.
        /// </summary>
        void LoadSpaceSystem();

        void EnterServer();
        void ExitServer();

        /// <summary>
        /// When the module loads, 
        /// </summary>
        void LoadLandingPoints();

        /// <summary>
        /// Registers a waypoint as a landing point.
        /// Once added, this location will become available for players to land at.
        /// </summary>
        /// <param name="waypoint">The waypoint to register.</param>
        /// <param name="area">The area to use for registration.</param>
        /// <param name="isNPC">If true, will be marked as an NPC dock. Otherwise will be marked as a PC dock.</param>
        /// <param name="propertyId">If specified, references the world property Id of this landing point.</param>
        void RegisterLandingPoint(uint waypoint, uint area, bool isNPC, string propertyId);

        /// <summary>
        /// Removes a waypoint from the landing point registration.
        /// Once removed, this location will no longer be available to land at.
        /// </summary>
        /// <param name="waypoint">The waypoint to remove.</param>
        /// <param name="cityArea">The area to remove from.</param>
        void RemoveLandingPoint(uint waypoint, uint cityArea);

        /// <summary>
        /// Retrieves all of the registered dock points for a given planet.
        /// </summary>
        /// <param name="planetType">The planet to search.</param>
        /// <returns>A dictionary of dock points.</returns>
        Dictionary<string, ShipDockPoint> GetDockPointsByPlanet(PlanetType planetType);

        /// <summary>
        /// Retrieves a ship's detail by type.
        /// </summary>
        /// <param name="itemTag">The item tag to search for.</param>
        /// <returns>A ship detail matching the type.</returns>
        ShipDetail GetShipDetailByItemTag(string itemTag);

        /// <summary>
        /// Retrieves a ship module's detail by type.
        /// </summary>
        /// <param name="itemTag">The item tag of the ship module to search for.</param>
        /// <returns>A ship module detail matching the type.</returns>
        ShipModuleDetail GetShipModuleDetailByItemTag(string itemTag);

        /// <summary>
        /// Determines whether an item's tag is registered to a ship module.
        /// </summary>
        /// <param name="itemTag">The item tag of the ship to search for.</param>
        /// <returns>true if registered, false otherwise</returns>
        bool IsRegisteredShip(string itemTag);

        /// <summary>
        /// Determines whether an item's tag is registered to a ship module.
        /// </summary>
        /// <param name="itemTag">The item tag of the ship module to search for.</param>
        /// <returns>true if registered, false otherwise</returns>
        bool IsRegisteredShipModule(string itemTag);

        /// <summary>
        /// Determines whether an item is a ship deed.
        /// </summary>
        /// <param name="item">The item to check</param>
        /// <returns>true if item is a ship deed, false otherwise</returns>
        bool IsItemShip(uint item);

        /// <summary>
        /// Determines whether an item is a ship module.
        /// </summary>
        /// <param name="item">The item to check</param>
        /// <returns>true if item is a ship module, false otherwise</returns>
        bool IsItemShipModule(uint item);

        /// <summary>
        /// Determines whether an item is starship ammo.
        /// </summary>
        bool IsStarshipAmmo(uint item);

        /// <summary>
        /// Retrieves the currently selected target of a player. Returns OBJECT_INVALID if not set.
        /// </summary>
        /// <param name="player">The player whose target to retrieve.</param>
        /// <returns>The selected target or OBJECT_INVALID.</returns>
        (uint, ShipStatus) GetCurrentTarget(uint player);

        /// <summary>
        /// Handles swapping a player's target to the object they attempted to attack using NWN's combat system.
        /// </summary>
        void SelectTarget();

        /// <summary>
        /// When a player enters a space area, update the property's space position.
        /// </summary>
        void UpdateSpacePosition();

        /// <summary>
        /// When a creature leaves an area, their current target is cleared.
        /// </summary>
        void ClearTargetOnAreaExit();

        /// <summary>
        /// When the ship computer is used, check the user's permissions.
        /// If player has permission and the ship isn't currently being controlled by another player,
        /// send the player into space mode.
        /// </summary>
        void UseShipComputer();

        /// <summary>
        /// Retrieves the slot number (1-30) of the ship module feat.
        /// </summary>
        /// <param name="feat">The feat to check</param>
        /// <returns>The slot number (1-30) of the ship module feat.</returns>
        int GetFeatSlotNumber(FeatType feat);

        /// <summary>
        /// Retrieves the associated feat given a high slot number.
        /// Must be in the range of 1-10
        /// </summary>
        /// <param name="slot">The slot number. Range is 1-10</param>
        /// <returns>The feat associated with the high slot number</returns>
        FeatType HighSlotToFeat(int slot);

        /// <summary>
        /// Retrieves the associated feat given a low slot number.
        /// Must be in the range of 1-10
        /// </summary>
        /// <param name="slot">The slot number. Range is 1-10</param>
        /// <returns>The feat associated with the low slot number.</returns>
        FeatType LowSlotToFeat(int slot);

        /// <summary>
        /// Converts a high slot feat to its slot number.
        /// </summary>
        /// <param name="feat">The feat to convert</param>
        /// <returns>The slot number associated with the feat.</returns>
        int HighFeatToSlot(FeatType feat);

        /// <summary>
        /// Converts a low slot feat to its slot number.
        /// </summary>
        /// <param name="feat">The feat to convert</param>
        /// <returns>The slot number associated with the feat.</returns>
        int LowFeatToSlot(FeatType feat);

        /// <summary>
        /// Checks whether a player is in Space mode.
        /// </summary>
        /// <param name="player">The player to check</param>
        /// <returns>true if player is in space mode, false otherwise</returns>
        bool IsPlayerInSpaceMode(uint player);

        /// <summary>
        /// Makes the player enter space mode which changes the player's appearance, loads the ship's hot bar, etc.
        /// </summary>
        /// <param name="player">The player entering space mode.</param>
        /// <param name="shipId">The Id of the ship to enter space with.</param>
        void EnterSpaceMode(uint player, string shipId);

        /// <summary>
        /// When a player enters the module, if they were piloting a ship, send them to the instance.
        /// Note that if the server rebooted since they logged off, the normal persistent locations script
        /// will take over and send them to the last dock they were at.
        /// </summary>
        void WarpPlayerInsideShip(uint player);

        /// <summary>
        /// Makes the player exit space mode which reverts the player's appearance, loads the character's hot bar, etc.
        /// </summary>
        /// <param name="player">The player exiting space mode.</param>
        void ExitSpaceMode(uint player);

        /// <summary>
        /// Determines if player can use the specified ship.
        /// </summary>
        /// <param name="player">The player to check</param>
        /// <param name="playerShip">The player ship status to check</param>
        /// <returns>true if all requirements are met, false otherwise</returns>
        bool CanPlayerUseShip(uint player, ShipStatus playerShip);

        /// <summary>
        /// Determines if player can use a ship module by its tag.
        /// </summary>
        /// <param name="player">The player to check</param>
        /// <param name="itemTag">The ship module item tag</param>
        /// <returns>true if player can use the module, false otherwise</returns>
        bool CanPlayerUseShipModule(uint player, string itemTag);

        /// <summary>
        /// When a ship module item is examined,
        /// append the configured description to the item's description and add prerequisite perk item properties.
        /// </summary>
        void ExamineShipModuleItem();

        /// <summary>
        /// When a ship item is examined, add the prerequisite perk item properties.
        /// </summary>
        void ExamineShipItem();

        /// <summary>
        /// When a ship module's feat is used, execute the currently equipped module's custom code.
        /// </summary>
        void HandleShipModuleFeats();

        void RestoreShield(uint creature, ShipStatus shipStatus, int amount);
        void ReduceShield(uint creature, ShipStatus shipStatus, int amount);
        void RestoreHull(uint creature, ShipStatus shipStatus, int amount);
        void ReduceHull(uint creature, ShipStatus shipStatus, int amount);
        void RestoreCapacitor(uint creature, ShipStatus shipStatus, int amount);
        void ReduceCapacitor(uint creature, ShipStatus shipStatus, int amount);

        /// <summary>
        /// When a creature spawns, track it in the cache.
        /// </summary>
        void CreatureSpawn();

        /// <summary>
        /// When a creature dies, remove it from the cache.
        /// </summary>
        void CreatureDeath();

        /// <summary>
        /// Retrieves the ship status of a given creature.
        /// If creature is an NPC, it will be retrieved from the cache.
        /// If creature is a PC, it will be retrieved from the database.
        /// </summary>
        /// <param name="creature">The creature to get the status of</param>
        /// <returns>A ship status containing current statistics about a creature's ship.</returns>
        ShipStatus GetShipStatus(uint creature);

        /// <summary>
        /// Calculates attacker's chance to hit target.
        /// </summary>
        /// <param name="attacker">The creature attacking.</param>
        /// <param name="defender">The creature being targeted.</param>
        int CalculateChanceToHit(uint attacker, uint defender);

        /// <summary>
        /// Calculates the attack of a ship.
        /// Does not take into account any equipped gear on the player, only modules attached to the ship.
        /// </summary>
        /// <param name="attacker">The attacker to check</param>
        /// <param name="attackBonus">The attack bonus to apply</param>
        /// <returns>The attack of the ship</returns>
        int GetShipAttack(uint attacker, int attackBonus);

        /// <summary>
        /// Calculates the defense of a ship.
        /// Does not take into account any equipped gear on the player, only modules attached to the ship.
        /// </summary>
        /// <param name="defender">The defender to check</param>
        /// <param name="defenseBonus">The defense bonus to apply</param>
        /// <returns>The defense of the ship</returns>
        int GetShipDefense(uint defender, int defenseBonus);

        /// <summary>
        /// Gets the ability score stat used by the attacking ship.
        /// If attacker has the Intuitive Piloting feat and WIL > PER, then WIL is returned.
        /// Otherwise returns PER
        /// </summary>
        /// <param name="attacker">The attacker to check</param>
        /// <returns>The raw stat value of the attacker. This will be either WIL or PER depending on Intuitive Piloting.</returns>
        int GetAttackStat(uint attacker);

        /// <summary>
        /// Applies damage to a ship target. Damage will first be taken to the shields.
        /// When shields reaches zero, damage will be taken on the hull.
        /// When hull reaches zero, the ship will explode.
        /// </summary>
        /// <param name="attacker">The attacking ship</param>
        /// <param name="target">The defending, targeted ship</param>
        /// <param name="amount">The amount of damage to apply to the target.</param>
        void ApplyShipDamage(uint attacker, uint target, int amount);

        /// <summary>
        /// Applies damage to a ship target's armor only. This bypasses shields.
        /// When hull reaches zero, the ship will explode.
        /// </summary>
        /// <param name="attacker">The attacking ship</param>
        /// <param name="target">The defending, targeted ship</param>
        /// <param name="amount">The amount of damage to apply to the target.</param>
        void ApplyHullDamage(uint attacker, uint target, int amount);

        /// <summary>
        /// Applies death to a creature.
        /// If this is a PC:
        ///     - The ship modules will either drop or be destroyed.
        ///     - The ship will require repairs
        ///     - The pilot will be killed (inflicting default death system penalties)
        ///     - Everyone inside the ship instance will be killed (inflicting default death system penalties)
        ///     - The ship will relocate back to the last dock it was at
        /// If this is an NPC, they will be killed and explode in spectacular fashion.
        /// </summary>
        void ApplyDeath();

        /// <summary>
        /// When a creature clicks on a space object, target that object.
        /// </summary>
        void TargetSpaceObject();

        /// <summary>
        /// Performs an emergency exit on a ship.
        /// This will send the ship back to the last place it docked if there are no players in the property 
        /// and no one is currently piloting the ship.
        /// </summary>
        /// <param name="instance">The area instance</param>
        void PerformEmergencyExit(uint instance);

        /// <summary>
        /// Retrieves the module bonus item property off a given item.
        /// If the item property doesn't exist, 0 will be returned.
        /// </summary>
        /// <param name="item">The item to calculate.</param>
        /// <returns>The module bonus of an item or 0 if none are found.</returns>
        int GetModuleBonus(uint item);

        /// <summary>
        /// Reads all starship bonus properties off an item and returns their cumulative values.
        /// </summary>
        /// <param name="item">The item to read.</param>
        /// <returns>An object containing cumulative starship bonus information</returns>
        ShipBonuses GetShipBonuses(uint item);

        /// <summary>
        /// When a player attempts to stealth while in space mode,
        /// exit the stealth mode and send an error message.
        /// </summary>
        void PreventSpaceStealth();
    }

}
