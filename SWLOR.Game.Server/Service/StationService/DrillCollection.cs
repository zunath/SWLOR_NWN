using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace SWLOR.Game.Server.Service.StationService
{
    /// <summary>
    /// The ore a player collected from an Asteroid Drill. The drill saves its stock without the ore and the collection as
    /// pending (<c>StationBuildingState.PendingDrillCollections</c>) before the character file is saved with the ore and
    /// this mark, so no one else can collect the same ore. A login after a restart in between keeps a pending collection
    /// whose mark the character file holds and returns the ore of any other to the stock.
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

        /// <summary>Takes the collected units out of the stock. A resref collected out is removed.</summary>
        public void TakeFrom(Dictionary<string, int> storage)
        {
            foreach (var (resref, units) in Units)
            {
                var left = storage.GetValueOrDefault(resref) - units;
                if (left > 0)
                    storage[resref] = left;
                else
                    storage.Remove(resref);
            }
        }

        /// <summary>Puts the collected units back in the stock, for a collection whose ore never reached the character file.</summary>
        public void ReturnTo(Dictionary<string, int> storage)
        {
            foreach (var (resref, units) in Units)
                storage[resref] = storage.GetValueOrDefault(resref) + units;
        }

        /// <summary>Whether the character file's mark (<see cref="Serialize"/>) names the collection <paramref name="collectionId"/>.</summary>
        public static bool IsMarked(string mark, string collectionId) =>
            TryParse(mark, out var marked) && string.Equals(marked.Id, collectionId, StringComparison.Ordinal);
    }
}
