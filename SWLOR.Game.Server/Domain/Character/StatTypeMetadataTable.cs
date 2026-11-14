using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Domain.Character;

namespace SWLOR.Game.Server.Domain.Character
{
    /// <summary>
    /// The metadata of every stat type. It builds, and says so on the console, when metadata is first read: this class is
    /// separate from <see cref="StatRules"/> so that a formula does not set it off, and its explicit static constructor
    /// keeps the line at that moment. The offline tools print that line as they always did; at boot the first read is
    /// <c>Stat.CacheData</c>.
    /// </summary>
    internal static class StatTypeMetadataTable
    {
        internal static readonly FrozenDictionary<StatType, StatTypeMetadata> Entries;

        static StatTypeMetadataTable()
        {
            var entries = new Dictionary<StatType, StatTypeMetadata>();

            foreach (var statType in Enum.GetValues(typeof(StatType)).Cast<StatType>())
            {
                var attribute = statType.GetAttribute<StatType, StatTypeAttribute>();
                entries[statType] = new StatTypeMetadata(attribute.Category, attribute.Aggregation, attribute.DeflectionSource);
            }

            Entries = entries.ToFrozenDictionary();

            Console.WriteLine($"Loaded {Entries.Count} stat type metadata entries.");
        }
    }
}
