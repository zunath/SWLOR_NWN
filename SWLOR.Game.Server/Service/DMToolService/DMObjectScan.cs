using System.Collections.Generic;
using System.Linq;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service.DMToolService
{
    /// <summary>
    /// A DM chooser search that reads whole areas a slice at a time, so a search of every area can be spread
    /// across server ticks instead of stalling one.
    /// </summary>
    public class DMObjectScan
    {
        private readonly DMObjectCategory _category;
        private readonly ObjectType _objectType;
        private readonly IReadOnlyList<string> _terms;
        private readonly Queue<uint> _areas;
        private readonly List<DMObjectSearchResult> _results = new();

        /// <summary>
        /// Set once every area has been read or the result limit was reached.
        /// </summary>
        public bool IsComplete { get; private set; }

        /// <summary>
        /// Set when more matches existed than <see cref="DMObjectSearch.MaxResults"/>.
        /// </summary>
        public bool IsTruncated { get; private set; }

        /// <summary>
        /// The matches found so far, ordered by name and then area once the scan is complete.
        /// </summary>
        public IReadOnlyList<DMObjectSearchResult> Results => _results;

        /// <summary>
        /// Creates a scan of the given areas. For <see cref="DMObjectCategory.Areas"/> the areas themselves are matched.
        /// </summary>
        /// <param name="category">The kind of object to find.</param>
        /// <param name="objectType">The object type read from each area. Ignored for areas.</param>
        /// <param name="areas">The areas to read, in order.</param>
        /// <param name="search">The search text. Empty matches everything.</param>
        public DMObjectScan(DMObjectCategory category, ObjectType objectType, IEnumerable<uint> areas, string search)
        {
            _category = category;
            _objectType = objectType;
            _terms = DMSearchText.GetTerms(search);
            _areas = new Queue<uint>(areas);
        }

        /// <summary>
        /// Reads whole areas until at least <paramref name="objectBudget"/> objects have been examined.
        /// An area is always finished in the slice that starts it, because object iteration cannot be resumed later.
        /// </summary>
        /// <param name="objectBudget">How many objects to examine before stopping after the current area.</param>
        /// <returns>true when the scan is complete.</returns>
        public bool Continue(int objectBudget)
        {
            var examined = 0;
            while (!IsComplete && examined < objectBudget)
            {
                if (_areas.Count == 0)
                {
                    Complete();
                    break;
                }

                var area = _areas.Dequeue();
                if (!GetIsObjectValid(area))
                    continue;

                examined += _category == DMObjectCategory.Areas
                    ? ReadArea(area)
                    : ReadObjects(area);
            }

            return IsComplete;
        }

        private int ReadArea(uint area)
        {
            var name = GetName(area);
            var tag = GetTag(area);
            var resref = GetResRef(area);
            if (DMSearchText.Matches(_terms, name, tag, resref))
                Add(new DMObjectSearchResult(area, _category, name, tag, resref, name));

            return 1;
        }

        private int ReadObjects(uint area)
        {
            var examined = 0;
            var areaName = GetName(area);
            for (var obj = GetFirstObjectInArea(area, _objectType);
                 GetIsObjectValid(obj);
                 obj = GetNextObjectInArea(area, _objectType))
            {
                examined++;

                // Player characters and DMs belong to the player search.
                if (_category == DMObjectCategory.Creatures &&
                    (GetIsDM(obj) || (GetIsPC(obj) && !GetIsDMPossessed(obj))))
                    continue;

                var name = GetName(obj);
                var tag = GetTag(obj);
                var resref = GetResRef(obj);
                if (DMSearchText.Matches(_terms, name, tag, resref))
                    Add(new DMObjectSearchResult(obj, _category, name, tag, resref, areaName));

                if (IsComplete)
                    break;
            }

            return examined;
        }

        private void Add(DMObjectSearchResult result)
        {
            if (_results.Count >= DMObjectSearch.MaxResults)
            {
                IsTruncated = true;
                Complete();
                return;
            }

            _results.Add(result);
        }

        private void Complete()
        {
            IsComplete = true;
            var ordered = _results
                .OrderBy(result => result.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(result => result.AreaName, StringComparer.OrdinalIgnoreCase)
                .ToList();
            _results.Clear();
            _results.AddRange(ordered);
        }
    }
}
