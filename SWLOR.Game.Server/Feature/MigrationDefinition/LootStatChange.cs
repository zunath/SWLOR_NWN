using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.MigrationDefinition;

/// <summary>
/// One revised blueprint property. Subtype only distinguishes Defense (physical or force);
/// Previous is zero when the revision adds the property.
/// </summary>
public readonly record struct LootStatChange(ItemPropertyType Type, int Subtype, int Previous, int Current);
