using System.Collections.Generic;
using SWLOR.Game.Server.Entity;

namespace SWLOR.Game.Server.Feature.MigrationDefinition;

/// <summary>What converting one registered ship changed, and what its owner is owed.</summary>
public sealed class PlayerShipConversionResult
{
    /// <summary>The ship record changed and must be saved.</summary>
    public bool Changed { get; set; }

    /// <summary>The ship's retired hull became a new ship.</summary>
    public bool HullConverted { get; set; }

    /// <summary>The hull's tag after conversion, or null when the ship's hull is unknown and was left alone.</summary>
    public string ShipTag { get; set; }

    /// <summary>Retired items converted or refunded: the deed, modules and configuration modules.</summary>
    public LegacySpaceItemTally Items { get; } = new();

    /// <summary>Modules that no longer fit the new ship, saved as overflow items for the owner.</summary>
    public List<OverflowItem> ReturnedModules { get; } = new();

    public int ModulesKept { get; set; }
}
