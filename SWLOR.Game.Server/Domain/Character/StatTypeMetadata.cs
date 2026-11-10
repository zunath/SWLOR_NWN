using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Character;

namespace SWLOR.Game.Server.Domain.Character
{
    /// <summary>
    /// What a <see cref="StatType"/>'s <see cref="StatTypeAttribute"/> declares, as a value that can sit in an immutable table.
    /// </summary>
    public readonly record struct StatTypeMetadata(
        StatTypeCategory Category,
        StatTypeAggregation Aggregation,
        DeflectionSource DeflectionSource);
}
