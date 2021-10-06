using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.PerkService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class PerksViewModel : GuiViewModelBase<PerksViewModel>
    {
        private static readonly GuiColor _redColor = new GuiColor(255, 0, 0);
        private static readonly GuiColor _greenColor = new GuiColor(0, 255, 0);

        private const int ItemsPerPage = 30;
        private int _pages;
        private bool _initialLoadDone;

        public GuiBindingList<GuiComboEntry> PageNumbers
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
                SelectedPerkIndex = -1;
                LoadPerks();
            }
        }

        public int SelectedPerkCategoryId
        {
            get => Get<int>();
            set
            {
                Set(value);
                SelectedPerkIndex = -1;
                LoadPerks();
            }
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

        public PerksViewModel()
        {
            _filteredPerks = new List<PerkType>();
            PerkButtonColors = new GuiBindingList<GuiColor>();
            PerkButtonTexts = new GuiBindingList<string>();
            PerkDetailSelected = new GuiBindingList<bool>();
            SelectedRequirements = new GuiBindingList<string>();
        }

        public Action OnLoadWindow() => () =>
        {
            _initialLoadDone = false;
            SelectedPerkCategoryId = 0;
            SearchText = string.Empty;
            BuyText = "Buy Upgrade";
            SelectedPage = 1;
            IsPerkSelected = false;
            IsBuyEnabled = false;

            WatchOnClient(model => model.SelectedPerkCategoryId);
            WatchOnClient(model => model.SearchText);
            WatchOnClient(model => model.SelectedPage);
            
            _initialLoadDone = true;
            LoadCharacterDetails();
            LoadPerks();
        };

        private void LoadCharacterDetails()
        {
            var playerId = GetObjectUUID(Player);
            var dbPlayer = DB.Get<Player>(playerId);
            var now = DateTime.UtcNow;
            var dateRefundAvailable = dbPlayer.DatePerkRefundAvailable ?? now;
            var isRefundAvailable = dateRefundAvailable <= now;
            var dateRefundAvailableText = isRefundAvailable
                ? "Now"
                : Time.GetTimeToWaitLongIntervals(now, dateRefundAvailable, true);

            AvailableSP = $"Available SP: {dbPlayer.UnallocatedSP}";
            TotalSP = $"Total SP: {dbPlayer.TotalSPAcquired} / {Skill.SkillCap}";
            ResetNextAvailable = $"Reset Available: {dateRefundAvailableText}";
            IsRefundEnabled = isRefundAvailable;
        }

        private void LoadPerks()
        {
            if (!_initialLoadDone) return;

            var playerId = GetObjectUUID(Player);
            var dbPlayer = DB.Get<Player>(playerId);

            _filteredPerks.Clear();

            var sw = new Stopwatch();
            sw.Start();
            
            var perkColors = new GuiBindingList<GuiColor>();
            var perkDetails = new GuiBindingList<string>();
            var perkDetailsSelected = new GuiBindingList<bool>();
            var pageNumbers = new GuiBindingList<GuiComboEntry>();

            var perkList = SelectedPerkCategoryId == 0
                ? Perk.GetAllActivePerks()
                : Perk.GetActivePerksInCategory((PerkCategoryType)SelectedPerkCategoryId);


            // Filter down to just perks with a name partially matching the search text
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                perkList = perkList.Where(x => x.Value.Name.ToLower().Contains(SearchText.ToLower()))
                    .ToDictionary(x => x.Key, y => y.Value);
            }

            _pages = perkList.Count / ItemsPerPage + (perkList.Count % ItemsPerPage == 0 ? 0 : 1);

            for (var x = 1; x <= _pages; x++)
            {
                pageNumbers.Add(new GuiComboEntry($"Page {x}", x));
            }

            // Paginate the results
            perkList = perkList
                .Skip((SelectedPage - 1) * ItemsPerPage)
                .Take(ItemsPerPage)
                .ToDictionary(x => x.Key, y => y.Value);

            foreach (var (type, detail) in perkList)
            {
                var playerRank = dbPlayer.Perks.ContainsKey(type)
                    ? dbPlayer.Perks[type]
                    : 0;
                var nextUpgrade = detail.PerkLevels.ContainsKey(playerRank + 1)
                    ? detail.PerkLevels[playerRank + 1]
                    : null;
                var meetsRequirements = true;

                if (nextUpgrade != null)
                {
                    foreach (var req in nextUpgrade.Requirements)
                    {
                        if (!string.IsNullOrWhiteSpace(req.CheckRequirements(Player)))
                        {
                            meetsRequirements = false;
                            break;
                        }
                    }
                }

                _filteredPerks.Add(type);
                perkDetails.Add($"{detail.Name} ({playerRank} / {detail.PerkLevels.Count})");
                perkDetailsSelected.Add(false);
                perkColors.Add(meetsRequirements ? _greenColor : _redColor);
            }

            PerkButtonColors = perkColors;
            PerkButtonTexts = perkDetails;
            PerkDetailSelected = perkDetailsSelected;
            PageNumbers = pageNumbers;

            sw.Stop();
            Console.WriteLine($"LoadPerks: {sw.ElapsedMilliseconds}ms");
        }

        public Action OnSelectPerk() => () =>
        {
            var index = NuiGetEventArrayIndex();

            // Adjust the selected perk.
            if (SelectedPerkIndex > -1)
            {
                PerkDetailSelected[SelectedPerkIndex] = false;
            }

            SelectedPerkIndex = index;
            PerkDetailSelected[SelectedPerkIndex] = true;
            var selectedPerk = _filteredPerks[index];

            // Build the strings used for the details and requirements list.
            var playerId = GetObjectUUID(Player);
            var dbPlayer = DB.Get<Player>(playerId);
            var detail = Perk.GetPerkDetails(selectedPerk);
            var playerRank = dbPlayer.Perks.ContainsKey(selectedPerk)
                ? dbPlayer.Perks[selectedPerk]
                : 0;
            var currentUpgrade = detail.PerkLevels.ContainsKey(playerRank)
                ? detail.PerkLevels[playerRank]
                : null;
            var nextUpgrade = detail.PerkLevels.ContainsKey(playerRank + 1)
                ? detail.PerkLevels[playerRank + 1]
                : null;
            var meetsRequirements = true;

            var selectedDetails = detail.Name + "\n\n";

            // Perk Description
            if (detail.Description != null)
            {
                selectedDetails += "Description: \n" + detail.Description + "\n\n";
            }

            if (currentUpgrade != null)
            {
                selectedDetails += "Current Upgrade: \n" + currentUpgrade.Description + "\n\n";
            }

            if (nextUpgrade != null)
            {
                selectedDetails += "Next Upgrade: \n" +
                                   $"    Price: {nextUpgrade.Price} SP\n" +
                                   $"{nextUpgrade.Description}\n\n";

                var requirements = new GuiBindingList<string>();
                var requirementColors = new GuiBindingList<GuiColor>();

                foreach (var req in nextUpgrade.Requirements)
                {
                    requirements.Add(req.RequirementText);

                    if (string.IsNullOrWhiteSpace(req.CheckRequirements(Player)))
                    {
                        requirementColors.Add(_greenColor);
                    }
                    else
                    {
                        requirementColors.Add(_redColor);
                        meetsRequirements = false;
                    }
                }

                if (nextUpgrade.Requirements.Count <= 0)
                {
                    requirements.Add("None");
                    requirementColors.Add(_greenColor);
                }

                SelectedRequirements = requirements;
                SelectedRequirementColors = requirementColors;
            }

            BuyText = nextUpgrade != null
                ? $"Buy Upgrade ({nextUpgrade.Price} SP)"
                : "Buy Upgrade";
            IsBuyEnabled = nextUpgrade != null &&
                           dbPlayer.UnallocatedSP >= nextUpgrade.Price &&
                           meetsRequirements;

            SelectedDetails = selectedDetails;
            IsPerkSelected = true;
        };

        public Action OnClickBuyUpgrade() => () =>
        {
            var playerId = GetObjectUUID(Player);
            var dbPlayer = DB.Get<Player>(playerId);
            var selectedPerk = _filteredPerks[_selectedPerkIndex];
            var detail = Perk.GetPerkDetails(selectedPerk);
            var playerRank = dbPlayer.Perks.ContainsKey(selectedPerk)
                ? dbPlayer.Perks[selectedPerk]
                : 0;
            var nextUpgrade = detail.PerkLevels.ContainsKey(playerRank + 1)
                ? detail.PerkLevels[playerRank + 1]
                : null;

            ShowModal(
                $"This upgrade will cost {nextUpgrade?.Price} SP. Are you sure you want to buy it?", 
                () =>
                {
                    Console.WriteLine("buy sumpin");
                });
        };

        public Action OnClickRefund() => () =>
        {

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
    }
}
