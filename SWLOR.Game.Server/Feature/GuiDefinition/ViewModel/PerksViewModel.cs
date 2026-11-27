using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Feature.GuiDefinition.PerksWindow;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Economy;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Domain.UI.Component;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Associate;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.Game.Server.Core;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class PerksViewModel : GuiViewModelBase<PerksViewModel, GuiPayloadBase>,
        IGuiRefreshable<SkillXPRefreshEvent>,
        IGuiRefreshable<PerkResetAcquiredRefreshEvent>,
        IGuiRefreshable<PerkRefundCooldownResetRefreshEvent>,
        IPerksHost
    {
        private ICurrencyService Currency { get; }
        private IDBService DB { get; }
        private IPerkService Perk { get; }
        private PerkDetailText PerkDetailText { get; }
        private PerkListBuilder PerkListBuilder { get; }
        private PerkPurchaseService PerkPurchaseService { get; }

        private int _pages;
        private bool _initialLoadDone;

        public GuiBindingList<GuiComboEntry> PageNumbers
        {
            get => Get<GuiBindingList<GuiComboEntry>>();
            set => Set(value);
        }

        public GuiBindingList<GuiComboEntry> Categories
        {
            get => Get<GuiBindingList<GuiComboEntry>>();
            set => Set(value);
        }

        public int SelectedPage
        {
            get => Get<int>();
            set
            {
                Set(value);
                SelectedPerkIndex = -1;
                LoadPerks();
            }
        }

        public string AvailableSP
        {
            get => Get<string>();
            set => Set(value);
        }

        public string TotalSP
        {
            get => Get<string>();
            set => Set(value);
        }

        public bool IsForceAffinityVisible
        {
            get => Get<bool>();
            set => Set(value);
        }

        public string ForceAffinityHeading
        {
            get => Get<string>();
            set => Set(value);
        }

        public GuiColor ForceAffinityColor
        {
            get => Get<GuiColor>();
            set => Set(value);
        }

        public string ResetNextAvailable
        {
            get => Get<string>();
            set => Set(value);
        }

        public string SearchText
        {
            get => Get<string>();
            set
            {
                Set(value);
                ResetPerkList();
            }
        }

        public int SelectedPerkCategoryId
        {
            get => Get<int>();
            set
            {
                Set(value);
                ResetPerkList();
            }
        }

        public int SelectedSortOrderId
        {
            get => Get<int>();
            set
            {
                Set(value);
                ResetPerkList();
            }
        }

        public bool IsInMyPerksMode
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsInBeastPerksMode
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool HasBeast
        {
            get => Get<bool>();
            set => Set(value);
        }

        private int _selectedPerkIndex;
        private int SelectedPerkIndex
        {
            get => _selectedPerkIndex;
            set
            {
                if (value == -1)
                {
                    IsPerkSelected = false;
                }

                _selectedPerkIndex = value;
            }
        }

        private readonly List<PerkType> _filteredPerks;

        public GuiBindingList<GuiColor> PerkButtonColors
        {
            get => Get<GuiBindingList<GuiColor>>();
            set => Set(value);
        }

        public GuiBindingList<string> PerkButtonIcons
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<string> PerkButtonTexts
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<bool> PerkDetailSelected
        {
            get => Get<GuiBindingList<bool>>();
            set => Set(value);
        }

        public string SelectedDetails
        {
            get => Get<string>();
            set => Set(value);
        }

        public GuiBindingList<string> SelectedRequirements
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<GuiColor> SelectedRequirementColors
        {
            get => Get<GuiBindingList<GuiColor>>();
            set => Set(value);
        }

        public GuiBindingList<string> SelectedRequirementIcons
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<string> SelectedRequirementTooltips
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public string BuyText
        {
            get => Get<string>();
            set => Set(value);
        }

        public bool IsBuyEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsRefundEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsPerkSelected
        {
            get => Get<bool>();
            set => Set(value);
        }

        public GuiBindingList<string> PerkRowReqIcons
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<string> PerkRowReqTooltips
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<string> PerkRowCosts
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public string WindowTitle
        {
            get => Get<string>();
            set => Set(value);
        }

        public int SelectedStatusFilter
        {
            get => Get<int>();
            set => Set(value);
        }

        public bool IsFilterAll
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsFilterOwned
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsFilterCanBuy
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsFilterMaxed
        {
            get => Get<bool>();
            set => Set(value);
        }

        public PerksViewModel(ICurrencyService currency, IDBService db, IPerkService perk, PerkDetailText perkDetailText, PerkListBuilder perkListBuilder, PerkPurchaseService perkPurchaseService)
        {
            Currency = currency;
            DB = db;
            Perk = perk;
            PerkDetailText = perkDetailText;
            PerkListBuilder = perkListBuilder;
            PerkPurchaseService = perkPurchaseService;
            _filteredPerks = new List<PerkType>();
            PerkButtonIcons = new GuiBindingList<string>();
            PerkButtonColors = new GuiBindingList<GuiColor>();
            PerkButtonTexts = new GuiBindingList<string>();
            PerkDetailSelected = new GuiBindingList<bool>();
            SelectedRequirements = new GuiBindingList<string>();
            SelectedRequirementIcons = new GuiBindingList<string>();
            SelectedRequirementTooltips = new GuiBindingList<string>();
            PerkRowReqIcons = new GuiBindingList<string>();
            PerkRowReqTooltips = new GuiBindingList<string>();
            PerkRowCosts = new GuiBindingList<string>();
            WindowTitle = "Perks";
        }

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            IsInMyPerksMode = true;
            IsInBeastPerksMode = false;
            _initialLoadDone = false;
            SelectedPerkCategoryId = 0;
            SearchText = string.Empty;
            SelectedSortOrderId = (int)PerkSortOrder.SkillLevelAscending;
            BuyText = "Buy Upgrade";
            SelectedPage = 1;
            IsPerkSelected = false;
            IsBuyEnabled = false;
            ResetStatusFilterToAll();

            _initialLoadDone = true;
            LoadCategories();
            LoadDetails();
            LoadPerks();

            WatchOnClient(model => model.SelectedPerkCategoryId);
            WatchOnClient(model => model.SearchText);
            WatchOnClient(model => model.SelectedSortOrderId);
            WatchOnClient(model => model.SelectedPage);
        }

        private void LoadCategories() => PerkListBuilder.LoadCategories(this);

        private void LoadDetails() => PerkListBuilder.LoadDetails(this);

        private void LoadPerks() => PerkListBuilder.LoadPerks(this);

        private void ResetPerkList()
        {
            SelectedPerkIndex = -1;

            if (SelectedPage != 1)
            {
                SelectedPage = 1;
                return;
            }

            LoadPerks();
        }

        private void ResetStatusFilterToAll()
        {
            SelectedStatusFilter = 0;
            IsFilterAll = true;
            IsFilterOwned = false;
            IsFilterCanBuy = false;
            IsFilterMaxed = false;
        }

        public Action OnSelectPerk() => () =>
        {
            SelectPerkAt(NuiGetEventArrayIndex());
        };

        private void SelectPerkAt(int index)
        {
            if (index < 0 || index >= _filteredPerks.Count)
                return;

            var playerId = GetObjectUUID(Player);
            var dbPlayer = DB.Get<Player>(playerId);

            // Adjust the selected perk.
            if (SelectedPerkIndex > -1)
            {
                PerkDetailSelected[SelectedPerkIndex] = false;
            }

            SelectedPerkIndex = index;
            PerkDetailSelected[SelectedPerkIndex] = true;
            var selectedPerk = _filteredPerks[index];

            var detail = Perk.GetPerkDetails(selectedPerk);
            int unallocatedSP;
            int rank;

            // Build the strings used for the details and requirements list.
            if (IsInMyPerksMode)
            {
                rank = dbPlayer.Perks.ContainsKey(selectedPerk)
                    ? dbPlayer.Perks[selectedPerk]
                    : 0;

                unallocatedSP = dbPlayer.UnallocatedSP;
            }
            else
            {
                var dbBeast = DB.Get<Beast>(dbPlayer.ActiveBeastId);
                if (dbBeast == null)
                    return;

                rank = dbBeast.Perks.ContainsKey(selectedPerk)
                    ? dbBeast.Perks[selectedPerk]
                    : 0;

                unallocatedSP = dbBeast.UnallocatedSP;
            }

            rank = PerkListBuilder.GetDisplayPerkRank(detail, rank);
            var currentUpgrade = detail.PerkLevels.ContainsKey(rank)
                ? detail.PerkLevels[rank]
                : null;
            var nextUpgrade = detail.PerkLevels.ContainsKey(rank + 1)
                ? detail.PerkLevels[rank + 1]
                : null;

            var selectedDetails = PerkDetailText.BuildSelectedPerkDetailText(this, detail, currentUpgrade, nextUpgrade, rank);

            var (meetsRequirements, requirements, requirementColors, requirementIcons, requirementTooltips) = PerkListBuilder.BuildRequirements(this, nextUpgrade);
            SelectedRequirements = requirements;
            SelectedRequirementColors = requirementColors;
            SelectedRequirementIcons = requirementIcons;
            SelectedRequirementTooltips = requirementTooltips;

            BuyText = nextUpgrade != null
                ? $"Buy Upgrade ({nextUpgrade.Price} SP)"
                : "Buy Upgrade";
            IsBuyEnabled = nextUpgrade != null &&
                           unallocatedSP >= nextUpgrade.Price &&
                           meetsRequirements;

            SelectedDetails = selectedDetails;
            IsPerkSelected = true;
            IsRefundEnabled = (dbPlayer.DatePerkRefundAvailable == null ||
                               dbPlayer.DatePerkRefundAvailable <= Clock.UtcNow) &&
                              Currency.GetCurrency(Player, CurrencyType.PerkRefundToken) > 0 &&
                              currentUpgrade != null;
        }

        public Action OnClickBuyUpgrade() => () =>
        {
            var selectedPerk = _filteredPerks[_selectedPerkIndex];
            var playerId = GetObjectUUID(Player);
            var dbPlayer = DB.Get<Player>(playerId);
            if (!PerkPurchaseService.TryGetPerkRank(this, dbPlayer, selectedPerk, out var rank, out _))
                return;

            var detail = Perk.GetPerkDetails(selectedPerk);

            var nextUpgrade = detail.PerkLevels.ContainsKey(rank + 1)
                ? detail.PerkLevels[rank + 1]
                : null;

            ShowModal($"This upgrade will cost {nextUpgrade?.Price} SP. Are you sure you want to buy it?",
                () => PerkPurchaseService.ConfirmBuyUpgrade(this, playerId));
        };

        private void RefreshAfterUpgrade(PerkDetail detail, int newRank, int unallocatedSP)
        {
            var currentUpgrade = detail.PerkLevels.ContainsKey(newRank)
                ? detail.PerkLevels[newRank]
                : null;
            var nextUpgrade = detail.PerkLevels.ContainsKey(newRank + 1)
                ? detail.PerkLevels[newRank + 1]
                : null;
            SelectedDetails = PerkDetailText.BuildSelectedPerkDetailText(this, detail, currentUpgrade, nextUpgrade, newRank);
            PerkButtonTexts[_selectedPerkIndex] = $"{detail.Name} ({newRank}/{detail.PerkLevels.Count})";

            var (meetsRequirements, requirements, requirementColors, requirementIcons, requirementTooltips) = PerkListBuilder.BuildRequirements(this, nextUpgrade);
            var (_, chipColor, rowIcon, rowTooltip) = PerkListBuilder.GetPerkRowStatus(this, detail, newRank, unallocatedSP);

            PerkButtonColors[_selectedPerkIndex] = chipColor;
            PerkRowReqIcons[_selectedPerkIndex] = rowIcon;
            PerkRowReqTooltips[_selectedPerkIndex] = rowTooltip;
            PerkRowCosts[_selectedPerkIndex] = nextUpgrade != null ? $"{nextUpgrade.Price} SP" : string.Empty;
            SelectedRequirements = requirements;
            SelectedRequirementColors = requirementColors;
            SelectedRequirementIcons = requirementIcons;
            SelectedRequirementTooltips = requirementTooltips;
            IsBuyEnabled = nextUpgrade != null &&
                           unallocatedSP >= nextUpgrade.Price &&
                           meetsRequirements;

            BuyText = nextUpgrade != null
                ? $"Buy Upgrade ({nextUpgrade.Price} SP)"
                : "Buy Upgrade";
        }

        public Action OnClickRefund() => () =>
        {
            ShowModal($"You may only refund one perk per 1 hour (real world time). This will also consume a refund token. Are you sure you want to refund this perk?", () =>
                PerkPurchaseService.Refund(this));
        };

        public Action OnClickPreviousPage() => () =>
        {
            var newPage = SelectedPage - 1;
            if (newPage < 1)
                newPage = 1;

            SelectedPage = newPage;
        };

        public Action OnClickNextPage() => () =>
        {
            var newPage = SelectedPage + 1;
            if (newPage > _pages)
                newPage = _pages;

            SelectedPage = newPage;
        };

        public void Refresh(SkillXPRefreshEvent payload)
        {
            LoadDetails();
        }

        public void Refresh(PerkResetAcquiredRefreshEvent payload)
        {
            LoadDetails();
        }

        public void Refresh(PerkRefundCooldownResetRefreshEvent payload)
        {
            var selectedPerkIndex = SelectedPerkIndex;
            LoadDetails();

            if (selectedPerkIndex > -1)
            {
                SelectPerkAt(selectedPerkIndex);
            }
        }

        public Action OnClickMyPerks() => () =>
        {
            IsInMyPerksMode = true;
            IsInBeastPerksMode = false;
            SelectedPerkCategoryId = 0;
            ResetStatusFilterToAll();
            LoadCategories();
            LoadDetails();
            LoadPerks();
        };
        public Action OnClickBeastPerks() => () =>
        {
            IsInMyPerksMode = false;
            IsInBeastPerksMode = true;
            SelectedPerkCategoryId = 0;
            ResetStatusFilterToAll();
            LoadCategories();
            LoadDetails();
            LoadPerks();
        };

        public Action OnClickFilterAll() => () =>
        {
            ResetStatusFilterToAll();
            ResetPerkList();
        };

        public Action OnClickFilterOwned() => () =>
        {
            SelectedStatusFilter = 1;
            IsFilterAll = false;
            IsFilterOwned = true;
            IsFilterCanBuy = false;
            IsFilterMaxed = false;
            ResetPerkList();
        };

        public Action OnClickFilterCanBuy() => () =>
        {
            SelectedStatusFilter = 2;
            IsFilterAll = false;
            IsFilterOwned = false;
            IsFilterCanBuy = true;
            IsFilterMaxed = false;
            ResetPerkList();
        };

        public Action OnClickFilterMaxed() => () =>
        {
            SelectedStatusFilter = 3;
            IsFilterAll = false;
            IsFilterOwned = false;
            IsFilterCanBuy = false;
            IsFilterMaxed = true;
            ResetPerkList();
        };

        uint IPerksHost.Player => Player;
        bool IPerksHost.IsInMyPerksMode => IsInMyPerksMode;
        bool IPerksHost.IsInBeastPerksMode => IsInBeastPerksMode;
        bool IPerksHost.InitialLoadDone => _initialLoadDone;
        List<PerkType> IPerksHost.FilteredPerks => _filteredPerks;
        int IPerksHost.SelectedPerkIndex { get => SelectedPerkIndex; set => SelectedPerkIndex = value; }
        int IPerksHost.Pages { get => _pages; set => _pages = value; }
        int IPerksHost.SelectedPage => SelectedPage;
        int IPerksHost.SelectedPerkCategoryId => SelectedPerkCategoryId;
        int IPerksHost.SelectedSortOrderId => SelectedSortOrderId;
        int IPerksHost.SelectedStatusFilter => SelectedStatusFilter;
        string IPerksHost.SearchText => SearchText;
        string IPerksHost.AvailableSP { get => AvailableSP; set => AvailableSP = value; }
        string IPerksHost.TotalSP { get => TotalSP; set => TotalSP = value; }
        string IPerksHost.WindowTitle { get => WindowTitle; set => WindowTitle = value; }
        string IPerksHost.ResetNextAvailable { get => ResetNextAvailable; set => ResetNextAvailable = value; }
        bool IPerksHost.IsRefundEnabled { get => IsRefundEnabled; set => IsRefundEnabled = value; }
        bool IPerksHost.HasBeast { get => HasBeast; set => HasBeast = value; }
        bool IPerksHost.IsForceAffinityVisible { get => IsForceAffinityVisible; set => IsForceAffinityVisible = value; }
        string IPerksHost.ForceAffinityHeading { get => ForceAffinityHeading; set => ForceAffinityHeading = value; }
        GuiColor IPerksHost.ForceAffinityColor { get => ForceAffinityColor; set => ForceAffinityColor = value; }
        GuiBindingList<GuiComboEntry> IPerksHost.Categories { get => Categories; set => Categories = value; }
        GuiBindingList<GuiComboEntry> IPerksHost.PageNumbers { get => PageNumbers; set => PageNumbers = value; }
        GuiBindingList<GuiColor> IPerksHost.PerkButtonColors { get => PerkButtonColors; set => PerkButtonColors = value; }
        GuiBindingList<string> IPerksHost.PerkButtonIcons { get => PerkButtonIcons; set => PerkButtonIcons = value; }
        GuiBindingList<string> IPerksHost.PerkButtonTexts { get => PerkButtonTexts; set => PerkButtonTexts = value; }
        GuiBindingList<bool> IPerksHost.PerkDetailSelected { get => PerkDetailSelected; set => PerkDetailSelected = value; }
        GuiBindingList<string> IPerksHost.PerkRowReqIcons { get => PerkRowReqIcons; set => PerkRowReqIcons = value; }
        GuiBindingList<string> IPerksHost.PerkRowReqTooltips { get => PerkRowReqTooltips; set => PerkRowReqTooltips = value; }
        GuiBindingList<string> IPerksHost.PerkRowCosts { get => PerkRowCosts; set => PerkRowCosts = value; }
        void IPerksHost.SelectPerkAt(int index) => SelectPerkAt(index);
        void IPerksHost.LoadDetails() => LoadDetails();
        void IPerksHost.LoadPerks() => LoadPerks();
        void IPerksHost.RefreshAfterUpgrade(PerkDetail detail, int newRank, int unallocatedSP) => RefreshAfterUpgrade(detail, newRank, unallocatedSP);
    }
}
