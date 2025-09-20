using System;
using System.Collections.Generic;

using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;

using SWLOR.Game.Server.Service.GuiService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Core.Event;
using SWLOR.Shared.Core.Service;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    internal class BankViewModel: GuiViewModelBase<BankViewModel, GuiPayloadBase>
    {
        /// <summary>
        /// When a bank placeable is used, display this UI view.
        /// </summary>
        [ScriptHandler(ScriptName.OnOpenBank)]
        public static void ShowBank()
        {
            var player = GetLastUsedBy();

            if (!GetIsPC(player) || GetIsDM(player))
            {
                SendMessageToPC(player, "Only players may use this.");
                return;
            }

            Gui.TogglePlayerWindow(player, GuiWindowType.Bank, null, OBJECT_SELF);
        }

        private readonly List<string> _itemIds = new();

        public float StoragePercentage
        {
            get => Get<float>();
            set => Set(value);
        }

        private long _itemCount;

        public string ItemCountText
        {
            get => Get<string>();
            set => Set(value);
        }

        public string SearchText
        {
            get => Get<string>();
            set => Set(value);
        }

        public bool IsDepositEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }

        public GuiBindingList<string> ItemResrefs
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<string> ItemNames
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        private void RefreshItemCount()
        {
            var playerId = GetObjectUUID(Player);
            var bank = TetherObject;
            var storageId = GetLocalString(bank, "STORAGE_ID");
            var maxItems = GetLocalInt(bank, "STORAGE_ITEM_LIMIT");

            var itemCount = DB.SearchCount(new DBQuery<InventoryItem>()
                .AddFieldSearch(nameof(InventoryItem.StorageId), storageId, false)
                .AddFieldSearch(nameof(InventoryItem.PlayerId), playerId, false));

            _itemCount = itemCount;
            ItemCountText = $"{itemCount} / {maxItems} Items";
            StoragePercentage = (float)itemCount / (float)maxItems;

            // If a city's level has downgraded it's possible for the storage percentage to be higher than 100%.
            // Clamp this down so as not to confuse the UI progress bar.
            if (StoragePercentage > 1f)
                StoragePercentage = 1f;

            IsDepositEnabled = _itemCount < maxItems;
        }

        private void Search()
        {
            var playerId = GetObjectUUID(Player);
            var bank = TetherObject;
            var storageId = GetLocalString(bank, "STORAGE_ID");

            var query = new DBQuery<InventoryItem>()
                .AddFieldSearch(nameof(InventoryItem.StorageId), storageId, false)
                .AddFieldSearch(nameof(InventoryItem.PlayerId), playerId, false);

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                query.AddFieldSearch(nameof(InventoryItem.Name), SearchText, true);
            }

            _itemIds.Clear();
            var items = DB.Search(query);
            var itemResrefs = new GuiBindingList<string>();
            var itemNames = new GuiBindingList<string>();

            foreach (var item in items)
            {
                _itemIds.Add(item.Id);
                itemResrefs.Add(item.IconResref);
                itemNames.Add($"{item.Quantity}x {item.Name}");
            }

            ItemResrefs = itemResrefs;
            ItemNames = itemNames;
        }

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            SearchText = string.Empty;

            RefreshItemCount();
            Search();

            WatchOnClient(model => model.SearchText);
        }

        public Action OnClickClearSearch() => () =>
        {
            SearchText = string.Empty;
            Search();
        };

        public Action OnClickSearch() => () =>
        {
            Search();
        };

        public Action OnClickWithdraw() => () =>
        {
            var index = NuiGetEventArrayIndex();
            var itemId = _itemIds[index];
            var dbItem = DB.Get<InventoryItem>(itemId);

            var item = ObjectPlugin.Deserialize(dbItem.Data);
            ObjectPlugin.AcquireItem(Player, item);

            DB.Delete<InventoryItem>(dbItem.Id);

            _itemIds.RemoveAt(index);
            ItemNames.RemoveAt(index);
            ItemResrefs.RemoveAt(index);

            if (Item.IsLegacyItem(item))
            {
                for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
                {
                    RemoveItemProperty(item, ip);
                }
            }

            RefreshItemCount();
        };

        public Action OnClickDeposit() => () =>
        {
            Targeting.EnterTargetingMode(Player, ObjectType.Item, "Please click on an item within your inventory.",
                item =>
            {
                var canStore = Item.CanBePersistentlyStored(Player, item);
                if (!string.IsNullOrWhiteSpace(canStore))
                {
                    SendMessageToPC(Player, ColorToken.Red(canStore));
                    return;
                }

                var playerId = GetObjectUUID(Player);
                var bank = TetherObject;
                var storageId = GetLocalString(bank, "STORAGE_ID");
                var maxItems = GetLocalInt(bank, "STORAGE_ITEM_LIMIT");
                if (_itemCount >= maxItems)
                {
                    SendMessageToPC(Player, ColorToken.Red("Your bank is full."));
                    return;
                }

                var dbItem = new InventoryItem
                {
                    StorageId = storageId,
                    PlayerId = playerId,
                    Name = GetName(item),
                    Tag = GetTag(item),
                    Resref = GetResRef(item),
                    Quantity = GetItemStackSize(item),
                    Data = ObjectPlugin.Serialize(item),
                    IconResref = Item.GetIconResref(item)
                };

                DB.Set(dbItem);

                DestroyObject(item);

                _itemIds.Add(dbItem.Id);
                ItemNames.Add($"{dbItem.Quantity}x {dbItem.Name}");
                ItemResrefs.Add(dbItem.IconResref);

                RefreshItemCount();
            });
        };
    }
}
