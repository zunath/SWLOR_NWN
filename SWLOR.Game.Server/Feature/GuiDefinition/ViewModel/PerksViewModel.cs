using System;
using System.Collections.Generic;
using System.Diagnostics;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.PerkService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class PerksViewModel: GuiViewModelBase<PerksViewModel>
    {
        private static GuiColor RedColor = new GuiColor(255, 0, 0);
        private static GuiColor GreenColor = new GuiColor(0, 255, 0);
        private static GuiColor WhiteColor = new GuiColor(255, 255, 255);

        public bool ShowAll
        {
            get => Get<bool>();
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

        public string ResetNextAvailable
        {
            get => Get<string>();
            set => Set(value);
        }

        public string SearchText
        {
            get => Get<string>();
            set => Set(value);
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

        public bool IsInMainView
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsConfirmingUpgrade
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsConfirmingRefund
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
            IsInMainView = true;
            IsConfirmingUpgrade = false;
            IsConfirmingRefund = false;
            SelectedPerkCategoryId = 0;
            ShowAll = false;
            SearchText = string.Empty;
            BuyText = "Buy Upgrade";

            LoadCharacterDetails();
            LoadPerks();

            WatchOnClient(model => model.SelectedPerkCategoryId);
            WatchOnClient(model => model.ShowAll);
            WatchOnClient(model => model.SearchText);
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
            var sw = new Stopwatch();
            sw.Start();

            var playerId = GetObjectUUID(Player);
            var dbPlayer = DB.Get<Player>(playerId);

            _filteredPerks.Clear();
            var perkColors = new GuiBindingList<GuiColor>();
            var perkDetails = new GuiBindingList<string>();
            var perkDetailsSelected = new GuiBindingList<bool>();
            var perkList = SelectedPerkCategoryId == 0
                ? Perk.GetAllActivePerks()
                : Perk.GetActivePerksInCategory((PerkCategoryType) SelectedPerkCategoryId);

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

                if (ShowAll || !ShowAll && meetsRequirements)
                {
                    _filteredPerks.Add(type);
                    perkDetails.Add($"{detail.Name} ({playerRank} / {detail.PerkLevels.Count})");
                    perkDetailsSelected.Add(false);
                    perkColors.Add(meetsRequirements ? GreenColor : RedColor);
                }
            }


            PerkButtonColors = perkColors;
            PerkButtonTexts = perkDetails;
            PerkDetailSelected = perkDetailsSelected;

            sw.Stop();
            Console.WriteLine($"LoadPerks(): {sw.ElapsedMilliseconds}ms");
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

            var selectedDetails = string.Empty;

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
                                   $"    Price: {nextUpgrade.Price}\n" +
                                   $"{nextUpgrade.Description}\n\n";

                var requirements = new GuiBindingList<string>();
                var requirementColors = new GuiBindingList<GuiColor>();

                foreach (var req in nextUpgrade.Requirements)
                {
                    requirements.Add(req.RequirementText);

                    if (string.IsNullOrWhiteSpace(req.CheckRequirements(Player)))
                    {
                        requirementColors.Add(GreenColor);
                    }
                    else
                    {
                        requirementColors.Add(RedColor);
                        meetsRequirements = false;
                    }
                }

                if (nextUpgrade.Requirements.Count <= 0)
                {
                    requirements.Add("None");
                    requirementColors.Add(GreenColor);
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

        };

        public Action OnClickRefund() => () =>
        {

        };

        public Action OnClickConfirmUpgrade() => () =>
        {

        };

        public Action OnClickConfirmRefund() => () =>
        {

        };

        public Action OnClickCancelConfirmation() => () =>
        {

        };

    }
}
