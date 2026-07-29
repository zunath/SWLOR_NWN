using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using SWLOR.Toolset.Domain.Editors.Behaviors;
using SWLOR.Toolset.Domain.Editors.Items;

namespace SWLOR.Toolset.Editors.Items
{
    /// <summary>
    /// The item editor's Stats tab: a family- and role-driven set of primary
    /// <see cref="ItemStatGroupViewModel"/> cards, and the family-independent engine-legacy sweep
    /// (<see cref="Engine"/>). Stats outside a family's primary groups are simply not shown - there
    /// is no secondary/"not used by this base type" section to expand.
    /// </summary>
    public sealed partial class ItemStatsSectionViewModel : ObservableObject
    {
        /// <summary>Key prefix a caller's resolveChoices resolves - the suffix is a SubtypeTableResRef.</summary>
        private const string SubtypeKeyPrefix = "item.subtypes:";

        private readonly ItemValueStore _store;
        private readonly Func<string, Action, bool> _runEdit;
        private readonly Action? _valueChanged;
        private readonly Func<string, IReadOnlyList<BehaviorChoice>>? _resolveChoices;
        private readonly Func<int, int?>? _costTableMax;

        private ItemFamily _family;

        public ObservableCollection<ItemStatGroupViewModel> Groups { get; } = new();

        /// <summary>
        /// <see cref="Groups"/> dealt into two columns of roughly equal height. A uniform-row
        /// layout sized every row to its tallest card, so a two-row Defense card beside an
        /// eight-row Resistance card left a hole the size of six rows underneath it; packing each
        /// column independently closes those.
        /// </summary>
        public ObservableCollection<ItemStatGroupViewModel> LeftColumn { get; } = new();

        public ObservableCollection<ItemStatGroupViewModel> RightColumn { get; } = new();

        /// <summary>The base-game engine properties the corpus still carries - built by <see cref="Rebuild"/>.</summary>
        public ItemEngineLegacySectionViewModel? Engine { get; private set; }

        public ItemStatsSectionViewModel(
            ItemValueStore store,
            Func<string, Action, bool> runEdit,
            Action? valueChanged = null,
            Func<string, IReadOnlyList<BehaviorChoice>>? resolveChoices = null,
            Func<int, int?>? costTableMax = null)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _runEdit = runEdit ?? throw new ArgumentNullException(nameof(runEdit));
            _valueChanged = valueChanged;
            _resolveChoices = resolveChoices;
            _costTableMax = costTableMax;
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

            LayOutColumns();

            Engine = new ItemEngineLegacySectionViewModel(_store, _runEdit, _resolveChoices, _costTableMax);
        }

        /// <summary>
        /// Deals the built groups into the two columns, next card always going to whichever column
        /// is currently shorter. Height is estimated by row count (cells plus entry lists plus
        /// exclusive choices), which is what actually drives a card's height.
        /// </summary>
        private void LayOutColumns()
        {
            LeftColumn.Clear();
            RightColumn.Clear();

            var leftRows = 0;
            var rightRows = 0;
            foreach (var group in Groups)
            {
                var rows = group.Cells.Count + group.EntryLists.Count + group.ExclusiveChoices.Count;
                if (leftRows <= rightRows)
                {
                    LeftColumn.Add(group);
                    leftRows += rows;
                }
                else
                {
                    RightColumn.Add(group);
                    rightRows += rows;
                }
            }
        }

        /// <summary>Re-reads every built cell/entry list/exclusive choice, and the engine rows.</summary>
        public void ReloadFromDocument()
        {
            foreach (var group in Groups)
                ReloadGroup(group);

            Engine?.Rebuild();
        }

        private static void ReloadGroup(ItemStatGroupViewModel group)
        {
            foreach (var cell in group.Cells)
                cell.Reload();

            foreach (var entryList in group.EntryLists)
                entryList.Reload();

            foreach (var exclusiveChoice in group.ExclusiveChoices)
                exclusiveChoice.Reload();
        }

        private ItemStatGroupViewModel BuildGroup(ItemStatGroup group)
        {
            var definitions = group == ItemStatGroup.Combat
                ? ItemStatVisibility.CombatStatsFor(_family)
                : ItemStatCatalog.ByGroup(group);

            var contextDefinitions = ItemStatVisibility.MultiEntryFor(_family, group);

            var entryLists = contextDefinitions
                .Where(definition => !definition.IsExclusive)
                .Select(BuildEntryList)
                .ToList();

            var exclusiveChoices = contextDefinitions
                .Where(definition => definition.IsExclusive)
                .Select(BuildExclusiveChoice)
                .ToList();

            return new ItemStatGroupViewModel(
                group, definitions, _store, _runEdit, _valueChanged, entryLists, exclusiveChoices, _costTableMax);
        }

        private ItemPropertyEntryListViewModel BuildEntryList(ItemMultiEntryDefinition definition) =>
            new(definition, _store, _runEdit, ResolveSubtypeChoices(definition.SubtypeTableResRef), _valueChanged,
                _costTableMax);

        private ItemExclusiveChoiceViewModel BuildExclusiveChoice(ItemMultiEntryDefinition definition) =>
            new(definition, _store, _runEdit, ResolveSubtypeChoices(definition.SubtypeTableResRef), _valueChanged);

        private IReadOnlyList<BehaviorChoice> ResolveSubtypeChoices(string tableResRef) =>
            _resolveChoices?.Invoke($"{SubtypeKeyPrefix}{tableResRef}") ?? Array.Empty<BehaviorChoice>();
    }
}
