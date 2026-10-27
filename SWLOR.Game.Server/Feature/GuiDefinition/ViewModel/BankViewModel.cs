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

        private bool _skipPaginationSearch;

        public GuiBindingList<GuiComboEntry> PageNumbers
        {
            get => Get<GuiBindingList<GuiComboEntry>>();
            set => Set(value);
        }

        public int SelectedPageIndex
        {
            get => Get<int>();
            set
            {
                Set(value);

                if (!_skipPaginationSearch)
                    Search();
            }
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
            LoadPage(SelectedPageIndex);
        }

        /// <summary>
        /// Loads a page of the bank. A page past the end shows the last page, which holds the newest deposits.
        /// </summary>
        private void LoadPage(int pageIndex)
        {
            var playerId = GetObjectUUID(Player);
            var page = Bank.SearchItems(playerId, SearchText, pageIndex);

            _skipPaginationSearch = true;
            PageNumbers = page.PageNumbers;
            SelectedPageIndex = page.PageIndex;
            _skipPaginationSearch = false;

            _itemIds.Clear();
            var itemResrefs = new GuiBindingList<string>();
            var itemNames = new GuiBindingList<string>();

            foreach (var item in page.Items)
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
            _skipPaginationSearch = true;
            SearchText = string.Empty;
            PageNumbers = new GuiBindingList<GuiComboEntry>();
            SelectedPageIndex = 0;
            _skipPaginationSearch = false;

            RefreshItemCount();
            LoadPage(0);

            WatchOnClient(model => model.SearchText);
            WatchOnClient(model => model.SelectedPageIndex);
        }

        public Action OnClickClearSearch() => () =>
        {
            SearchText = string.Empty;
            LoadPage(0);
        };

        public Action OnClickSearch() => () =>
        {
            LoadPage(0);
        };

        public Action OnClickPreviousPage() => () =>
        {
            LoadPage(Math.Max(0, SelectedPageIndex - 1));
        };

        public Action OnClickNextPage() => () =>
        {
            LoadPage(Math.Min(PageNumbers.Count - 1, SelectedPageIndex + 1));
        };

        public Action OnClickWithdraw() => () =>
        {
            var index = NuiGetEventArrayIndex();
            var itemId = _itemIds[index];

            var failure = Bank.WithdrawItem(Player, itemId);
            if (!string.IsNullOrWhiteSpace(failure))
            {
                SendMessageToPC(Player, ColorToken.Red(failure));
                return;
            }

            RefreshItemCount();
            // Reload so the page refills from the next page, or moves back a page when it empties.
            LoadPage(SelectedPageIndex);
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

                Bank.DepositItem(Player, item);

                RefreshItemCount();
                // New deposits sort last, so show the last page where the player can see the item.
                LoadPage(int.MaxValue);
            });
        };
    }
}
