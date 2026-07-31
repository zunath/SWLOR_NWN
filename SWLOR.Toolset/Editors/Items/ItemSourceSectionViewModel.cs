using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Editors.Items
{
    /// <summary>
    /// The item editor's Source tab: "where can a player obtain this item", backed by
    /// <see cref="ItemObtainabilityIndex.SourcesFor"/> through a caller-supplied lookup delegate so
    /// this view model never has to know how, or when, the workspace's index gets built.
    /// </summary>
    public sealed class ItemSourceSectionViewModel
    {
        private readonly Func<string, IReadOnlyList<ItemSourceEntry>>? _lookup;

        private string _resRef;

        /// <summary>True once a real lookup delegate is available (the workspace's index has been built).</summary>
        public bool IsLoaded => _lookup != null;

        /// <summary>True when at least one source was found for the current resref.</summary>
        public bool IsObtainable { get; private set; }

        /// <summary>
        /// The tab's headline: "&#x2713; Obtainable — N sources in the module" when at least one
        /// source exists, "No player source grants this item" when none do, or a distinct loading
        /// message while <see cref="IsLoaded"/> is false.
        /// </summary>
        public string Verdict { get; private set; } = string.Empty;

        /// <summary>
        /// One group per <see cref="ItemSourceKind"/> that has at least one entry, in the fixed
        /// display order Store, Recipe, Loot, Quest, Container, Other.
        /// </summary>
        public IReadOnlyList<ItemSourceGroupViewModel> Groups { get; private set; } =
            Array.Empty<ItemSourceGroupViewModel>();

        /// <summary>
        /// Kind names with zero entries for this item, in the same fixed order - the dim
        /// "not in any loot table" rows the tab still shows so a missing kind reads as "checked and
        /// absent" rather than "not looked at".
        /// </summary>
        public IReadOnlyList<string> EmptyKinds { get; private set; } = Array.Empty<string>();

        /// <summary>
        /// Null when the caller has no notion of readiness (tests, and any lookup that answers
        /// immediately); otherwise false while the workspace's index is still being built in the
        /// background, so "not found yet" is never shown as "no player source grants this item".
        /// </summary>
        private readonly Func<bool>? _isReady;

        public ItemSourceSectionViewModel(
            string resRef,
            Func<string, IReadOnlyList<ItemSourceEntry>>? lookup,
            Func<bool>? isReady = null)
        {
            _resRef = resRef ?? throw new ArgumentNullException(nameof(resRef));
            _lookup = lookup;
            _isReady = isReady;

            Refresh(resRef);
        }

        /// <summary>False while the index is still building - the tab shows progress, not a verdict.</summary>
        public bool IsReady => _isReady?.Invoke() ?? true;

        /// <summary>
        /// Answers obtainability for a prospective resref without changing the Source tab. Save uses
        /// this when the editable TemplateResRef may already differ from the file/header resref.
        /// </summary>
        public bool HasPlayerSource(string resRef) =>
            IsLoaded &&
            IsReady &&
            (_lookup!(resRef) ?? Array.Empty<ItemSourceEntry>()).Count > 0;

        /// <summary>Re-runs the lookup for (possibly) a new resref and rebuilds every derived property.</summary>
        public void Refresh(string resRef)
        {
            _resRef = resRef ?? throw new ArgumentNullException(nameof(resRef));

            if (_lookup == null)
            {
                IsObtainable = false;
                Verdict = "Obtainability index not loaded";
                Groups = Array.Empty<ItemSourceGroupViewModel>();
                EmptyKinds = Enum.GetValues<ItemSourceKind>().Select(kind => kind.ToString()).ToList();
                return;
            }

            var entries = _lookup(_resRef) ?? Array.Empty<ItemSourceEntry>();

            if (!IsReady)
            {
                IsObtainable = false;
                Verdict = "Looking for the places this item can be obtained...";
                Groups = Array.Empty<ItemSourceGroupViewModel>();
                EmptyKinds = Array.Empty<string>();
                return;
            }

            var groups = new List<ItemSourceGroupViewModel>();
            var emptyKinds = new List<string>();

            foreach (var kind in Enum.GetValues<ItemSourceKind>())
            {
                var kindEntries = entries.Where(entry => entry.Kind == kind).ToList();
                if (kindEntries.Count > 0)
                    groups.Add(new ItemSourceGroupViewModel(kind.ToString(), kindEntries));
                else
                    emptyKinds.Add(kind.ToString());
            }

            Groups = groups;
            EmptyKinds = emptyKinds;
            IsObtainable = entries.Count > 0;
            Verdict = IsObtainable
                ? $"✓ Obtainable — {entries.Count} sources in the module"
                : "No player source grants this item";
        }
    }
}
