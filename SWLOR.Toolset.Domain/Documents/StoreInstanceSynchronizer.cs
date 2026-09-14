using System.Globalization;
using System.Text;
using SWLOR.Toolset.Domain.Editing;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Domain.Documents
{
    /// <summary>The source-backed records that the CLI-style store expansion would replace.</summary>
    public sealed record StoreInstanceSyncStatus(
        int OutOfDateMerchantRecords,
        int OutOfDateItemRecords)
    {
        public bool IsCurrent => OutOfDateMerchantRecords == 0 && OutOfDateItemRecords == 0;
    }

    /// <summary>
    /// Builds the canonical placed-store shape used by the CLI store synchronizer while preserving
    /// placement and per-slot metadata owned by the instance.
    /// </summary>
    public static class StoreInstanceSynchronizer
    {
        public static JsonGffStruct BuildExpected(
            JsonGffDocument storeBlueprint,
            JsonGffStruct placedStore,
            string storeResRef,
            Func<string, JsonGffDocument?> loadItemBlueprint)
        {
            ArgumentNullException.ThrowIfNull(storeBlueprint);
            ArgumentNullException.ThrowIfNull(placedStore);
            ArgumentException.ThrowIfNullOrWhiteSpace(storeResRef);
            ArgumentNullException.ThrowIfNull(loadItemBlueprint);

            // This builds a detached replacement while an editor DocumentSession is open. The
            // mutation guard is ambient, so explicitly mark it as construction rather than an edit
            // to the merchant document's undo stack.
            using var construction = EditScope.EnterConstruction();

            var expected = JsonGffField.CreateStruct(StructId(placedStore)).Struct!;
            foreach (var (name, field) in storeBlueprint.Root.Entries)
            {
                if (name is "Comment" or "ID")
                    continue;

                expected.Add(
                    name,
                    name == "StoreList"
                        ? BuildExpectedStoreList(
                            storeBlueprint.Root.GetListOrEmpty("StoreList"),
                            placedStore,
                            loadItemBlueprint)
                        : CloneField(field));
            }

            // These are instance-owned. They follow the blueprint fields in the same order as the
            // existing CLI synchronizer, which makes its output and this editor's status agree.
            CopySingleIfPresent(placedStore, expected, "XOrientation");
            CopySingleIfPresent(placedStore, expected, "XPosition");
            CopySingleIfPresent(placedStore, expected, "YOrientation");
            CopySingleIfPresent(placedStore, expected, "YPosition");
            CopySingleIfPresent(placedStore, expected, "ZPosition");
            return expected;
        }

        public static bool IsCurrent(
            JsonGffDocument storeBlueprint,
            JsonGffStruct placedStore,
            string storeResRef,
            Func<string, JsonGffDocument?> loadItemBlueprint) =>
            Inspect(storeBlueprint, placedStore, storeResRef, loadItemBlueprint).IsCurrent;

        /// <summary>
        /// Counts stale records with the same semantics as SWLOR.CLI's store sync report: the
        /// placed store is one merchant record when its canonical expansion differs, and each
        /// existing source-backed embedded item that differs from its UTI is one item record.
        /// Missing/extra inventory membership is represented by the stale merchant record because
        /// there is no corresponding embedded item record to compare.
        /// </summary>
        public static StoreInstanceSyncStatus Inspect(
            JsonGffDocument storeBlueprint,
            JsonGffStruct placedStore,
            string storeResRef,
            Func<string, JsonGffDocument?> loadItemBlueprint)
        {
            var expected = BuildExpected(
                storeBlueprint,
                placedStore,
                storeResRef,
                loadItemBlueprint);
            if (Equivalent(placedStore, expected))
                return new StoreInstanceSyncStatus(0, 0);

            var outOfDateItems = 0;
            var existingItems = ExistingItemLookup(placedStore);
            foreach (var pane in expected.GetListOrEmpty("StoreList"))
            {
                foreach (var expectedItem in pane.GetListOrEmpty("ItemList"))
                {
                    var itemResRef = ItemResRef(expectedItem);
                    var existingItem = TakeExisting(existingItems, itemResRef);
                    if (existingItem == null ||
                        string.IsNullOrWhiteSpace(itemResRef) ||
                        loadItemBlueprint(itemResRef) == null)
                    {
                        continue;
                    }

                    if (!Equivalent(existingItem, expectedItem))
                        outOfDateItems++;
                }
            }

            return new StoreInstanceSyncStatus(1, outOfDateItems);
        }

        public static bool Equivalent(JsonGffStruct left, JsonGffStruct right)
        {
            var leftDocument = new JsonGffDocument("GFF ", left)
            {
                UsesCrLf = false,
                HasTrailingNewline = false,
                TrailingNewlineUsesCrLf = false
            };
            var rightDocument = new JsonGffDocument("GFF ", right)
            {
                UsesCrLf = false,
                HasTrailingNewline = false,
                TrailingNewlineUsesCrLf = false
            };
            return leftDocument.ToBytes().AsSpan().SequenceEqual(rightDocument.ToBytes());
        }

        private static JsonGffField BuildExpectedStoreList(
            IReadOnlyList<JsonGffStruct> sourcePanes,
            JsonGffStruct placedStore,
            Func<string, JsonGffDocument?> loadItemBlueprint)
        {
            var expected = JsonGffField.CreateList();
            var existingItems = ExistingItemLookup(placedStore);

            foreach (var sourcePane in sourcePanes)
            {
                var pane = JsonGffField.CreateStruct(StructId(sourcePane)).Struct!;

                if (sourcePane.GetOrNull("ItemList") is { Elements: { } sourceItems })
                {
                    var itemList = JsonGffField.CreateList();
                    foreach (var sourceSlot in sourceItems)
                    {
                        var itemResRef = sourceSlot.GetStringOrNull("InventoryRes") ?? string.Empty;
                        var existingItem = TakeExisting(existingItems, itemResRef);
                        var itemBlueprint = string.IsNullOrWhiteSpace(itemResRef)
                            ? null
                            : loadItemBlueprint(itemResRef);

                        var expanded = itemBlueprint != null
                            ? BuildExpectedItem(itemBlueprint, sourceSlot, existingItem, itemResRef)
                            : PreserveExistingItem(sourceSlot, existingItem);
                        if (expanded != null)
                            itemList.InsertElement(itemList.Elements!.Count, expanded);
                    }

                    pane.Add("ItemList", itemList);
                }

                expected.InsertElement(expected.Elements!.Count, pane);
            }

            return expected;
        }

        private static JsonGffStruct BuildExpectedItem(
            JsonGffDocument itemBlueprint,
            JsonGffStruct sourceSlot,
            JsonGffStruct? existingItem,
            string itemResRef)
        {
            _ = itemResRef;
            var item = JsonGffField.CreateStruct(
                StructIdOrNull(sourceSlot) ?? (existingItem == null ? null : StructIdOrNull(existingItem)) ?? 0).Struct!;

            var insertedInfinite = false;
            var insertedRepositoryPosition = false;
            foreach (var (name, field) in itemBlueprint.Root.Entries)
            {
                if (name is "Comment" or "PaletteID")
                    continue;

                if (!insertedInfinite && name == "LocalizedName")
                {
                    SetInfinite(item, sourceSlot, existingItem);
                    insertedInfinite = true;
                }

                if (!insertedRepositoryPosition && name == "StackSize")
                {
                    SetRepositoryPosition(item, sourceSlot, existingItem);
                    insertedRepositoryPosition = true;
                }

                item.Add(name, CloneField(field));

                if (!insertedInfinite && name == "Identified")
                {
                    SetInfinite(item, sourceSlot, existingItem);
                    insertedInfinite = true;
                }

                if (!insertedRepositoryPosition && name == "PropertiesList")
                {
                    SetRepositoryPosition(item, sourceSlot, existingItem);
                    insertedRepositoryPosition = true;
                }
            }

            if (!insertedInfinite)
                SetInfinite(item, sourceSlot, existingItem);
            if (!insertedRepositoryPosition)
                SetRepositoryPosition(item, sourceSlot, existingItem);

            item.SetSingle("XOrientation", existingItem?.GetSingleOrNull("XOrientation") ?? 0f);
            item.SetSingle("XPosition", existingItem?.GetSingleOrNull("XPosition") ?? -1f);
            item.SetSingle("YOrientation", existingItem?.GetSingleOrNull("YOrientation") ?? 1f);
            item.SetSingle("YPosition", existingItem?.GetSingleOrNull("YPosition") ?? -1f);
            item.SetSingle("ZPosition", existingItem?.GetSingleOrNull("ZPosition") ?? -1f);
            return item;
        }

        private static JsonGffStruct? PreserveExistingItem(
            JsonGffStruct sourceSlot,
            JsonGffStruct? existingItem)
        {
            if (existingItem == null)
                return null;

            var preserved = InstanceFieldMap.Duplicate(existingItem);
            preserved.SetStructId(StructIdOrNull(sourceSlot) ?? StructId(existingItem));
            ApplySlotMetadata(preserved, sourceSlot, existingItem);
            return preserved;
        }

        private static void ApplySlotMetadata(
            JsonGffStruct target,
            JsonGffStruct sourceSlot,
            JsonGffStruct? existingItem)
        {
            SetInfinite(target, sourceSlot, existingItem);
            SetRepositoryPosition(target, sourceSlot, existingItem);
        }

        private static void SetInfinite(
            JsonGffStruct target,
            JsonGffStruct sourceSlot,
            JsonGffStruct? existingItem) =>
            target.SetInt(
                "Infinite",
                GffFieldType.Byte,
                sourceSlot.GetIntOrNull("Infinite") ?? existingItem?.GetIntOrNull("Infinite") ?? 1);

        private static void SetRepositoryPosition(
            JsonGffStruct target,
            JsonGffStruct sourceSlot,
            JsonGffStruct? existingItem)
        {
            target.SetInt(
                "Repos_PosX",
                GffFieldType.Word,
                sourceSlot.GetIntOrNull("Repos_PosX") ?? existingItem?.GetIntOrNull("Repos_PosX") ?? 0);
            target.SetInt(
                "Repos_Posy",
                GffFieldType.Word,
                sourceSlot.GetIntOrNull("Repos_Posy") ?? existingItem?.GetIntOrNull("Repos_Posy") ?? 0);
        }

        private static Dictionary<string, Queue<JsonGffStruct>> ExistingItemLookup(JsonGffStruct store)
        {
            var lookup = new Dictionary<string, Queue<JsonGffStruct>>(StringComparer.OrdinalIgnoreCase);
            foreach (var pane in store.GetListOrEmpty("StoreList"))
            {
                foreach (var item in pane.GetListOrEmpty("ItemList"))
                {
                    var resRef = ItemResRef(item);
                    if (string.IsNullOrWhiteSpace(resRef))
                        continue;

                    if (!lookup.TryGetValue(resRef, out var queue))
                    {
                        queue = new Queue<JsonGffStruct>();
                        lookup[resRef] = queue;
                    }

                    queue.Enqueue(item);
                }
            }

            return lookup;
        }

        private static JsonGffStruct? TakeExisting(
            IReadOnlyDictionary<string, Queue<JsonGffStruct>> lookup,
            string resRef) =>
            !string.IsNullOrWhiteSpace(resRef) &&
            lookup.TryGetValue(resRef, out var queue) &&
            queue.Count > 0
                ? queue.Dequeue()
                : null;

        private static string ItemResRef(JsonGffStruct item)
        {
            var resRef = item.GetStringOrNull("TemplateResRef");
            return string.IsNullOrWhiteSpace(resRef)
                ? item.GetStringOrNull("Tag") ?? string.Empty
                : resRef;
        }

        private static void CopySingleIfPresent(
            JsonGffStruct source,
            JsonGffStruct target,
            string fieldName)
        {
            if (source.GetSingleOrNull(fieldName) is { } value)
                target.SetSingle(fieldName, value);
        }

        private static JsonGffField CloneField(JsonGffField source)
        {
            JsonGffField clone;
            switch (source.Type)
            {
                case GffFieldType.Struct:
                    clone = JsonGffField.CreateStruct(
                        source.GetStructId() ?? StructId(source.Struct!));
                    CopyStruct(source.Struct!, clone.Struct!);
                    break;
                case GffFieldType.List:
                    clone = JsonGffField.CreateList();
                    foreach (var element in source.Elements ?? new List<JsonGffStruct>())
                    {
                        var child = JsonGffField.CreateStruct(StructId(element)).Struct!;
                        CopyStruct(element, child);
                        clone.InsertElement(clone.Elements!.Count, child);
                    }
                    break;
                case GffFieldType.CExoLocString:
                    clone = JsonGffField.CreateLocString();
                    foreach (var entry in source.LocStringEntries ?? new List<LocStringEntry>())
                    {
                        clone.AddLocStringEntry(
                            new LocStringEntry(entry.LanguageKey, (byte[])entry.RawText.Clone()));
                    }
                    break;
                default:
                    clone = JsonGffField.CreateScalar(
                        source.Type,
                        (byte[])(source.RawValue?.Clone()
                                 ?? throw new InvalidDataException("Scalar field is missing its value.")));
                    break;
            }

            clone.RawLocStringId = source.RawLocStringId == null
                ? null
                : (byte[])source.RawLocStringId.Clone();
            return clone;
        }

        private static void CopyStruct(JsonGffStruct source, JsonGffStruct target)
        {
            foreach (var (name, field) in source.Entries)
                target.Add(name, CloneField(field));
        }

        private static uint StructId(JsonGffStruct value) => StructIdOrNull(value) ?? 0;

        private static uint? StructIdOrNull(JsonGffStruct value) =>
            value.RawStructId == null
                ? null
                : uint.Parse(Encoding.ASCII.GetString(value.RawStructId), CultureInfo.InvariantCulture);
    }
}
