using System.Collections.ObjectModel;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Toolset.Domain.Editors.Behaviors;
using SWLOR.Toolset.Domain.Editors.Items;

namespace SWLOR.Toolset.Editors.Items
{
    /// <summary>
    /// The item editor's Requirements tab: skill requirements grouped by
    /// <see cref="SkillCategoryType"/>, the six ability-score requirements, and the perk/race gates.
    /// </summary>
    /// <remarks>
    /// The perk (100) and race (64) gates are multi-subtype in the same sense
    /// <see cref="ItemMultiEntryCatalog"/> models FoodBonus/DroidInstruction/etc: a real item can
    /// gate on more than one perk or more than one race at once, and the subtype it stores is the
    /// perk/race id itself rather than a fixed enumeration <see cref="ItemStatCatalog"/> would expand
    /// inline. They surface as <see cref="ItemPropertyEntryListViewModel"/>s here rather than the
    /// single wrapped cell the groundwork pass used before a real subtype picker existed
    /// (<see cref="ItemMultiEntryCatalog.IsRequirement"/> marks exactly these two).
    /// </remarks>
    public sealed class ItemRequirementsSectionViewModel
    {
        private const string SubtypeKeyPrefix = "item.subtypes:";

        private readonly ItemValueStore _store;
        private readonly Func<string, Action, bool> _runEdit;
        private readonly Action? _valueChanged;
        private readonly Func<string, IReadOnlyList<BehaviorChoice>>? _resolveChoices;
        private ItemCostTableRanges? _costTables;

        public ObservableCollection<ItemRequirementGroupViewModel> Groups { get; } = new();

        /// <summary>The perk and race gates (<see cref="ItemMultiEntryDefinition.IsRequirement"/>), one list each.</summary>
        public IReadOnlyList<ItemPropertyEntryListViewModel> EntryLists { get; private set; } =
            Array.Empty<ItemPropertyEntryListViewModel>();

        public ItemRequirementsSectionViewModel(
            ItemValueStore store,
            Func<string, Action, bool> runEdit,
            Action? valueChanged = null,
            Func<string, IReadOnlyList<BehaviorChoice>>? resolveChoices = null,
            ItemCostTableRanges? costTables = null)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _runEdit = runEdit ?? throw new ArgumentNullException(nameof(runEdit));
            _valueChanged = valueChanged;
            _resolveChoices = resolveChoices;
            _costTables = costTables;

            Build();
        }

        /// <summary>Re-reads every built cell and entry list from the document.</summary>
        public void ReloadFromDocument()
        {
            foreach (var group in Groups)
                foreach (var cell in group.Cells)
                    cell.Reload();

            foreach (var entryList in EntryLists)
                entryList.Reload();
        }

        /// <summary>
        /// Rebuilds controls whose immutable option lists came from the assigned HAK stack.
        /// </summary>
        public void ReloadGameResources(ItemCostTableRanges? costTables)
        {
            _costTables = costTables;
            Build();
        }

        private void Build()
        {
            Groups.Clear();

            // Skill requirements, one card per SkillCategoryType. Languages never appears here -
            // ItemRequirementCatalog already excludes it when it builds its Skill rows.
            var skillRequirements = ItemRequirementCatalog.ByCategory(ItemRequirementCategory.Skill);
            foreach (var category in skillRequirements
                         .Select(requirement => requirement.SkillCategory)
                         .Where(category => category != null)
                         .Select(category => category!.Value)
                         .Distinct())
            {
                var cells = skillRequirements
                    .Where(requirement => requirement.SkillCategory == category)
                    .Select(BuildCell)
                    .ToList();
                Groups.Add(new ItemRequirementGroupViewModel($"{category} skills", cells));
            }

            var statCells = ItemRequirementCatalog.ByCategory(ItemRequirementCategory.Stat)
                .Select(BuildCell)
                .ToList();
            Groups.Add(new ItemRequirementGroupViewModel("Required stat", statCells));

            EntryLists = ItemMultiEntryCatalog.All
                .Where(definition => definition.IsRequirement)
                .Select(BuildEntryList)
                .ToList();
        }

        private ItemPropertyEntryListViewModel BuildEntryList(ItemMultiEntryDefinition definition) =>
            new(definition, _store, _runEdit, ResolveSubtypeChoices(definition.SubtypeTableResRef), _valueChanged,
                _costTables);

        private IReadOnlyList<BehaviorChoice> ResolveSubtypeChoices(string tableResRef) =>
            _resolveChoices?.Invoke($"{SubtypeKeyPrefix}{tableResRef}") ?? Array.Empty<BehaviorChoice>();

        /// <summary>
        /// Wraps a requirement row as an <see cref="ItemStatDefinition"/> so it can share
        /// <see cref="ItemStatCellViewModel"/> with the Stats tab. The wrapper's Group and
        /// DisplayOrder are never read by the cell - only Label/PropertyId/SubtypeId/CostTableId are.
        /// </summary>
        private ItemStatCellViewModel BuildCell(ItemRequirementDefinition requirement)
        {
            var definition = new ItemStatDefinition(
                ItemStatGroup.Utility,
                requirement.Label,
                requirement.PropertyId,
                requirement.SubtypeId,
                requirement.CostTableId,
                requirement.DisplayOrder);

            return new ItemStatCellViewModel(definition, _store, _runEdit, _valueChanged, _costTables);
        }
    }
}
