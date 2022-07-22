using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.StatusEffectService;
using Skill = SWLOR.Game.Server.Service.Skill;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class PerksViewModel : GuiViewModelBase<PerksViewModel, GuiPayloadBase>,
        IGuiRefreshable<SkillXPRefreshEvent>,
        IGuiRefreshable<PerkResetAcquiredRefreshEvent>
    {
        private static readonly GuiColor _red = new GuiColor(255, 0, 0);
        private static readonly GuiColor _green = new GuiColor(0, 255, 0);

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
            PerkButtonIcons = new GuiBindingList<string>();
            PerkButtonColors = new GuiBindingList<GuiColor>();
            PerkButtonTexts = new GuiBindingList<string>();
            PerkDetailSelected = new GuiBindingList<bool>();
            SelectedRequirements = new GuiBindingList<string>();
        }

        protected override void Initialize(GuiPayloadBase initialPayload)
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
        }

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
            ResetNextAvailable = $"Reset Available: {dateRefundAvailableText} [# Available: {dbPlayer.NumberPerkResetsAvailable}]";
            IsRefundEnabled = false;
        }

        private void LoadPerks()
        {
            if (!_initialLoadDone) return;

            var playerId = GetObjectUUID(Player);
            var dbPlayer = DB.Get<Player>(playerId);

            _filteredPerks.Clear();
            
            var perkButtonColors = new GuiBindingList<GuiColor>();
            var perkButtonIcons = new GuiBindingList<string>();
            var perkButtonTexts = new GuiBindingList<string>();
            var perkDetailSelected = new GuiBindingList<bool>();
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
                perkButtonIcons.Add(detail.IconResref);
                perkButtonTexts.Add($"{detail.Name} ({playerRank} / {detail.PerkLevels.Count})");
                perkDetailSelected.Add(false);
                perkButtonColors.Add(meetsRequirements ? _green : _red);
            }

            PerkButtonColors = perkButtonColors;
            PerkButtonIcons = perkButtonIcons;
            PerkButtonTexts = perkButtonTexts;
            PerkDetailSelected = perkDetailSelected;
            PageNumbers = pageNumbers;
        }

        private string BuildSelectedPerkDetailText(PerkDetail detail, PerkLevel currentUpgrade, PerkLevel nextUpgrade)
        {
            var categoryDetail = Perk.GetPerkCategoryDetails(detail.Category);
            var selectedDetails = detail.Name + "\n\n";

            // Perk Description
            if (detail.Description != null)
            {
                selectedDetails += "Description: \n" + detail.Description + "\n";
            }

            selectedDetails += $"[{categoryDetail.Name}]\n\n";

            if (currentUpgrade != null)
            {
                selectedDetails += "Current Upgrade: \n" + currentUpgrade.Description + "\n\n";
            }

            if (nextUpgrade != null)
            {
                selectedDetails += "Next Upgrade: \n" +
                                   $"    Price: {nextUpgrade.Price} SP\n" +
                                   $"{nextUpgrade.Description}\n\n";
            }

            return selectedDetails;
        }

        private (bool, GuiBindingList<string>, GuiBindingList<GuiColor>) BuildRequirements(PerkLevel nextUpgrade)
        {
            var meetsRequirements = true;
            var requirements = new GuiBindingList<string>();
            var requirementColors = new GuiBindingList<GuiColor>();

            if (nextUpgrade == null)
            {
                requirements.Add("MAXED");
                requirementColors.Add(_green);
            }
            else
            {
                foreach (var req in nextUpgrade.Requirements)
                {
                    requirements.Add(req.RequirementText);

                    if (string.IsNullOrWhiteSpace(req.CheckRequirements(Player)))
                    {
                        requirementColors.Add(_green);
                    }
                    else
                    {
                        requirementColors.Add(_red);
                        meetsRequirements = false;
                    }
                }

                if (nextUpgrade.Requirements.Count <= 0)
                {
                    requirements.Add("None");
                    requirementColors.Add(_green);
                }
            }

            return (meetsRequirements, requirements, requirementColors);
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
            var selectedDetails = BuildSelectedPerkDetailText(detail, currentUpgrade, nextUpgrade);
            var meetsRequirements = true;
            
            var (meetsReqs, requirements, requirementColors) = BuildRequirements(nextUpgrade);
            meetsRequirements = meetsReqs;
            SelectedRequirements = requirements;
            SelectedRequirementColors = requirementColors;

            BuyText = nextUpgrade != null
                ? $"Buy Upgrade ({nextUpgrade.Price} SP)"
                : "Buy Upgrade";
            IsBuyEnabled = nextUpgrade != null &&
                           dbPlayer.UnallocatedSP >= nextUpgrade.Price &&
                           meetsRequirements;

            SelectedDetails = selectedDetails;
            IsPerkSelected = true;
            IsRefundEnabled = (dbPlayer.DatePerkRefundAvailable == null ||
                               dbPlayer.DatePerkRefundAvailable <= DateTime.UtcNow) &&
                              dbPlayer.NumberPerkResetsAvailable > 0 &&
                              currentUpgrade != null;
        };

        private void GrantFeats(PerkLevel nextLevel)
        {
            foreach (var feat in nextLevel.GrantedFeats)
            {
                if (GetHasFeat(feat, Player)) continue;
                CreaturePlugin.AddFeatByLevel(Player, feat, 1);

                // If feat isn't registered or the ability doesn't have an impact or concentration action,
                // don't add the feat to the player's hot bar.
                if (!Ability.IsFeatRegistered(feat)) continue;
                var abilityDetail = Ability.GetAbilityDetail(feat);
                if (abilityDetail.ImpactAction == null && abilityDetail.ConcentrationStatusEffectType == StatusEffectType.Invalid) continue;

                AddFeatToHotBar(feat);
            }
        }

        private void AddFeatToHotBar(FeatType feat)
        {
            var qbs = PlayerQuickBarSlot.UseFeat(feat);

            // Try to add the new feat to the player's hotbar.
            if (PlayerPlugin.GetQuickBarSlot(Player, 0).ObjectType == QuickBarSlotType.Empty)
                PlayerPlugin.SetQuickBarSlot(Player, 0, qbs);
            else if (PlayerPlugin.GetQuickBarSlot(Player, 1).ObjectType == QuickBarSlotType.Empty)
                PlayerPlugin.SetQuickBarSlot(Player, 1, qbs);
            else if (PlayerPlugin.GetQuickBarSlot(Player, 2).ObjectType == QuickBarSlotType.Empty)
                PlayerPlugin.SetQuickBarSlot(Player, 2, qbs);
            else if (PlayerPlugin.GetQuickBarSlot(Player, 3).ObjectType == QuickBarSlotType.Empty)
                PlayerPlugin.SetQuickBarSlot(Player, 3, qbs);
            else if (PlayerPlugin.GetQuickBarSlot(Player, 4).ObjectType == QuickBarSlotType.Empty)
                PlayerPlugin.SetQuickBarSlot(Player, 4, qbs);
            else if (PlayerPlugin.GetQuickBarSlot(Player, 5).ObjectType == QuickBarSlotType.Empty)
                PlayerPlugin.SetQuickBarSlot(Player, 5, qbs);
            else if (PlayerPlugin.GetQuickBarSlot(Player, 6).ObjectType == QuickBarSlotType.Empty)
                PlayerPlugin.SetQuickBarSlot(Player, 6, qbs);
            else if (PlayerPlugin.GetQuickBarSlot(Player, 7).ObjectType == QuickBarSlotType.Empty)
                PlayerPlugin.SetQuickBarSlot(Player, 7, qbs);
            else if (PlayerPlugin.GetQuickBarSlot(Player, 8).ObjectType == QuickBarSlotType.Empty)
                PlayerPlugin.SetQuickBarSlot(Player, 8, qbs);
            else if (PlayerPlugin.GetQuickBarSlot(Player, 9).ObjectType == QuickBarSlotType.Empty)
                PlayerPlugin.SetQuickBarSlot(Player, 9, qbs);
            else if (PlayerPlugin.GetQuickBarSlot(Player, 10).ObjectType == QuickBarSlotType.Empty)
                PlayerPlugin.SetQuickBarSlot(Player, 10, qbs);
        }

        // Applies any Purchase triggers associated with this perk.
        private void ApplyPurchasePerkTriggers(int perkLevel, PerkType selectedPerk)
        {
            var perkDetail = Perk.GetPerkDetails(selectedPerk);
            if (perkDetail.PurchasedTriggers.Count > 0)
            {
                foreach (var action in perkDetail.PurchasedTriggers)
                {
                    action(Player, selectedPerk, perkLevel);
                }
            }
        }

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

                    if (GetResRef(GetArea(Player)) == "char_migration")
                    {
                        FloatingTextStringOnCreature($"Perks cannot be purchased in this area.", Player, false);
                        return;
                    }

                    // Refresh data
                    dbPlayer = DB.Get<Player>(playerId);
                    selectedPerk = _filteredPerks[_selectedPerkIndex];
                    detail = Perk.GetPerkDetails(selectedPerk);
                    playerRank = dbPlayer.Perks.ContainsKey(selectedPerk)
                        ? dbPlayer.Perks[selectedPerk]
                        : 0;
                    nextUpgrade = detail.PerkLevels.ContainsKey(playerRank + 1)
                        ? detail.PerkLevels[playerRank + 1]
                        : null;

                    // Run validation again
                    if (nextUpgrade == null)
                        return;

                    if (playerRank + 1 > detail.PerkLevels.Count)
                        return;

                    foreach (var req in nextUpgrade.Requirements)
                    {
                        if (!string.IsNullOrWhiteSpace(req.CheckRequirements(Player)))
                        {
                            return;
                        }
                    }

                    if (dbPlayer.UnallocatedSP < nextUpgrade.Price)
                        return;

                    // Custom purchase validation logic for the perk.
                    var canPurchase = detail.PurchaseRequirement == null
                        ? string.Empty
                        : detail.PurchaseRequirement(Player, selectedPerk, playerRank);

                    if (!string.IsNullOrWhiteSpace(canPurchase))
                    {
                        SendMessageToPC(Player, ColorToken.Red(canPurchase));
                        return;
                    }

                    // All validation passes. Perform the upgrade.
                    dbPlayer.Perks[selectedPerk] = playerRank + 1;
                    dbPlayer.UnallocatedSP -= nextUpgrade.Price;
                    DB.Set(dbPlayer);

                    GrantFeats(nextUpgrade);
                    ApplyPurchasePerkTriggers(dbPlayer.Perks[selectedPerk], selectedPerk);

                    FloatingTextStringOnCreature(ColorToken .Green($"You purchase '{detail.Name}' rank {dbPlayer.Perks[selectedPerk]}."), Player, false);

                    EventsPlugin.SignalEvent("SWLOR_BUY_PERK", Player);
                    Gui.PublishRefreshEvent(Player, new PerkAcquiredRefreshEvent(selectedPerk));

                    // Update UI with latest upgrade changes.
                    LoadCharacterDetails();

                    var currentUpgrade = detail.PerkLevels.ContainsKey(dbPlayer.Perks[selectedPerk])
                        ? detail.PerkLevels[dbPlayer.Perks[selectedPerk]]
                        : null;
                    nextUpgrade = detail.PerkLevels.ContainsKey(dbPlayer.Perks[selectedPerk] + 1)
                        ? detail.PerkLevels[dbPlayer.Perks[selectedPerk] + 1]
                        : null;
                    SelectedDetails = BuildSelectedPerkDetailText(detail, currentUpgrade, nextUpgrade);

                    PerkButtonTexts[_selectedPerkIndex] = $"{detail.Name} ({dbPlayer.Perks[selectedPerk]} / {detail.PerkLevels.Count})";

                    var (meetsRequirements, requirements, requirementColors) = BuildRequirements(nextUpgrade);

                    PerkButtonColors[_selectedPerkIndex] = meetsRequirements ? _green : _red;
                    SelectedRequirements = requirements;
                    SelectedRequirementColors = requirementColors;
                    IsBuyEnabled = nextUpgrade != null &&
                                   dbPlayer.UnallocatedSP >= nextUpgrade.Price &&
                                   meetsRequirements;

                    BuyText = nextUpgrade != null
                        ? $"Buy Upgrade ({nextUpgrade.Price} SP)"
                        : "Buy Upgrade";
                });
        };

        public Action OnClickRefund() => () =>
        {
            ShowModal($"You may only refund one perk per 12 hours (real world time). This will also consume a refund token. Are you sure you want to refund this perk?", () =>
            {
                var playerId = GetObjectUUID(Player);
                var dbPlayer = DB.Get<Player>(playerId);
                var selectedPerk = _filteredPerks[SelectedPerkIndex];
                var perkDetail = Perk.GetPerkDetails(selectedPerk);

                if (dbPlayer.NumberPerkResetsAvailable <= 0)
                {
                    FloatingTextStringOnCreature($"You do not have any refund tokens.", Player, false);
                }
                else if (dbPlayer.DatePerkRefundAvailable != null &&
                    dbPlayer.DatePerkRefundAvailable > DateTime.UtcNow)
                {
                    var delta = (DateTime)dbPlayer.DatePerkRefundAvailable - DateTime.UtcNow;
                    var time = Time.GetTimeLongIntervals(delta.Days, delta.Hours, delta.Minutes, delta.Seconds, false);
                    FloatingTextStringOnCreature($"You can refund another perk in {time}.", Player, false);
                }
                else
                {
                    // Some individual perks have validation checks. 
                    // Run that now if specified.
                    var canRefund = perkDetail.RefundRequirement == null
                        ? string.Empty
                        : perkDetail.RefundRequirement(Player, selectedPerk, Perk.GetEffectivePerkLevel(Player, selectedPerk));
                    if (!string.IsNullOrWhiteSpace(canRefund))
                    {
                        FloatingTextStringOnCreature(canRefund, Player, false);
                        return;
                    }

                    var pcPerkLevel = dbPlayer.Perks[selectedPerk];
                    var refundAmount = perkDetail.PerkLevels
                        .Where(x => x.Key <= pcPerkLevel)
                        .Sum(x => x.Value.Price);
                    // Update player's DB record.
                    dbPlayer.DatePerkRefundAvailable = DateTime.UtcNow.AddHours(12);
                    dbPlayer.UnallocatedSP += refundAmount;
                    dbPlayer.Perks.Remove(selectedPerk);
                    dbPlayer.NumberPerkResetsAvailable--;
                    DB.Set(dbPlayer);

                    // Write an audit log and notify the player
                    Log.Write(LogGroup.PerkRefund, $"REFUND - {playerId} - Refunded Date {DateTime.UtcNow} - Level {pcPerkLevel} - PerkID {selectedPerk}");
                    FloatingTextStringOnCreature($"Perk refunded! You reclaimed {refundAmount} SP.", Player, false);
                    Gui.PublishRefreshEvent(Player, new PerkRefundedRefreshEvent(selectedPerk));

                    // Remove all feats granted by all levels of this perk.
                    var feats = perkDetail.PerkLevels.Values.SelectMany(s => s.GrantedFeats);
                    foreach (var feat in feats)
                    {
                        CreaturePlugin.RemoveFeat(Player, feat);
                    }

                    // Run all of the triggers related to refunding this perk.
                    foreach (var action in perkDetail.RefundedTriggers)
                    {
                        action(Player, selectedPerk, 0);
                    }

                    LoadCharacterDetails();
                    SelectedPerkIndex = -1;
                    LoadPerks();
                }
            });
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
            LoadCharacterDetails();
        }

        public void Refresh(PerkResetAcquiredRefreshEvent payload)
        {
            LoadCharacterDetails();
        }
    }
}
