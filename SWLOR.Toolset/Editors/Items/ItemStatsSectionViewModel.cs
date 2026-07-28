using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using SWLOR.Toolset.Domain.Editors.Behaviors;
using SWLOR.Toolset.Domain.Editors.Items;

namespace SWLOR.Toolset.Editors.Items
{
    /// <summary>
    /// The item editor's Stats tab: a family- and role-driven set of primary
    /// <see cref="ItemStatGroupViewModel"/> cards, plus every other group tucked behind a "show more"
    /// summary until the builder actually asks for it; and the family-independent engine-legacy
    /// sweep (<see cref="Engine"/>).
    /// </summary>
    public sealed partial class ItemStatsSectionViewModel : ObservableObject
    {
        /// <summary>Key prefix a caller's resolveChoices resolves - the suffix is a SubtypeTableResRef.</summary>
        private const string SubtypeKeyPrefix = "item.subtypes:";

        private readonly ItemValueStore _store;
        private readonly Func<string, Action, bool> _runEdit;
        private readonly Action? _valueChanged;
        private readonly Func<string, IReadOnlyList<BehaviorChoice>>? _resolveChoices;

        private ItemFamily _family;
        private IReadOnlyList<ItemStatGroup> _secondaryGroupIds = Array.Empty<ItemStatGroup>();

        public ObservableCollection<ItemStatGroupViewModel> Groups { get; } = new();

        public ObservableCollection<ItemStatGroupViewModel> SecondaryGroups { get; } = new();

        public bool HasSecondary => _secondaryGroupIds.Count > 0;

        /// <summary>"Crafting · Bonuses · Droid · ..." - the titles of the groups tucked away.</summary>
        public string SecondarySummary =>
            string.Join(" · ", _secondaryGroupIds.Select(ItemStatGroupViewModel.TitleFor));

        [ObservableProperty]
        private bool _isSecondaryExpanded;

        /// <summary>The base-game engine properties the corpus still carries - built by <see cref="Rebuild"/>.</summary>
        public ItemEngineLegacySectionViewModel? Engine { get; private set; }

        public ItemStatsSectionViewModel(
            ItemValueStore store,
            Func<string, Action, bool> runEdit,
            Action? valueChanged = null,
            Func<string, IReadOnlyList<BehaviorChoice>>? resolveChoices = null)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _runEdit = runEdit ?? throw new ArgumentNullException(nameof(runEdit));
            _valueChanged = valueChanged;
            _resolveChoices = resolveChoices;
        }

        /// <summary>
        /// Rebuilds every group for the given family/role combination. Called on load and whenever
        /// the base item or role selection changes.
        /// </summary>
        public void Rebuild(ItemFamily family, string roleId)
        {
            _family = family;

            var primaryIds = ItemStatVisibility.PrimaryGroups(family)
                .Concat(ItemRoleCatalog.GroupsUnlockedBy(roleId))
                .Distinct()
                .ToList();

            // The *Enhancement multi-entry properties (ItemStatGroup.Enhancements) mark an Essence AS
            // an enhancement module - relevant to every Essence regardless of which role it takes -
            // so Essence always sees the group as primary. ItemStatVisibility.PrimaryGroups itself
            // stays untouched; this is purely local to how the Stats tab decides what to show.
            if (family == ItemFamily.Essence && !primaryIds.Contains(ItemStatGroup.Enhancements))
                primaryIds.Add(ItemStatGroup.Enhancements);

            Groups.Clear();
            foreach (var groupId in primaryIds)
                Groups.Add(BuildGroup(groupId));

            _secondaryGroupIds = Enum.GetValues<ItemStatGroup>()
                .Where(group => !primaryIds.Contains(group))
                .ToList();

            // The secondary set changed shape, so any previously expanded view no longer applies -
            // it is rebuilt from scratch the next time the builder opens it.
            IsSecondaryExpanded = false;
            SecondaryGroups.Clear();

            OnPropertyChanged(nameof(HasSecondary));
            OnPropertyChanged(nameof(SecondarySummary));

            Engine = new ItemEngineLegacySectionViewModel(_store, _runEdit, _resolveChoices);
        }

        /// <summary>Re-reads every built cell/entry list (primary, and secondary once expanded), and the engine rows.</summary>
        public void ReloadFromDocument()
        {
            foreach (var group in Groups)
                ReloadGroup(group);

            foreach (var group in SecondaryGroups)
                ReloadGroup(group);

            Engine?.Rebuild();
        }

        private static void ReloadGroup(ItemStatGroupViewModel group)
        {
            foreach (var cell in group.Cells)
                cell.Reload();

            foreach (var entryList in group.EntryLists)
                entryList.Reload();
        }

        partial void OnIsSecondaryExpandedChanged(bool value)
        {
            if (!value || SecondaryGroups.Count > 0)
                return;

            foreach (var groupId in _secondaryGroupIds)
                SecondaryGroups.Add(BuildGroup(groupId));
        }

        private ItemStatGroupViewModel BuildGroup(ItemStatGroup group)
        {
            var definitions = group == ItemStatGroup.Combat
                ? ItemStatVisibility.CombatStatsFor(_family)
                : ItemStatCatalog.ByGroup(group);

            var entryLists = ItemMultiEntryCatalog.All
                .Where(definition => definition.Context == group)
                .Select(BuildEntryList)
                .ToList();

            return new ItemStatGroupViewModel(group, definitions, _store, _runEdit, _valueChanged, entryLists);
        }

        private ItemPropertyEntryListViewModel BuildEntryList(ItemMultiEntryDefinition definition) =>
            new(definition, _store, _runEdit, ResolveSubtypeChoices(definition.SubtypeTableResRef), _valueChanged);

        private IReadOnlyList<BehaviorChoice> ResolveSubtypeChoices(string tableResRef) =>
            _resolveChoices?.Invoke($"{SubtypeKeyPrefix}{tableResRef}") ?? Array.Empty<BehaviorChoice>();
    }
}
