using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Domain.Perk.Enums;
using SWLOR.Shared.Domain.Space.Enums;
using SWLOR.Shared.Domain.Space.ValueObjects;

namespace SWLOR.Component.Space.Contracts
{
    public interface IShipModuleBuilder
    {
        /// <summary>
        /// Creates a new ship module.
        /// </summary>
        /// <param name="itemTag">The tag of the item associated with this ship module.</param>
        /// <returns>A ship module builder with the configured options.</returns>
        IShipModuleBuilder Create(string itemTag);

        /// <summary>
        /// Sets the name of the active ship module we're building.
        /// </summary>
        /// <param name="name">The name to set.</param>
        /// <returns>A ship module builder with the configured options.</returns>
        IShipModuleBuilder Name(string name);

        /// <summary>
        /// Sets the short name of the active ship module we're building.
        /// Short names should generally be no longer than 14 characters.
        /// </summary>
        /// <param name="shortName">The short name to set.</param>
        /// <returns>A ship module builder with the configured options.</returns>
        IShipModuleBuilder ShortName(string shortName);

        /// <summary>
        /// Sets the texture used by the ship module item.
        /// This should match whatever is set in the toolset for this item.
        /// </summary>
        /// <param name="texture">The name of the texture</param>
        /// <returns>A ship module builder with the configured options.</returns>
        IShipModuleBuilder Texture(string texture);

        /// <summary>
        /// Sets the ship module type which is used in NPC AI determinations.
        /// </summary>
        /// <param name="type">The type of module to associate with the AI.</param>
        /// <returns>A ship module builder with the configured options.</returns>
        IShipModuleBuilder Type(ShipModuleType type);

        /// <summary>
        /// Sets the description of the active ship module we're building.
        /// This description is shown when the ship module is examined.
        /// </summary>
        /// <param name="description">The description to set.</param>
        /// <returns>A ship module builder with the configured options.</returns>
        IShipModuleBuilder Description(string description);

        /// <summary>
        /// Sets the power type of the active ship module we're building.
        /// </summary>
        /// <param name="powerType">The power type to set.</param>
        /// <returns>A ship module builder with the configured options.</returns>
        IShipModuleBuilder PowerType(ShipModulePowerType powerType);

        /// <summary>
        /// Sets the action to run when this module's recast timer is calculated.
        /// </summary>
        /// <param name="action">The action to take.</param>
        /// <returns>A ship module builder with the configured options.</returns>
        IShipModuleBuilder Recast(ShipModuleCalculateRecastDelegate action);

        /// <summary>
        /// Sets the number of seconds it takes to recast the module.
        /// </summary>
        /// <param name="seconds">The number of seconds to apply to the recast when used.</param>
        /// <returns>A ship module builder with the configured options.</returns>
        IShipModuleBuilder Recast(float seconds);

        /// <summary>
        /// Sets the action to run when this module's capacitor usage is calculated.
        /// </summary>
        /// <param name="action">The action to take.</param>
        /// <returns>A ship module builder with the configured options.</returns>
        IShipModuleBuilder Capacitor(ShipModuleCalculateCapacitorDelegate action);

        /// <summary>
        /// Sets the amount of capacitor required to use the module.
        /// </summary>
        /// <param name="capacitor">The amount of capacitor to drain from the ship when this module is used.</param>
        /// <returns>A ship module builder with the configured options.</returns>
        IShipModuleBuilder Capacitor(int capacitor);

        /// <summary>
        /// Sets the action to run when this module is equipped to a ship.
        /// </summary>
        /// <param name="action">The action to take.</param>
        /// <returns>A ship module builder with the configured options.</returns>
        IShipModuleBuilder EquippedAction(ShipModuleEquippedDelegate action);

        /// <summary>
        /// Sets the action to run when this module is unequipped from a ship.
        /// </summary>
        /// <param name="action">The action to take.</param>
        /// <returns>A ship module builder with the configured options.</returns>
        IShipModuleBuilder UnequippedAction(ShipModuleUnequippedDelegate action);

        /// <summary>
        /// Sets the action to run when this module is used in space by the activator.
        /// </summary>
        /// <param name="action">The action to take.</param>
        /// <returns>A ship module builder with the configured options.</returns>
        IShipModuleBuilder ActivatedAction(ShipModuleActivatedDelegate action);

        /// <summary>
        /// Sets the action to run when custom validation is requested.
        /// </summary>
        /// <param name="action">The action to take.</param>
        /// <returns>A ship module builder with the configured options.</returns>
        IShipModuleBuilder ValidationAction(ShipModuleValidationDelegate action);

        /// <summary>
        /// Indicates this ship module can target itself.
        /// Only applicable when dealing with an active ship module.
        /// </summary>
        /// <returns>A ship module builder with the configured options.</returns>
        IShipModuleBuilder CanTargetSelf();

        /// <summary>
        /// Adds a valid object type which this ship module can be used upon.
        /// </summary>
        /// <param name="type">The type of object to allow.</param>
        /// <returns>A ship module builder with the configured options.</returns>
        IShipModuleBuilder ValidTargetType(ObjectType type);

        /// <summary>
        /// Indicates if the ship module is only equippable by capital ships.
        /// Capital ships cannot equip non-capital modules. Non-capital ships cannot equip capital modules.
        /// </summary>
        /// <returns>A ship module builder with the configured options.</returns>
        IShipModuleBuilder CapitalClassModule();

        /// <summary>
        /// Indicates a player must have the perk at a specific level in order to equip and use it.
        /// </summary>
        /// <param name="perkType">The type of perk to require.</param>
        /// <param name="requiredLevel">The required level of the perk.</param>
        /// <returns>A ship module builder with the configured options.</returns>
        IShipModuleBuilder RequirePerk(PerkType perkType, int requiredLevel);

        /// <summary>
        /// Runs an action to determine the maximum distance the ship module can be used.
        /// </summary>
        /// <param name="action">The action to run when max distance is calculated.</param>
        /// <returns>A ship module builder with the configured options.</returns>
        IShipModuleBuilder MaxDistance(ShipModuleCalculateMaxDistanceDelegate action);

        /// <summary>
        /// Sets a static maximum distance the module can be used.
        /// </summary>
        /// <param name="distance">The maximum distance in meters the module can be used.</param>
        /// <returns>A ship module builder with the configured options.</returns>
        IShipModuleBuilder MaxDistance(float distance);

        /// <summary>
        /// Builds a dictionary of ship module details.
        /// </summary>
        /// <returns>A dictionary of ship module details.</returns>
        Dictionary<string, ShipModuleDetail> Build();
    }

}
