using System.Collections.Concurrent;
using SWLOR.Toolset.Domain.GameData.Resources;

namespace SWLOR.Toolset.Editors.Items
{
    /// <summary>
    /// Which numbered variants of each armor body part actually have a model, so the Appearance tab
    /// can offer the real ones instead of a 0-255 spinner over a range that is mostly empty.
    /// </summary>
    /// <remarks>
    /// The part models are named <c>{prefix}_{part}{number:D3}</c> and the numbering is sparse -
    /// a torso has 64 variants and a hand 12, neither of them contiguous. Typing a number that has
    /// no model silently drops that piece off the body, which is indistinguishable from the armor
    /// simply not covering it.
    /// <para>
    /// Male and female variants are unioned: the two bodies carry the same armor at the same number,
    /// but one gender occasionally ships a variant the other lacks, and hiding it would make the
    /// list depend on which mannequin happened to be selected.
    /// </para>
    /// </remarks>
    public sealed class ArmorPartCatalog
    {
        /// <summary>Aurora never numbers a body part above this.</summary>
        private const int MaximumPartNumber = 255;

        private static readonly string[] Prefixes = { "pmh0", "pfh0" };

        private readonly ResourceIndex? _resources;
        private readonly ConcurrentDictionary<string, IReadOnlyList<int>> _cache = new(StringComparer.OrdinalIgnoreCase);

        public ArmorPartCatalog(ResourceIndex? resources)
        {
            _resources = resources;
        }

        /// <summary>
        /// The numbers that resolve to a real model for a part category ("chest", "shol", ...),
        /// ascending. Empty when no game data is loaded, which leaves the caller on a plain number
        /// box rather than an empty list.
        /// </summary>
        public IReadOnlyList<int> Numbers(string partType)
        {
            if (_resources == null || string.IsNullOrWhiteSpace(partType))
                return Array.Empty<int>();

            return _cache.GetOrAdd(partType.Trim(), Probe);
        }

        private IReadOnlyList<int> Probe(string partType)
        {
            var found = new List<int>();
            for (var number = 0; number <= MaximumPartNumber; number++)
            {
                foreach (var prefix in Prefixes)
                {
                    var resRef = $"{prefix}_{partType}{number:D3}";
                    if (!_resources!.TryLookup(ResourceIdentity.FromFileName(resRef + ".mdl"), out _))
                        continue;

                    found.Add(number);
                    break;
                }
            }

            return found;
        }
    }
}
