using Microsoft.Extensions.DependencyInjection;
using SWLOR.NWN.API.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Abstractions.Enums;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Domain.Entities;
using SWLOR.Shared.Domain.Inventory.Contracts;
using SWLOR.Shared.Domain.Perk.Contracts;
using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Events.Inventory;
using SWLOR.Shared.UI.Contracts;
using SWLOR.Shared.UI.Model;
using SWLOR.Shared.UI.Service;
using SWLOR.Shared.Domain.Repositories;

namespace SWLOR.Component.Inventory.UI.ViewModel
{
    internal class BankViewModel: GuiViewModelBase<BankViewModel, IGuiPayload>
    {
        private readonly IDatabaseService _db;
        private readonly IServiceProvider _serviceProvider;
        private readonly IInventoryItemRepository _inventoryItemRepository;
        
        // Lazy-loaded services to break circular dependencies
        private readonly Lazy<IItemService> _itemService;
        private readonly Lazy<IPerkService> _perkService;
        private readonly Lazy<ITargetingService> _targetingService;
        private readonly Lazy<IObjectPluginService> _objectPlugin;

        public BankViewModel(IGuiService guiService, IDatabaseService db, IServiceProvider serviceProvider, IInventoryItemRepository inventoryItemRepository) : base(guiService)
        {
            _db = db;
            _serviceProvider = serviceProvider;
            _inventoryItemRepository = inventoryItemRepository;
            
            // Initialize lazy services
            _itemService = new Lazy<IItemService>(() => _serviceProvider.GetRequiredService<IItemService>());
            _perkService = new Lazy<IPerkService>(() => _serviceProvider.GetRequiredService<IPerkService>());
            _targetingService = new Lazy<ITargetingService>(() => _serviceProvider.GetRequiredService<ITargetingService>());
            _objectPlugin = new Lazy<IObjectPluginService>(() => _serviceProvider.GetRequiredService<IObjectPluginService>());
        }

        // Lazy-loaded services to break circular dependencies
        private IItemService ItemService => _itemService.Value;
        private IPerkService PerkService => _perkService.Value;
        private ITargetingService TargetingService => _targetingService.Value;
        private IObjectPluginService ObjectPlugin => _objectPlugin.Value;
        
        /// <summary>
        /// When a bank placeable is used, display this UI view.
        /// </summary>
        [ScriptHandler<OnOpenBank>]
        public void ShowBank()
        {
            var player = GetLastUsedBy();

            if (!GetIsPC(player) || GetIsDM(player))
            {
                SendMessageToPC(player, "Only players may use this.");
                return;
            }

            _guiService.TogglePlayerWindow(player, GuiWindowType.Bank, null, OBJECT_SELF);
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

            var itemCount = _inventoryItemRepository.GetCountByStorageIdAndPlayerId(storageId, playerId);

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

            var allItems = _inventoryItemRepository.GetByStorageIdAndPlayerId(storageId, playerId);

            var filteredItems = allItems;
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                filteredItems = allItems.Where(item => item.Name.ToLower().Contains(SearchText.ToLower()));
            }

            _itemIds.Clear();
            var items = filteredItems;
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

        protected override void Initialize(IGuiPayload initialPayload)
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
            var dbItem = _db.Get<InventoryItem>(itemId);

            var item = ObjectPlugin.Deserialize(dbItem.Data);
            ObjectPlugin.AcquireItem(Player, item);

            _db.Delete<InventoryItem>(dbItem.Id);

            _itemIds.RemoveAt(index);
            ItemNames.RemoveAt(index);
            ItemResrefs.RemoveAt(index);

            if (ItemService.IsLegacyItem(item))
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
            TargetingService.EnterTargetingMode(Player, ObjectType.Item, "Please click on an item within your inventory.",
                item =>
            {
                var canStore = ItemService.CanBePersistentlyStored(Player, item);
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
                    IconResref = ItemService.GetIconResref(item)
                };

                _db.Set(dbItem);

                DestroyObject(item);

                _itemIds.Add(dbItem.Id);
                ItemNames.Add($"{dbItem.Quantity}x {dbItem.Name}");
                ItemResrefs.Add(dbItem.IconResref);

                RefreshItemCount();
            });
        };
    }
}
