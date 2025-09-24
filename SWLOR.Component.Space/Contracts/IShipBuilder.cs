using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Domain.Properties.Enums;
using SWLOR.Shared.Domain.Space.ValueObjects;

namespace SWLOR.Component.Space.Service;

public interface IShipBuilder
{
    /// <summary>
    /// Creates a new ship.
    /// </summary>
    /// <param name="itemTag">The tag of the item to associate with the ship detail.</param>
    /// <returns>A ship builder with the configured options.</returns>
    ShipBuilder Create(string itemTag);

    /// <summary>
    /// Sets the name of the ship currently being built.
    /// </summary>
    /// <param name="name">The name to set.</param>
    /// <returns>A ship builder with the configured options.</returns>
    ShipBuilder Name(string name);

    /// <summary>
    /// Sets the appearance of the ship.
    /// The player's appearance will be changed to this when they enter space mode.
    /// </summary>
    /// <param name="appearance">The appearance to set.</param>
    /// <returns>A ship builder with the configured options.</returns>
    ShipBuilder Appearance(AppearanceType appearance);

    /// <summary>
    /// Sets the resref of the deed item which is used for registering this ship type.
    /// </summary>
    /// <param name="itemResref">The resref of the deed item.</param>
    /// <returns>A ship builder with the configured options.</returns>
    ShipBuilder ItemResref(string itemResref);

    /// <summary>
    /// Sets the maximum shields of the ship.
    /// </summary>
    /// <param name="maxShield">The value to set.</param>
    /// <returns>A ship builder with the configured options.</returns>
    ShipBuilder MaxShield(int maxShield);

    /// <summary>
    /// Sets the maximum armor of the ship.
    /// </summary>
    /// <param name="maxArmor">The value to set.</param>
    /// <returns>A ship builder with the configured options.</returns>
    ShipBuilder MaxArmor(int maxArmor);

    /// <summary>
    /// Sets the maximum capacitor of the ship.
    /// </summary>
    /// <param name="maxCapacitor">The value to set.</param>
    /// <returns>A ship builder with the configured options.</returns>
    ShipBuilder MaxCapacitor(int maxCapacitor);

    /// <summary>
    /// Sets the shield recharge rate of the ship (in whole seconds).
    /// </summary>
    /// <param name="shieldRechargeRate">The value to set.</param>
    /// <returns>A ship builder with the configured options.</returns>
    ShipBuilder ShieldRechargeRate(int shieldRechargeRate);

    /// <summary>
    /// Sets the number of high power nodes on this ship.
    /// High power nodes are typically used for weaponry and mining lasers.
    /// </summary>
    /// <param name="highPowerNodes">The number of high power nodes to set.</param>
    /// <returns>A ship builder with the configured options.</returns>
    ShipBuilder HighPowerNodes(int highPowerNodes);

    /// <summary>
    /// Sets the number of low power nodes on this ship.
    /// Low power nodes are typically used for shield boosters, armor reinforcement, etc.
    /// </summary>
    /// <param name="lowPowerNodes">The number of configuration nodes to set.</param>
    /// <returns>A ship builder with the configured options.</returns>
    ShipBuilder LowPowerNodes(int lowPowerNodes);

    /// <summary>
    /// Sets the number of ship configuration nodes on this ship.
    /// By default ships should have 1 configuration node.
    /// </summary>
    /// <param name="shipConfigurationNodes">The number of low power nodes to set.</param>
    /// <returns>A ship builder with the configured options.</returns>
    ShipBuilder ShipConfigurationNodes(int shipConfigurationNodes);

    /// <summary>
    /// Determines if the ship is a capital ship.
    /// </summary>
    /// <param name="CapitalShip">If the ship is a capital ship.</param>
    /// <returns>A ship builder with the configured options.</returns>
    ShipBuilder CapitalShip();

    /// <summary>
    /// Indicates a player must have the perk at a specific level in order to use the ship.
    /// </summary>
    /// <param name="perkType">The type of perk to require</param>
    /// <param name="requiredLevel">The minimum level required</param>
    /// <returns>A ship builder with the configured options.</returns>
    ShipBuilder RequirePerk(PerkType perkType, int requiredLevel);

    /// <summary>
    /// Sets the interior layout type used by the associated property on this ship.
    /// </summary>
    /// <param name="layout">The layout to assign.</param>
    /// <returns>A ship builder with the configured options.</returns>
    ShipBuilder InteriorLayout(PropertyLayoutType layout);

    /// <summary>
    /// Returns a built dictionary of ships.
    /// </summary>
    /// <returns>A dictionary of ship details.</returns>
    Dictionary<string, ShipDetail> Build();
}