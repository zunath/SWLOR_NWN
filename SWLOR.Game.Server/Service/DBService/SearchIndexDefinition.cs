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

        /// <summary>
        /// Rebuilds the FT.CREATE arguments of a live index from its FT.INFO parts, in the same form as
        /// BuildCreateArgs, so a live index can be compared with the one this code would create.
        /// </summary>
        /// <param name="stopWords">The index's own stop-word list, or null when it uses RediSearch's default list.</param>
        public static List<object> DescribeLiveIndex(
            string indexName,
            string keyType,
            IReadOnlyList<string> prefixes,
            IReadOnlyList<string> stopWords,
            IEnumerable<(string Name, string Type)> fields)
        {
            var args = new List<object> { indexName, "ON", keyType.ToUpperInvariant(), "PREFIX", prefixes.Count.ToString() };
            args.AddRange(prefixes);

            if (stopWords != null)
            {
                args.Add("STOPWORDS");
                args.Add(stopWords.Count.ToString());
                args.AddRange(stopWords);
            }

            args.Add("SCHEMA");
            foreach (var (name, type) in fields.OrderBy(field => field.Name, StringComparer.Ordinal))
            {
                args.Add(name);
                args.Add(type.ToUpperInvariant());
            }

            return args;
        }

        public static string Fingerprint(IEnumerable<object> createArgs)
        {
            return string.Join(" ", createArgs);
        }
    }
}
