using System;
using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.DBService
{
    /// <summary>
    /// Describes an entity's search index exactly as it is created, so a stored fingerprint
    /// can tell on boot whether the index must be rebuilt.
    /// </summary>
    public static class SearchIndexDefinition
    {
        /// <summary>
        /// The key prefix of an entity's search hashes. The trailing colon keeps "Index:Player:"
        /// from also matching "Index:PlayerNote:".
        /// </summary>
        public static string KeyPrefix(string entityKeyPrefix)
        {
            return $"Index:{entityKeyPrefix}:";
        }

        /// <summary>
        /// The Redis key holding the fingerprint of the index that was last built.
        /// </summary>
        public static string FingerprintKey(string indexName)
        {
            return $"IndexDefinition:{indexName}";
        }

        public static bool IsNumeric(Type propertyType)
        {
            return propertyType == typeof(int) ||
                   propertyType == typeof(int?) ||
                   propertyType == typeof(ulong) ||
                   propertyType == typeof(ulong?) ||
                   propertyType == typeof(long) ||
                   propertyType == typeof(long?);
        }

        /// <summary>
        /// Builds the FT.CREATE arguments. Each index covers only its own entity's hashes and keeps
        /// stop words so partial-match searches can require every word.
        /// </summary>
        public static List<object> BuildCreateArgs(
            string indexName,
            string entityKeyPrefix,
            IEnumerable<(string Name, bool IsNumeric)> fields)
        {
            var args = new List<object>
            {
                indexName, "ON", "HASH", "PREFIX", "1", KeyPrefix(entityKeyPrefix), "STOPWORDS", "0", "SCHEMA"
            };

            // Sorted so the fingerprint does not depend on reflection order.
            foreach (var (name, isNumeric) in fields.OrderBy(field => field.Name, StringComparer.Ordinal))
            {
                args.Add(name);
                args.Add(isNumeric ? "NUMERIC" : "TEXT");
            }

            return args;
        }

        public static string Fingerprint(IEnumerable<object> createArgs)
        {
            return string.Join(" ", createArgs);
        }
    }
}
