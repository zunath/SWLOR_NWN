using System.Text.RegularExpressions;
using SWLOR.Toolset.Domain.Editors.Items;

namespace SWLOR.Toolset.Editors.Items
{
    /// <summary>
    /// One Stats-tab card: every <see cref="ItemStatDefinition"/> in a single
    /// <see cref="ItemStatGroup"/>, built into cells against the same document and edit runner.
    /// </summary>
    /// <remarks>
    /// <see cref="IsMatrix"/> is always false in this pass - the Crafting group's Control /
    /// Craftsmanship / CP Bonus rows read fine as a flat cell list (each label already names its
    /// craft type, e.g. "Control (Smithery)"), and building a true row/column matrix view here would
    /// require the Stats tab layout this groundwork does not own. Left as a deliberate deviation from
    /// the ask rather than a half-built matrix.
    /// </remarks>
    public sealed class ItemStatGroupViewModel
    {
        public ItemStatGroup Group { get; }

        public string Title { get; }

        public IReadOnlyList<ItemStatCellViewModel> Cells { get; }

        /// <summary>
        /// The multi-subtype properties (<see cref="ItemMultiEntryCatalog"/>) whose Context matches
        /// this group - empty for every group with no such property (most of them).
        /// </summary>
        public IReadOnlyList<ItemPropertyEntryListViewModel> EntryLists { get; }

        /// <summary>
        /// The exclusive multi-subtype properties (<see cref="ItemMultiEntryDefinition.IsExclusive"/>)
        /// whose Context matches this group - a single pick-one-or-none dropdown each, rendered
        /// separately from <see cref="EntryLists"/>' add/remove lists.
        /// </summary>
        public IReadOnlyList<ItemExclusiveChoiceViewModel> ExclusiveChoices { get; }

        public bool IsMatrix => false;

        public ItemStatGroupViewModel(
            ItemStatGroup group,
            IReadOnlyList<ItemStatDefinition> definitions,
            ItemValueStore store,
            Func<string, Action, bool> runEdit,
            Action? valueChanged,
            IReadOnlyList<ItemPropertyEntryListViewModel>? entryLists = null,
            IReadOnlyList<ItemExclusiveChoiceViewModel>? exclusiveChoices = null,
            ItemCostTableRanges? costTables = null)
        {
            ArgumentNullException.ThrowIfNull(definitions);

            Group = group;
            Title = TitleFor(group);
            Cells = definitions
                .Select(definition => new ItemStatCellViewModel(definition, store, runEdit, valueChanged, costTables))
                .ToList();
            EntryLists = entryLists ?? Array.Empty<ItemPropertyEntryListViewModel>();
            ExclusiveChoices = exclusiveChoices ?? Array.Empty<ItemExclusiveChoiceViewModel>();
        }

        /// <summary>Splits the enum's PascalCase name into words and sentence-cases them.</summary>
        public static string TitleFor(ItemStatGroup group)
        {
            var words = Regex.Matches(group.ToString(), "[A-Z][a-z0-9]*")
                .Select(match => match.Value)
                .ToList();

            for (var index = 1; index < words.Count; index++)
                words[index] = words[index].ToLowerInvariant();

            return words.Count == 0 ? group.ToString() : string.Join(' ', words);
        }
    }
}
