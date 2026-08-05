using SWLOR.Toolset.Domain.Editors.Behaviors;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Gff;

namespace SWLOR.Toolset.Domain.Editors.Merchants
{
    /// <summary>Typed mutations for UTM inventory panes and base-item buying rules.</summary>
    public sealed class MerchantValueStore : BehaviorValueStore
    {
        public const int InventoryPaneCount = 5;
        public const string WillNotBuyField = "WillNotBuy";
        public const string WillOnlyBuyField = "WillOnlyBuy";
        private const uint BuyingRuleStructId = 97869;
        private const int InventoryColumns = 5;
        private const int InventoryCellSpacing = 2;

        public MerchantValueStore(JsonGffStruct merchant) : base(merchant)
        {
        }

        public IReadOnlyList<JsonGffStruct> Inventory(int paneIndex)
        {
            var panes = Owner.GetListOrEmpty("StoreList");
            if (paneIndex < 0 || paneIndex >= panes.Count)
                return Array.Empty<JsonGffStruct>();

            return panes[paneIndex].GetListOrEmpty("ItemList");
        }

        public void AddInventoryItem(int paneIndex, string resRef)
        {
            ValidatePane(paneIndex);
            JsonGffField.ValidateStringValue(GffFieldType.ResRef, resRef);

            var pane = EnsurePane(paneIndex);
            var items = pane.GetOrNull("ItemList");
            if (items == null)
            {
                items = JsonGffField.CreateList();
                pane.Add("ItemList", items);
            }

            var index = items.Elements!.Count;
            var item = JsonGffField.CreateStruct((uint)index).Struct!;
            item.SetInt("Infinite", GffFieldType.Byte, 1);
            item.SetString("InventoryRes", GffFieldType.ResRef, resRef);
            SetInventoryPosition(item, index);
            items.InsertElement(index, item);
        }

        public void RemoveInventoryItem(int paneIndex, int itemIndex)
        {
            var pane = PaneOrNull(paneIndex);
            var items = pane?.GetOrNull("ItemList");
            if (items?.Elements == null || itemIndex < 0 || itemIndex >= items.Elements.Count)
                return;

            items.RemoveElementAt(itemIndex);
            Renumber(items.Elements);
        }

        /// <summary>Removes multiple inventory slots while preserving the native order of every
        /// pane. All slots are validated before the first mutation so the operation is atomic.</summary>
        public void RemoveInventoryItems(
            IEnumerable<(int PaneIndex, int ItemIndex)> inventoryItems)
        {
            ArgumentNullException.ThrowIfNull(inventoryItems);
            var removals = inventoryItems.Distinct().ToList();
            if (removals.Count == 0)
                return;

            var panes = Owner.GetListOrEmpty("StoreList");
            foreach (var (paneIndex, itemIndex) in removals)
            {
                if (paneIndex < 0 || paneIndex >= panes.Count)
                    throw new ArgumentOutOfRangeException(nameof(inventoryItems));

                var items = panes[paneIndex].GetListOrEmpty("ItemList");
                if (itemIndex < 0 || itemIndex >= items.Count)
                    throw new ArgumentOutOfRangeException(nameof(inventoryItems));
            }

            foreach (var group in removals.GroupBy(removal => removal.PaneIndex))
            {
                var items = panes[group.Key].Get("ItemList");
                foreach (var itemIndex in group.Select(removal => removal.ItemIndex)
                             .OrderByDescending(index => index))
                {
                    items.RemoveElementAt(itemIndex);
                }

                Renumber(items.Elements!);
            }
        }

        public void SetInventoryInfinite(int paneIndex, int itemIndex, bool infinite)
        {
            var item = Inventory(paneIndex).ElementAtOrDefault(itemIndex);
            item?.SetInt("Infinite", GffFieldType.Byte, infinite ? 1 : 0);
        }

        public IReadOnlySet<int> BuyingRuleIds(bool buyOnlySelected)
        {
            var fieldName = buyOnlySelected ? WillOnlyBuyField : WillNotBuyField;
            return Owner.GetListOrEmpty(fieldName)
                .Select(entry => entry.GetIntOrNull("BaseItem"))
                .Where(value => value.HasValue)
                .Select(value => value!.Value)
                .ToHashSet();
        }

        public bool UsesBuyOnlyRules => Owner.GetListOrEmpty(WillOnlyBuyField).Count > 0;

        public void SwitchBuyingRuleMode(bool buyOnlySelected)
        {
            var oldField = buyOnlySelected ? WillNotBuyField : WillOnlyBuyField;
            var newField = buyOnlySelected ? WillOnlyBuyField : WillNotBuyField;
            var selected = Owner.GetListOrEmpty(oldField)
                .Select(entry => entry.GetIntOrNull("BaseItem"))
                .Where(value => value.HasValue)
                .Select(value => value!.Value)
                .ToList();

            if (buyOnlySelected && selected.Count == 0)
            {
                throw new InvalidOperationException(
                    "Select at least one base item type before choosing 'buy only'.");
            }

            ReplaceBuyingRules(newField, selected);
            ReplaceBuyingRules(oldField, Array.Empty<int>());
        }

        public void SetBuyingRule(bool buyOnlySelected, int baseItem, bool selected)
        {
            var fieldName = buyOnlySelected ? WillOnlyBuyField : WillNotBuyField;
            var otherField = buyOnlySelected ? WillNotBuyField : WillOnlyBuyField;
            var ids = BuyingRuleIds(buyOnlySelected).ToHashSet();
            if (selected)
                ids.Add(baseItem);
            else
                ids.Remove(baseItem);

            if (buyOnlySelected && ids.Count == 0)
            {
                throw new InvalidOperationException(
                    "A 'buy only' merchant must keep at least one base item type selected.");
            }

            ReplaceBuyingRules(otherField, Array.Empty<int>());
            ReplaceBuyingRules(fieldName, ids.OrderBy(id => id));
        }

        public void EnsureInventoryPanes()
        {
            var list = Owner.GetOrNull("StoreList");
            if (list == null)
            {
                list = JsonGffField.CreateList();
                Owner.Add("StoreList", list);
            }

            while (list.Elements!.Count < InventoryPaneCount)
            {
                var paneIndex = list.Elements.Count;
                list.InsertElement(paneIndex, JsonGffField.CreateStruct((uint)paneIndex).Struct!);
            }
        }

        /// <summary>Whether all inventory entries are in the pane selected by their BaseItem row,
        /// with index-derived struct ids and repository positions. A null resolver result preserves
        /// an unresolvable legacy entry in its current valid pane. Slot metadata is part of the
        /// check because this answer short-circuits <see cref="NormalizeInventoryPanes"/> - a file
        /// whose panes are category-correct but carries stale or colliding ids/positions must still
        /// be repaired on save.</summary>
        public bool InventoryMatchesCategories(Func<string, int?> resolveStorePanel)
        {
            ArgumentNullException.ThrowIfNull(resolveStorePanel);

            var panes = Owner.GetListOrEmpty("StoreList");
            if (panes.Count != InventoryPaneCount)
                return false;

            for (var paneIndex = 0; paneIndex < panes.Count; paneIndex++)
            {
                var items = panes[paneIndex].GetListOrEmpty("ItemList");
                for (var index = 0; index < items.Count; index++)
                {
                    var item = items[index];
                    var resRef = item.GetStringOrNull("InventoryRes") ?? string.Empty;
                    var expected = resolveStorePanel(resRef);
                    if (expected.HasValue && NormalizePane(expected.Value) != paneIndex)
                        return false;

                    if (!HasIndexDerivedSlotMetadata(item, index))
                        return false;
                }
            }

            return true;
        }

        private static bool HasIndexDerivedSlotMetadata(JsonGffStruct item, int index)
        {
            return item.StructId == (uint)index &&
                   item.GetIntOrNull("Repos_PosX") == index % InventoryColumns * InventoryCellSpacing &&
                   item.GetIntOrNull("Repos_Posy") == index / InventoryColumns * InventoryCellSpacing;
        }

        /// <summary>Moves every resolvable inventory entry into its baseitems.2da StorePanel,
        /// preserves the relative file order within each destination, and leaves exactly five
        /// engine panes with valid item indices and non-overlapping repository positions.</summary>
        public void NormalizeInventoryPanes(Func<string, int?> resolveStorePanel)
        {
            ArgumentNullException.ThrowIfNull(resolveStorePanel);
            if (InventoryMatchesCategories(resolveStorePanel))
                return;

            EnsureInventoryPanes();
            var storeList = Owner.Get("StoreList");
            var entries = new List<(JsonGffStruct Item, int Destination)>();
            for (var paneIndex = 0; paneIndex < storeList.Elements!.Count; paneIndex++)
            {
                var pane = storeList.Elements[paneIndex];
                var items = pane.GetOrNull("ItemList");
                if (items?.Elements == null)
                    continue;

                foreach (var item in items.Elements)
                {
                    var resRef = item.GetStringOrNull("InventoryRes") ?? string.Empty;
                    var resolved = resolveStorePanel(resRef);
                    var destination = resolved.HasValue
                        ? NormalizePane(resolved.Value)
                        : NormalizePane(paneIndex);
                    entries.Add((item, destination));
                }

                while (items.Elements.Count > 0)
                    items.RemoveElementAt(items.Elements.Count - 1);
            }

            while (storeList.Elements.Count > InventoryPaneCount)
                storeList.RemoveElementAt(storeList.Elements.Count - 1);

            foreach (var (item, destination) in entries)
            {
                var pane = storeList.Elements[destination];
                var items = pane.GetOrNull("ItemList");
                if (items == null)
                {
                    items = JsonGffField.CreateList();
                    pane.Add("ItemList", items);
                }

                items.InsertElement(items.Elements!.Count, item);
            }

            foreach (var pane in storeList.Elements)
                Renumber(pane.GetListOrEmpty("ItemList"));
        }

        public void EnsureBuyingRuleLists()
        {
            EnsureList(WillNotBuyField);
            EnsureList(WillOnlyBuyField);
        }

        private JsonGffStruct EnsurePane(int paneIndex)
        {
            EnsureInventoryPanes();
            return Owner.GetListOrEmpty("StoreList")[paneIndex];
        }

        private JsonGffStruct? PaneOrNull(int paneIndex)
        {
            var panes = Owner.GetListOrEmpty("StoreList");
            return paneIndex >= 0 && paneIndex < panes.Count ? panes[paneIndex] : null;
        }

        private void ReplaceBuyingRules(string fieldName, IEnumerable<int> ids)
        {
            var list = EnsureList(fieldName);
            while (list.Elements!.Count > 0)
                list.RemoveElementAt(list.Elements.Count - 1);

            foreach (var id in ids.Distinct().OrderBy(id => id))
            {
                var entry = JsonGffField.CreateStruct(BuyingRuleStructId).Struct!;
                entry.SetInt("BaseItem", GffFieldType.Int, id);
                list.InsertElement(list.Elements.Count, entry);
            }
        }

        private JsonGffField EnsureList(string fieldName)
        {
            if (Owner.GetOrNull(fieldName) is { } existing)
                return existing;

            var list = JsonGffField.CreateList();
            Owner.Add(fieldName, list);
            return list;
        }

        /// <summary>
        /// Re-derives every slot's struct id and repository position from its list index. Position
        /// must follow the id: <see cref="AddInventoryItem"/> places a new item purely from the
        /// list count, so a survivor left on its pre-removal cell would collide with the next add,
        /// and the overlap persists into the saved UTM and every placed store instance cloned from
        /// it.
        /// </summary>
        private static void Renumber(IReadOnlyList<JsonGffStruct> items)
        {
            for (var index = 0; index < items.Count; index++)
            {
                items[index].SetStructId((uint)index);
                SetInventoryPosition(items[index], index);
            }
        }

        private static void SetInventoryPosition(JsonGffStruct item, int index)
        {
            item.SetInt(
                "Repos_PosX",
                GffFieldType.Word,
                index % InventoryColumns * InventoryCellSpacing);
            item.SetInt(
                "Repos_Posy",
                GffFieldType.Word,
                index / InventoryColumns * InventoryCellSpacing);
        }

        private static int NormalizePane(int paneIndex) =>
            paneIndex is >= 0 and < InventoryPaneCount
                ? paneIndex
                : (int)MerchantInventoryCategory.Miscellaneous;

        private static void ValidatePane(int paneIndex)
        {
            if (paneIndex is < 0 or >= InventoryPaneCount)
                throw new ArgumentOutOfRangeException(nameof(paneIndex));
        }
    }
}
