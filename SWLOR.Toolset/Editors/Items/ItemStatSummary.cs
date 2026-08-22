using SWLOR.Toolset.Domain.Editors.Behaviors;
using SWLOR.Toolset.Domain.Editors.Items;
using SWLOR.Toolset.Domain.Gff;

namespace SWLOR.Toolset.Editors.Items
{
    /// <summary>One read-only gameplay value shown outside the full item editor.</summary>
    public sealed record ItemStatSummaryEntry(string Label, string Value);

    /// <summary>One item-editor stat card projected into a compact, read-only group.</summary>
    public sealed record ItemStatSummaryGroup(
        string Title,
        IReadOnlyList<ItemStatSummaryEntry> Entries);

    /// <summary>
    /// A compact stat summary split so a narrow row can trim the stats without hiding its
    /// remaining-stat count.
    /// </summary>
    public sealed record ItemStatCompactSummary(string Primary, string Overflow)
    {
        public bool HasOverflow => !string.IsNullOrEmpty(Overflow);
        public string Text => HasOverflow ? $"{Primary}  ·  {Overflow}" : Primary;
    }

    /// <summary>
    /// Builds item-property summaries from the same catalogs and cost-table labels as the item
    /// editor. Equipment, merchants, loot, and any future item browser can therefore show what an
    /// item does without maintaining another property-id list or exposing raw CostValue rows.
    /// </summary>
    public static class ItemStatSummary
    {
        private const string SubtypeKeyPrefix = "item.subtypes:";

        private static readonly ItemStatGroup[] PreferredGroupOrder =
        {
            ItemStatGroup.Defense,
            ItemStatGroup.Vitals,
            ItemStatGroup.Resistance,
            ItemStatGroup.Combat,
            ItemStatGroup.Utility
        };

        public static IReadOnlyList<ItemStatSummaryGroup> Build(
            JsonGffStruct item,
            ItemCostTableRanges? costTables = null,
            Func<string, IReadOnlyList<BehaviorChoice>>? resolveChoices = null)
        {
            ArgumentNullException.ThrowIfNull(item);

            var groups = new Dictionary<(int Rank, string Title), List<ItemStatSummaryEntry>>();
            var store = new ItemValueStore(item);

            foreach (var property in store.Properties)
            {
                var stat = ItemStatCatalog.All.FirstOrDefault(candidate =>
                    candidate.PropertyId == property.PropertyId &&
                    (candidate.SubtypeId < 0 || candidate.SubtypeId == property.SubtypeId));
                if (stat != null)
                {
                    Add(
                        GroupRank(stat.Group),
                        ItemStatGroupViewModel.TitleFor(stat.Group),
                        stat.Label,
                        CostValueDisplay(costTables, stat.CostTableId, property.CostValue));
                    continue;
                }

                var requirement = ItemRequirementCatalog.All.FirstOrDefault(candidate =>
                    candidate.PropertyId == property.PropertyId &&
                    candidate.SubtypeId >= 0 && candidate.SubtypeId == property.SubtypeId);
                if (requirement != null)
                {
                    Add(
                        100,
                        "Requirements",
                        requirement.Label,
                        CostValueDisplay(costTables, requirement.CostTableId, property.CostValue));
                    continue;
                }

                var multi = ItemMultiEntryCatalog.ByPropertyId(property.PropertyId);
                if (multi != null)
                {
                    var subtype = SubtypeDisplay(
                        resolveChoices, multi.SubtypeTableResRef, property.SubtypeId);

                    if (multi.IsRequirement)
                    {
                        if (multi.CostTableId < 0)
                        {
                            Add(100, "Requirements", multi.Label, subtype);
                        }
                        else
                        {
                            Add(
                                100,
                                "Requirements",
                                $"{multi.Label}: {subtype}",
                                CostValueDisplay(costTables, multi.CostTableId, property.CostValue));
                        }
                    }
                    else if (multi.Context is { } context)
                    {
                        Add(
                            GroupRank(context),
                            ItemStatGroupViewModel.TitleFor(context),
                            multi.IsExclusive || multi.CostTableId < 0 ? multi.Label : subtype,
                            multi.IsExclusive || multi.CostTableId < 0
                                ? subtype
                                : CostValueDisplay(costTables, multi.CostTableId, property.CostValue));
                    }

                    continue;
                }

                // Preserve the item editor's existing engine-property accounting. Properties not
                // in that catalog (for example the deliberately hidden unlimited-ammo marker) stay
                // behind the scenes instead of leaking raw engine rows into builder-facing UI.
                var legacy = ItemEngineLegacyCatalog.All.FirstOrDefault(candidate =>
                    candidate.PropertyId == property.PropertyId);
                if (legacy == null)
                    continue;

                var legacyLabel = string.IsNullOrWhiteSpace(legacy.SubtypeTableResRef)
                    ? legacy.Label
                    : $"{legacy.Label}: {SubtypeDisplay(resolveChoices, legacy.SubtypeTableResRef, property.SubtypeId)}";
                Add(
                    200,
                    "Additional stats",
                    legacyLabel,
                    CostValueDisplay(costTables, legacy.CostTableId, property.CostValue));
            }

            return groups
                .OrderBy(group => group.Key.Rank)
                .ThenBy(group => group.Key.Title, StringComparer.OrdinalIgnoreCase)
                .Select(group => new ItemStatSummaryGroup(group.Key.Title, group.Value))
                .ToList();

            void Add(int rank, string title, string label, string value)
            {
                var key = (rank, title);
                if (!groups.TryGetValue(key, out var entries))
                {
                    entries = new List<ItemStatSummaryEntry>();
                    groups.Add(key, entries);
                }

                entries.Add(new ItemStatSummaryEntry(label, value));
            }
        }

        /// <summary>
        /// A bounded row summary whose overflow is separate so the UI can reserve space for it.
        /// </summary>
        public static ItemStatCompactSummary CompactParts(
            IReadOnlyList<ItemStatSummaryGroup>? groups)
        {
            var entries = groups?.SelectMany(group => group.Entries).ToList()
                          ?? new List<ItemStatSummaryEntry>();
            if (entries.Count == 0)
                return new ItemStatCompactSummary("No gameplay stats", string.Empty);

            const int shown = 4;
            var primary = string.Join("  ·  ", entries.Take(shown).Select(entry =>
                $"{entry.Label} {entry.Value}"));
            var overflow = entries.Count > shown
                ? $"+{entries.Count - shown} more"
                : string.Empty;
            return new ItemStatCompactSummary(primary, overflow);
        }

        /// <summary>A plain-text form for consumers that do not control row layout.</summary>
        public static string Compact(IReadOnlyList<ItemStatSummaryGroup>? groups) =>
            CompactParts(groups).Text;

        private static int GroupRank(ItemStatGroup group)
        {
            var preferred = Array.IndexOf(PreferredGroupOrder, group);
            return preferred >= 0 ? preferred : PreferredGroupOrder.Length + (int)group;
        }

        private static string CostValueDisplay(
            ItemCostTableRanges? costTables,
            int costTableId,
            int costValue)
        {
            foreach (var option in costTables?.OptionsFor(costTableId)
                                   ?? Array.Empty<ItemCostTableOption>())
            {
                if (option.Value == costValue)
                    return option.Label;
            }

            return costValue.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        private static string SubtypeDisplay(
            Func<string, IReadOnlyList<BehaviorChoice>>? resolveChoices,
            string tableResRef,
            int subtypeId)
        {
            var choices = resolveChoices?.Invoke($"{SubtypeKeyPrefix}{tableResRef}")
                          ?? Array.Empty<BehaviorChoice>();
            return choices.FirstOrDefault(choice => choice.Value == subtypeId)?.Display
                   ?? subtypeId.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
