using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace SWLOR.Game.Server.Service.StationService
{
    /// <summary>
    /// The ore a player collected from an Asteroid Drill, saved in the character file with the ore before the drill saves
    /// its stock. A restart between the two leaves a mark whose units the next login takes from the stock once: the drill
    /// records the last collection applied for each player (<c>StationBuildingState.LastDrillCollectionIds</c>).
    /// </summary>
    public sealed class DrillCollection
    {
        public string Id { get; init; }
        public string BuildingId { get; init; }

        /// <summary>The units collected: item resref to units.</summary>
        public Dictionary<string, int> Units { get; init; } = new();

        public string Serialize() =>
            string.Join("|", Id, BuildingId,
                string.Join(",", Units.Select(x => $"{x.Key}={x.Value.ToString(CultureInfo.InvariantCulture)}")));

        public static bool TryParse(string value, out DrillCollection collection)
        {
            collection = null;
            var parts = (value ?? string.Empty).Split('|');
            if (parts.Length != 3 || string.IsNullOrWhiteSpace(parts[0]) || string.IsNullOrWhiteSpace(parts[1]))
                return false;

            var units = new Dictionary<string, int>();
            foreach (var entry in parts[2].Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                var pair = entry.Split('=');
                if (pair.Length != 2 || string.IsNullOrWhiteSpace(pair[0]) ||
                    !int.TryParse(pair[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var count) || count <= 0)
                {
                    return false;
                }

                units[pair[0]] = count;
            }

            collection = new DrillCollection { Id = parts[0], BuildingId = parts[1], Units = units };
            return true;
        }

        /// <summary>
        /// Takes the collected units from a stock that has not had this collection yet, and records it as the player's
        /// last. True when the stock changed; false when the collection is already applied.
        /// </summary>
        public bool ApplyTo(Dictionary<string, int> storage, Dictionary<string, string> lastCollectionIds, string playerId)
        {
            if (storage == null || lastCollectionIds == null ||
                string.Equals(lastCollectionIds.GetValueOrDefault(playerId), Id, StringComparison.Ordinal))
            {
                return false;
            }

            foreach (var (resref, units) in Units)
            {
                var left = storage.GetValueOrDefault(resref) - units;
                if (left > 0)
                    storage[resref] = left;
                else
                    storage.Remove(resref);
            }

            lastCollectionIds[playerId] = Id;
            return true;
        }
    }
}
