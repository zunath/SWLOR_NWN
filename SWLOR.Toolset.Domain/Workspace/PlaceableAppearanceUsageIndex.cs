using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Gff;

namespace SWLOR.Toolset.Domain.Workspace
{
    /// <summary>
    /// How many blueprints use each placeables.2da row, so the model grid can lead with the models
    /// this module actually builds with and say what a row is already used by.
    /// </summary>
    /// <remarks>
    /// The full table offers 24,304 models. The module uses about 8,000 of them across its 8,355
    /// blueprints - very nearly one each - so "used here" is both the smaller list and the one a
    /// builder wants first when matching an existing area's look.
    /// </remarks>
    public sealed class PlaceableAppearanceUsageIndex
    {
        private readonly IReadOnlyDictionary<int, int> _counts;

        private PlaceableAppearanceUsageIndex(IReadOnlyDictionary<int, int> counts)
        {
            _counts = counts;
        }

        /// <summary>An index with nothing counted, used before the scan finishes.</summary>
        public static PlaceableAppearanceUsageIndex Empty { get; } =
            new(new Dictionary<int, int>());

        /// <summary>True once a real scan has run; an empty index must not be read as "nothing is used".</summary>
        public bool IsBuilt => _counts.Count > 0;

        /// <summary>Blueprints using this appearance row.</summary>
        public int CountFor(int appearanceId) => _counts.TryGetValue(appearanceId, out var count) ? count : 0;

        /// <summary>One pass over the placeable blueprints, reading only their Appearance field.</summary>
        public static PlaceableAppearanceUsageIndex Build(ModuleWorkspace workspace)
        {
            ArgumentNullException.ThrowIfNull(workspace);

            var counts = new Dictionary<int, int>();

            foreach (var resRef in workspace.EnumerateResRefs(ResourceType.Utp))
            {
                try
                {
                    var path = workspace.GetResourcePath(ResourceType.Utp, resRef);
                    if (!File.Exists(path))
                        continue;

                    var appearance = JsonGffDocument.Load(path).Root.GetUIntOrNull("Appearance");
                    if (appearance == null)
                        continue;

                    var id = (int)appearance.Value;
                    counts[id] = counts.TryGetValue(id, out var existing) ? existing + 1 : 1;
                }
                catch (Exception)
                {
                    // A blueprint that will not parse costs one count, not the whole index.
                }
            }

            return new PlaceableAppearanceUsageIndex(counts);
        }
    }
}
