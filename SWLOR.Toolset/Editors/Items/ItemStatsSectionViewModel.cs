using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using SWLOR.Toolset.Domain.Editors.Behaviors;
using SWLOR.Toolset.Domain.Editors.Items;

namespace SWLOR.Toolset.Editors.Items
{
    /// <summary>
    /// The item editor's Stats tab: a set of <see cref="ItemStatGroupViewModel"/> cards chosen by
    /// family, role, and what the open blueprint actually stores, plus the family-independent
    /// engine-legacy sweep (<see cref="Engine"/>). A group the item has no value in and its family
    /// does not use is simply absent - there is no secondary "not used by this base type" section
    /// to expand.
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

        /// <summary>
        /// The properties the open blueprint carries, snapshotted at the start of each
        /// <see cref="Rebuild"/> - every group built in that pass sees the same set.
        /// </summary>
        private IReadOnlySet<int> _storedProperties = new HashSet<int>();

        public ObservableCollection<ItemStatGroupViewModel> Groups { get; } = new();

        /// <summary>
        /// <see cref="Groups"/> dealt into two columns of roughly equal height. A uniform-row
        /// layout sized every row to its tallest card, so a two-row Defense card beside an
        /// eight-row Resistance card left a hole the size of six rows underneath it; packing each
        /// column independently closes those.
        /// </summary>
        public ObservableCollection<object> LeftColumn { get; } = new();

        public ObservableCollection<object> RightColumn { get; } = new();

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
            _storedProperties = _store.Properties.Select(property => property.PropertyId).ToHashSet();

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

            // The family decides what a blueprint of this kind USUALLY has; what this blueprint
            // actually carries decides the rest. A cook's armor holds Crafting stats, an armor-based
            // enhancement module holds Enhancements, an essence holds weapon DMG - none of which the
            // family alone would show, leaving real stored values invisible and uneditable. Only
            // groups with a value on THIS item are added, so nothing empty appears: this is not the
            // "stats your base type doesn't use" list, it is "the stats this item has".
            foreach (var group in StoredGroups())
            {
                if (!primaryIds.Contains(group))
                    primaryIds.Add(group);
            }

            Groups.Clear();
            foreach (var groupId in primaryIds)
                Groups.Add(BuildGroup(groupId));

            Engine = new ItemEngineLegacySectionViewModel(_store, _runEdit, _resolveChoices, _costTableMax);

            LayOutColumns();
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
            foreach (var (card, rows) in Cards())
            {
                if (leftRows <= rightRows)
                {
                    LeftColumn.Add(card);
                    leftRows += rows;
                }
                else
                {
                    RightColumn.Add(card);
                    rightRows += rows;
                }
            }
        }

        /// <summary>
        /// Every card the columns have to place, tallest first, with the row count that drives its
        /// height. Dealing in size order matters: taking them in declaration order let a tall card
        /// arrive last with nowhere balanced to go.
        /// </summary>
        private IEnumerable<(object Card, int Rows)> Cards()
        {
            var cards = Groups
                .Select(group => ((object)group,
                    group.Cells.Count + group.EntryLists.Count + group.ExclusiveChoices.Count))
                .ToList();

            // The engine sweep is a card like any other. It used to sit below the whole grid, so
            // the space under the shorter column stayed empty and the sweep began below the taller
            // one - a hole the size of the difference, with the answer to it directly underneath.
            if (Engine is { HasEntries: true } engine)
                cards.Add((engine, engine.Entries.Count + 1));

            return cards.OrderByDescending(card => card.Item2);
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

        /// <summary>Every stat group this item already stores at least one property in.</summary>
        private IReadOnlyList<ItemStatGroup> StoredGroups()
        {
            if (_storedProperties.Count == 0)
                return Array.Empty<ItemStatGroup>();

            var groups = new List<ItemStatGroup>();
            foreach (var stat in ItemStatCatalog.All)
            {
                if (_storedProperties.Contains(stat.PropertyId) && !groups.Contains(stat.Group))
                    groups.Add(stat.Group);
            }

            foreach (var definition in ItemMultiEntryCatalog.All)
            {
                // A requirement has no stat group of its own - it lives on the Requirements tab,
                // which every family shows anyway.
                if (definition.IsRequirement || definition.Context is not { } context)
                    continue;

                if (_storedProperties.Contains(definition.PropertyId) && !groups.Contains(context))
                    groups.Add(context);
            }

            return groups;
        }

        private ItemStatGroupViewModel BuildGroup(ItemStatGroup group)
        {
            var definitions = group == ItemStatGroup.Combat
                ? ItemStatVisibility.CombatStatsFor(_family, _storedProperties)
                : ItemStatCatalog.ByGroup(group);

            var contextDefinitions = ItemStatVisibility.MultiEntryFor(_family, group, _storedProperties);

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
