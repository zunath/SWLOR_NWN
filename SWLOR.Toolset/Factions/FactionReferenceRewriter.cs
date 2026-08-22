using System.Security.Cryptography;
using SWLOR.Toolset.Domain.Editing;
using SWLOR.Toolset.Domain.Gff;

namespace SWLOR.Toolset.Factions
{
    public sealed record FactionReferenceUsage(
        int BlueprintCount,
        int PlacedObjectCount,
        bool IsKnown = true)
    {
        public int Total => BlueprintCount + PlacedObjectCount;

        public static FactionReferenceUsage Unknown { get; } = new(0, 0, IsKnown: false);
    }

    public sealed record FactionReferenceRewrite(
        string Path,
        byte[] Bytes,
        int ChangedReferences,
        bool IsAreaInstanceFile,
        byte[] SourceSha256)
    {
        /// <summary>Whether the resource still contains the exact bytes this rewrite was built from.</summary>
        public bool SourceMatchesCurrentFile()
        {
            try
            {
                return File.Exists(Path) && CryptographicOperations.FixedTimeEquals(
                    SourceSha256,
                    SHA256.HashData(File.ReadAllBytes(Path)));
            }
            catch (IOException)
            {
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Finds and remaps numeric faction references outside <c>repute.fac</c>. A faction id is a
    /// list index, so deleting one shifts every larger id as well as moving direct members to the
    /// deleted faction's parent.
    /// </summary>
    public static class FactionReferenceRewriter
    {
        private static readonly string[] FactionResourceDirectories =
        {
            "utc", // creature blueprints: FactionID
            "utp", // placeable blueprints: Faction
            "utd", // door blueprints: Faction
            "utt", // trigger blueprints: Faction
            "ute", // encounter blueprints: Faction
            "git"  // placed creatures, placeables, doors, triggers, and encounters
        };

        public static IReadOnlyDictionary<int, FactionReferenceUsage> ScanUsage(
            string moduleRoot,
            int factionCount)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(moduleRoot);
            if (factionCount < 0)
                throw new ArgumentOutOfRangeException(nameof(factionCount));

            var blueprints = new int[factionCount];
            var placed = new int[factionCount];

            foreach (var path in EnumerateFactionResources(moduleRoot))
            {
                JsonGffDocument document;
                try
                {
                    document = JsonGffDocument.Load(path);
                }
                catch (Exception)
                {
                    // A malformed resource is already surfaced by validation and must not stop the
                    // editor from describing all of the well-formed references it can prove.
                    continue;
                }

                var counts = IsAreaInstanceFile(path) ? placed : blueprints;
                VisitFactionFields(document.Root, field =>
                {
                    var id = (int)field.GetInteger();
                    if (id >= 0 && id < counts.Length)
                        counts[id]++;
                });
            }

            return Enumerable.Range(0, factionCount).ToDictionary(
                id => id,
                id => new FactionReferenceUsage(blueprints[id], placed[id]));
        }

        public static IReadOnlyList<FactionReferenceRewrite> BuildRewrites(
            string moduleRoot,
            IReadOnlyDictionary<int, int> idMap)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(moduleRoot);
            ArgumentNullException.ThrowIfNull(idMap);

            var rewrites = new List<FactionReferenceRewrite>();
            foreach (var path in EnumerateFactionResources(moduleRoot))
            {
                var sourceBytes = File.ReadAllBytes(path);
                var document = JsonGffDocument.Parse(sourceBytes);
                var changes = 0;
                VisitFactionFields(document.Root, field =>
                {
                    var oldId = (int)field.GetInteger();
                    if (idMap.TryGetValue(oldId, out var newId) && newId != oldId)
                        changes++;
                });

                if (changes == 0)
                    continue;

                using var session = new DocumentSession(path, document);
                session.Execute("Remap faction references", () =>
                {
                    VisitFactionFields(document.Root, field =>
                    {
                        var oldId = (int)field.GetInteger();
                        if (idMap.TryGetValue(oldId, out var newId) && newId != oldId)
                            field.SetInteger(newId);
                    });
                });

                rewrites.Add(new FactionReferenceRewrite(
                    path,
                    session.ToBytes(),
                    changes,
                    IsAreaInstanceFile(path),
                    SHA256.HashData(sourceBytes)));
            }

            return rewrites;
        }

        private static IEnumerable<string> EnumerateFactionResources(string moduleRoot)
        {
            foreach (var directoryName in FactionResourceDirectories)
            {
                var directory = Path.Combine(moduleRoot, directoryName);
                if (!Directory.Exists(directory))
                    continue;

                foreach (var path in Directory.EnumerateFiles(directory, "*.json", SearchOption.TopDirectoryOnly)
                             .OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
                {
                    yield return path;
                }
            }
        }

        private static bool IsAreaInstanceFile(string path) =>
            string.Equals(
                Directory.GetParent(path)?.Name,
                "git",
                StringComparison.OrdinalIgnoreCase);

        private static void VisitFactionFields(JsonGffStruct target, Action<JsonGffField> visit)
        {
            foreach (var (name, field) in target.Entries)
            {
                if ((string.Equals(name, "Faction", StringComparison.Ordinal) ||
                     string.Equals(name, "FactionID", StringComparison.Ordinal)) &&
                    GffFieldTypeNames.IsNumeric(field.Type) &&
                    field.Type is not GffFieldType.Float and not GffFieldType.Double)
                {
                    visit(field);
                }

                if (field.Struct != null)
                    VisitFactionFields(field.Struct, visit);
                if (field.Elements == null)
                    continue;
                foreach (var element in field.Elements)
                    VisitFactionFields(element, visit);
            }
        }
    }
}
