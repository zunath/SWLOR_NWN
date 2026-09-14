using SWLOR.Toolset.Domain.Editors.Behaviors;
using SWLOR.Toolset.Domain.GameData.Lookups;

namespace SWLOR.Toolset.Domain.Editors.Waypoints
{
    /// <summary>
    /// The marker appearances a waypoint can wear, each carrying the model it is drawn from so the
    /// editor can offer the picture rather than the name.
    /// </summary>
    /// <remarks>
    /// A waypoint's appearance is a coloured flag, a letter, or a symbol - treasure, mapnote,
    /// bullseye, snowflake. "cyan" and "treasure" say nothing about which marker a builder wants to
    /// see on the ground, so the picker draws them; <c>waypoint.2da</c>'s RESREF is the model, with
    /// no separate model column to consult.
    /// </remarks>
    public static class WaypointAppearanceCatalog
    {
        public static IReadOnlyList<BehaviorChoice> Read(WaypointAppearanceService? appearances)
        {
            if (appearances == null)
                return Array.Empty<BehaviorChoice>();

            return appearances.GetAll()
                .Select(row => new BehaviorChoice(
                    row.Id,
                    row.DisplayName,
                    modelResRef: row.ModelName))
                .ToList();
        }
    }
}
