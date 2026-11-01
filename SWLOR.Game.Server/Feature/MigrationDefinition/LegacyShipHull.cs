namespace SWLOR.Game.Server.Feature.MigrationDefinition;

/// <summary>
/// A retired ship deed and the ship it becomes. The deed's tag is what a registered ship's status stores; the
/// appearance is the model the old hull showed, kept as a Hull Design of the new ship when it is one of its designs.
/// </summary>
public sealed record LegacyShipHull(string Resref, string Tag, string ShipTag, int Appearance);
