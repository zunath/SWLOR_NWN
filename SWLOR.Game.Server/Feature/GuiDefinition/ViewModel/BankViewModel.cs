using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    internal class BankViewModel: GuiViewModelBase<BankViewModel, GuiPayloadBase>
    {
        private static readonly GuiColor _storageFreeColor = new(90, 150, 95);
        private static readonly GuiColor _storageFullColor = new(200, 100, 70);

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

        public GuiColor StorageColor
        {
            get => Get<GuiColor>();
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
            StorageColor = Bank.IsFull(itemCount) ? _storageFullColor : _storageFreeColor;
        }

        private void Search()
        {
            var playerId = GetObjectUUID(Player);

            _itemIds.Clear();
            var items = Bank.SearchItems(playerId, SearchText);
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
