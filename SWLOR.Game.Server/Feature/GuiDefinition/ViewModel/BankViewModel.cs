using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Feature.GuiDefinition.Component;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    internal class BankViewModel: GuiViewModelBase<BankViewModel, GuiPayloadBase>
    {
        /// <summary>
        /// When a bank placeable is used, display this UI view.
        /// </summary>
        [NWNEventHandler(ScriptName.OnOpenBank)]
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

        // One row DTO per bank item, replacing the two hand-synced parallel
        // GuiBindingList instances Search used to build in lockstep.
        private sealed class ItemEntry
        {
            public string Id { get; }
            public string Resref { get; }
            public string Name { get; }

            public ItemEntry(string id, string resref, string name)
            {
                Id = id;
                Resref = resref;
                Name = name;
            }
        }

        private static readonly GuiTableSource<BankViewModel, ItemEntry> ItemsTable =
            new GuiTableSource<BankViewModel, ItemEntry>()
                .Column((m, v) => m.ItemResrefs = v, r => r.Resref)
                .Column((m, v) => m.ItemNames = v, r => r.Name);

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
            var itemCount = Bank.GetItemCount(playerId);

            _itemCount = itemCount;
            ItemCountText = Bank.GetItemCountText(itemCount);
            StoragePercentage = Bank.GetStoragePercentage(itemCount);
            IsDepositEnabled = !Bank.IsFull(itemCount);
        }

        private void Search()
        {
            var playerId = GetObjectUUID(Player);

            var items = Bank.SearchItems(playerId, SearchText);
            var rows = new List<ItemEntry>();

            foreach (var item in items)
            {
                rows.Add(new ItemEntry(item.Id, item.IconResref, $"{item.Quantity}x {item.Name}"));
            }

            _itemIds.Clear();
            foreach (var row in rows)
                _itemIds.Add(row.Id);

            ItemsTable.Refresh(this, rows);
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

            Bank.WithdrawItem(Player, itemId);
            _itemIds.RemoveAt(index);
            ItemNames.RemoveAt(index);
            ItemResrefs.RemoveAt(index);

            RefreshItemCount();
        };

        public Action OnClickDeposit() => () =>
        {
            Targeting.EnterTargetingMode(Player, ObjectType.Item, "Please click on an item within your inventory.",
                item =>
            {
                var failure = Bank.GetDepositFailure(Player, item);
                if (!string.IsNullOrWhiteSpace(failure))
                {
                    SendMessageToPC(Player, ColorToken.Red(failure));
                    return;
                }

                var dbItem = Bank.DepositItem(Player, item);

                _itemIds.Add(dbItem.Id);
                ItemNames.Add($"{dbItem.Quantity}x {dbItem.Name}");
                ItemResrefs.Add(dbItem.IconResref);

                RefreshItemCount();
            });
        };
    }
}
