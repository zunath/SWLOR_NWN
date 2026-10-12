using System;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Feature.MigrationDefinition;

/// <summary>
/// Keeps placed Chancellor chairs where players put them after the chair's mesh moved back from its origin,
/// which is where the engine seats a sitter.
/// </summary>
public static class ChancellorChairMigration
{
    public const float ModelShift = 0.15f;

    public static bool Migrate(WorldProperty structure)
    {
        if (structure.StructureType != StructureType.ChairChancellor ||
            !structure.Positions.TryGetValue(PropertyLocationType.StaticPosition, out var position))
            return false;

        // The seat front points opposite the chair's facing.
        var facing = position.Orientation * MathF.PI / 180f;
        position.X -= ModelShift * MathF.Cos(facing);
        position.Y -= ModelShift * MathF.Sin(facing);
        return true;
    }
}
