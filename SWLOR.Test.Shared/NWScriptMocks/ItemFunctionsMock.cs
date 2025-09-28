using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Test.Shared.NWScriptMocks
{
    public partial class NWScriptServiceMock
    {
        // Mock data storage for items
        private readonly Dictionary<uint, ItemData> _itemData = new();
        private readonly Dictionary<uint, List<uint>> _inventoryItems = new();
        private readonly Dictionary<uint, uint> _itemIterators = new();
        private readonly Dictionary<InventorySlotType, uint> _equippedItems = new();
        private int _moduleItemAcquiredStackSize = 0;
        private uint _moduleItemAcquired = OBJECT_INVALID;
        private uint _moduleItemAcquiredFrom = OBJECT_INVALID;
        private uint _spellCastItem = OBJECT_INVALID;
        private uint _itemActivated = OBJECT_INVALID;
        private uint _itemActivator = OBJECT_INVALID;
        private Location _itemActivatedTargetLocation = new Location(0);
        private uint _itemActivatedTarget = OBJECT_INVALID;

        private class ItemData
        {
            public Dictionary<ItemModelColorType, Dictionary<int, int>> Appearance { get; set; } = new();
            public int StackSize { get; set; } = 1;
            public int Charges { get; set; } = 0;
            public bool HiddenWhenEquipped { get; set; } = false;
            public bool InfiniteFlag { get; set; } = false;
            public bool StolenFlag { get; set; } = false;
            public bool PickpocketableFlag { get; set; } = true;
            public bool DroppableFlag { get; set; } = true;
            public bool UseableFlag { get; set; } = true;
            public bool IsRanged { get; set; } = false;
            public int ACValue { get; set; } = 0;
            public BaseItemType BaseItemType { get; set; } = BaseItemType.Invalid;
            public HashSet<ItemPropertyType> ItemProperties { get; set; } = new();
            public bool Identified { get; set; } = false;
            public int GoldPieceValue { get; set; } = 0;
        }

        public int GetItemAppearance(uint oItem, ItemModelColorType nType, int nIndex) => 
            _itemData.GetValueOrDefault(oItem, new ItemData())
                .Appearance.GetValueOrDefault(nType, new Dictionary<int, int>())
                .GetValueOrDefault(nIndex, 0);

        public int GetItemStackSize(uint oItem) => 
            _itemData.GetValueOrDefault(oItem, new ItemData()).StackSize;

        public void SetItemStackSize(uint oItem, int nSize) 
        {
            var data = GetOrCreateItemData(oItem);
            data.StackSize = nSize;
        }

        public int GetItemCharges(uint oItem) => 
            _itemData.GetValueOrDefault(oItem, new ItemData()).Charges;

        public void SetItemCharges(uint oItem, int nCharges) 
        {
            var data = GetOrCreateItemData(oItem);
            data.Charges = nCharges;
        }

        public uint CopyItem(uint oItem, uint oTargetInventory = OBJECT_INVALID, bool bCopyVars = false) 
        {
            var newItem = (uint)(_itemData.Count + 6000);
            if (_itemData.ContainsKey(oItem))
            {
                _itemData[newItem] = _itemData[oItem];
            }
            return newItem;
        }

        public int GetModuleItemAcquiredStackSize() => _moduleItemAcquiredStackSize;

        public int GetNumStackedItems(uint oItem) => 
            _itemData.GetValueOrDefault(oItem, new ItemData()).StackSize;

        public void SetHiddenWhenEquipped(uint oItem, bool nValue) 
        {
            var data = GetOrCreateItemData(oItem);
            data.HiddenWhenEquipped = nValue;
        }

        public int GetHiddenWhenEquipped(uint oItem) => 
            _itemData.GetValueOrDefault(oItem, new ItemData()).HiddenWhenEquipped ? 1 : 0;

        public int GetInfiniteFlag(uint oItem) => 
            _itemData.GetValueOrDefault(oItem, new ItemData()).InfiniteFlag ? 1 : 0;

        public void SetInfiniteFlag(uint oItem, bool bInfinite = true) 
        {
            var data = GetOrCreateItemData(oItem);
            data.InfiniteFlag = bInfinite;
        }

        public void SetStolenFlag(uint oItem, bool nStolen) 
        {
            var data = GetOrCreateItemData(oItem);
            data.StolenFlag = nStolen;
        }

        public bool GetPickpocketableFlag(uint oItem) => 
            _itemData.GetValueOrDefault(oItem, new ItemData()).PickpocketableFlag;

        public void SetPickpocketableFlag(uint oItem, bool bPickpocketable) 
        {
            var data = GetOrCreateItemData(oItem);
            data.PickpocketableFlag = bPickpocketable;
        }

        public void SetDroppableFlag(uint oItem, bool bDroppable) 
        {
            var data = GetOrCreateItemData(oItem);
            data.DroppableFlag = bDroppable;
        }

        public bool GetDroppableFlag(uint oItem) => 
            _itemData.GetValueOrDefault(oItem, new ItemData()).DroppableFlag;

        public bool GetUseableFlag(uint oObject = OBJECT_INVALID) => 
            _itemData.GetValueOrDefault(oObject, new ItemData()).UseableFlag;

        public bool GetStolenFlag(uint oStolen) => 
            _itemData.GetValueOrDefault(oStolen, new ItemData()).StolenFlag;

        public bool GetWeaponRanged(uint oItem) => 
            _itemData.GetValueOrDefault(oItem, new ItemData()).IsRanged;

        public uint GetSpellCastItem() => _spellCastItem;

        public uint GetItemActivated() => _itemActivated;

        public uint GetItemActivator() => _itemActivator;

        public Location GetItemActivatedTargetLocation() => _itemActivatedTargetLocation;

        public uint GetItemActivatedTarget() => _itemActivatedTarget;

        public int GetItemACValue(uint oItem) => 
            _itemData.GetValueOrDefault(oItem, new ItemData()).ACValue;

        public BaseItemType GetBaseItemType(uint oItem) => 
            _itemData.GetValueOrDefault(oItem, new ItemData()).BaseItemType;

        public bool GetItemHasItemProperty(uint oItem, ItemPropertyType nProperty) => 
            _itemData.GetValueOrDefault(oItem, new ItemData()).ItemProperties.Contains(nProperty);

        public uint GetFirstItemInInventory(uint oTarget = OBJECT_INVALID) 
        {
            _itemIterators[oTarget] = OBJECT_INVALID;
            if (_inventoryItems.ContainsKey(oTarget) && _inventoryItems[oTarget].Count > 0)
            {
                _itemIterators[oTarget] = _inventoryItems[oTarget][0];
            }
            return _itemIterators[oTarget];
        }

        public uint GetNextItemInInventory(uint oTarget = OBJECT_INVALID) 
        {
            if (_itemIterators[oTarget] == OBJECT_INVALID || !_inventoryItems.ContainsKey(oTarget))
                return OBJECT_INVALID;
            
            var items = _inventoryItems[oTarget];
            var currentIndex = items.IndexOf(_itemIterators[oTarget]);
            if (currentIndex >= 0 && currentIndex < items.Count - 1)
            {
                _itemIterators[oTarget] = items[currentIndex + 1];
                return _itemIterators[oTarget];
            }
            
            _itemIterators[oTarget] = OBJECT_INVALID;
            return OBJECT_INVALID;
        }

        public bool GetIdentified(uint oItem) => 
            _itemData.GetValueOrDefault(oItem, new ItemData()).Identified;

        public void SetIdentified(uint oItem, bool bIdentified) 
        {
            var data = GetOrCreateItemData(oItem);
            data.Identified = bIdentified;
        }

        public int GetGoldPieceValue(uint oItem) => 
            _itemData.GetValueOrDefault(oItem, new ItemData()).GoldPieceValue;

        public uint GetModuleItemAcquired() => _moduleItemAcquired;

        public uint GetModuleItemAcquiredFrom() => _moduleItemAcquiredFrom;

        public uint GetItemInSlot(InventorySlotType nInventorySlot, uint oCreature = OBJECT_INVALID) => 
            _equippedItems.GetValueOrDefault(nInventorySlot, OBJECT_INVALID);

        public bool GetBaseItemFitsInInventory(BaseItemType baseItemType, uint target) 
        {
            // Mock implementation - assume all items fit
            return true;
        }

        private ItemData GetOrCreateItemData(uint oItem)
        {
            if (!_itemData.ContainsKey(oItem))
                _itemData[oItem] = new ItemData();
            return _itemData[oItem];
        }

        // Helper methods for testing



        public void AddItemProperty(uint oItem, ItemPropertyType nProperty) 
        {
            var data = GetOrCreateItemData(oItem);
            data.ItemProperties.Add(nProperty);
        }







    }
}
