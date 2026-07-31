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
            item.SetInt("Repos_PosX", GffFieldType.Word, index % 10);
            item.SetInt("Repos_Posy", GffFieldType.Word, index / 10);
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

            ReplaceBuyingRules(newField, selected);
            ReplaceBuyingRules(oldField, Array.Empty<int>());
        }

        public void SetBuyingRule(bool buyOnlySelected, int baseItem, bool selected)
        {
            var fieldName = buyOnlySelected ? WillOnlyBuyField : WillNotBuyField;
            var otherField = buyOnlySelected ? WillNotBuyField : WillOnlyBuyField;
            ReplaceBuyingRules(otherField, Array.Empty<int>());

            var ids = BuyingRuleIds(buyOnlySelected).ToHashSet();
            if (selected)
                ids.Add(baseItem);
            else
                ids.Remove(baseItem);
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

        private static void Renumber(IReadOnlyList<JsonGffStruct> items)
        {
            for (var index = 0; index < items.Count; index++)
                items[index].SetStructId((uint)index);
        }

        private static void ValidatePane(int paneIndex)
        {
            if (paneIndex is < 0 or >= InventoryPaneCount)
                throw new ArgumentOutOfRangeException(nameof(paneIndex));
        }
    }
}
