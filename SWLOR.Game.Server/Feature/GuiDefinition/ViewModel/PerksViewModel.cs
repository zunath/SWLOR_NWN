using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.Associate;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CurrencyService;
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
        private const int ItemsPerPage = 30;
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
            IsInMyPerksMode = true;
            IsInBeastPerksMode = false;
            _initialLoadDone = false;
            SelectedPerkCategoryId = 0;
            SearchText = string.Empty;
            BuyText = "Buy Upgrade";
            SelectedPage = 1;
            IsPerkSelected = false;
            IsBuyEnabled = false;

            _initialLoadDone = true;
            LoadCategories();
            LoadDetails();
            LoadPerks();

            WatchOnClient(model => model.SelectedPerkCategoryId);
            WatchOnClient(model => model.SearchText);
            WatchOnClient(model => model.SelectedPage);
        }

        private void LoadCategories()
        {
            var groupType = IsInMyPerksMode ? PerkGroupType.Player : PerkGroupType.Beast;
            var categories = new GuiBindingList<GuiComboEntry>
            {
                new("<All Categories>", 0)
            };

            foreach (var (type, detail) in Perk.GetAllActivePerkCategories(groupType)) 
            {
                categories.Add(new GuiComboEntry(detail.Name, (int)type));
            }

            Categories = categories;
        }

        private void LoadDetails()
        {
            var playerId = GetObjectUUID(Player);
            var dbPlayer = DB.Get<Player>(playerId);
            var now = DateTime.UtcNow;

            if (IsInMyPerksMode)
            {
                AvailableSP = $"Available SP: {dbPlayer.UnallocatedSP}";
                TotalSP = $"Total SP: {dbPlayer.TotalSPAcquired} / {Skill.SkillCap}";
            }
            else if (IsInBeastPerksMode)
            {
                var dbBeast = DB.Get<Beast>(dbPlayer.ActiveBeastId);
                AvailableSP = $"Available SP: {dbBeast.UnallocatedSP}";
                TotalSP = $"Total SP: {dbBeast.Level} / {BeastMastery.MaxLevel}";
            }

            var dateRefundAvailable = dbPlayer.DatePerkRefundAvailable ?? now;
            var isRefundAvailable = dateRefundAvailable <= now;
            var dateRefundAvailableText = isRefundAvailable
                ? "Now"
                : Time.GetTimeToWaitLongIntervals(now, dateRefundAvailable, true);
            ResetNextAvailable = $"Reset Available: {dateRefundAvailableText} [# Available: {Currency.GetCurrency(Player, CurrencyType.PerkRefundToken)}]";
            IsRefundEnabled = false;
            HasBeast = !string.IsNullOrWhiteSpace(dbPlayer.ActiveBeastId);
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

            var group = IsInMyPerksMode
                ? PerkGroupType.Player
                : PerkGroupType.Beast;
            var perkList = SelectedPerkCategoryId == 0
                ? Perk.GetAllActivePerks(group)
                : Perk.GetActivePerksInCategory(group, (PerkCategoryType)SelectedPerkCategoryId);

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
                int rank;

                if (IsInMyPerksMode)
                {
                    rank = dbPlayer.Perks.ContainsKey(type)
                        ? dbPlayer.Perks[type]
                        : 0;
                }
                else
                {
                    var dbBeast = DB.Get<Beast>(dbPlayer.ActiveBeastId);
                    if (dbBeast == null)
                    {
                        rank = 0;
                    }
                    else
                    {
                        rank = dbBeast.Perks.ContainsKey(type)
                            ? dbBeast.Perks[type]
                            : 0;
                    }
                }

                var nextUpgrade = detail.PerkLevels.ContainsKey(rank + 1)
                    ? detail.PerkLevels[rank + 1]
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
                perkButtonTexts.Add($"{detail.Name} ({rank} / {detail.PerkLevels.Count})");
                perkDetailSelected.Add(false);
                perkButtonColors.Add(meetsRequirements ? GuiColor.Green : GuiColor.Red);
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
                requirementColors.Add(GuiColor.Green);
            }
            else
            {
                foreach (var req in nextUpgrade.Requirements)
                {
                    requirements.Add(req.RequirementText);

                    if (string.IsNullOrWhiteSpace(req.CheckRequirements(Player)))
                    {
                        requirementColors.Add(GuiColor.Green);
                    }
                    else
                    {
                        requirementColors.Add(GuiColor.Red);
                        meetsRequirements = false;
                    }
                }

                if (nextUpgrade.Requirements.Count <= 0)
                {
                    requirements.Add("None");
                    requirementColors.Add(GuiColor.Green);
                }
            }

            return (meetsRequirements, requirements, requirementColors);
        }

        public Action OnSelectPerk() => () =>
        {
            var index = NuiGetEventArrayIndex();
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

            var currentUpgrade = detail.PerkLevels.ContainsKey(rank)
                ? detail.PerkLevels[rank]
                : null;
            var nextUpgrade = detail.PerkLevels.ContainsKey(rank + 1)
                ? detail.PerkLevels[rank + 1]
                : null;

            var selectedDetails = BuildSelectedPerkDetailText(detail, currentUpgrade, nextUpgrade);

            var (meetsRequirements, requirements, requirementColors) = BuildRequirements(nextUpgrade);
            SelectedRequirements = requirements;
            SelectedRequirementColors = requirementColors;

            BuyText = nextUpgrade != null
                ? $"Buy Upgrade ({nextUpgrade.Price} SP)"
                : "Buy Upgrade";
            IsBuyEnabled = nextUpgrade != null &&
                           unallocatedSP >= nextUpgrade.Price &&
                           meetsRequirements;

            SelectedDetails = selectedDetails;
            IsPerkSelected = true;
            IsRefundEnabled = (dbPlayer.DatePerkRefundAvailable == null ||
                               dbPlayer.DatePerkRefundAvailable <= DateTime.UtcNow) &&
                              Currency.GetCurrency(Player, CurrencyType.PerkRefundToken) > 0 &&
                              currentUpgrade != null;
        };

        private void GrantFeats(PerkLevel nextLevel)
        {
            var target = IsInMyPerksMode ? Player : GetAssociate(AssociateType.Henchman, Player);
            if (!GetIsObjectValid(target))
                return;

            foreach (var feat in nextLevel.GrantedFeats)
            {
                if (GetHasFeat(feat, target)) continue;
                CreaturePlugin.AddFeatByLevel(target, feat, 1);

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
            if (!IsInMyPerksMode)
                return;

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
            var target = IsInMyPerksMode ? Player : GetAssociate(AssociateType.Henchman, Player);
            if (!GetIsObjectValid(target))
                return;

            var perkDetail = Perk.GetPerkDetails(selectedPerk);
            if (perkDetail.PurchasedTriggers.Count > 0)
            {
                foreach (var action in perkDetail.PurchasedTriggers)
                {
                    action(target, selectedPerk, perkLevel);
                }
            }
        }

        public Action OnClickBuyUpgrade() => () =>
        {
            int rank;
            var selectedPerk = _filteredPerks[_selectedPerkIndex];

            var playerId = GetObjectUUID(Player);
            var dbPlayer = DB.Get<Player>(playerId);
            if (IsInMyPerksMode)
            {
                rank = dbPlayer.Perks.ContainsKey(selectedPerk)
                    ? dbPlayer.Perks[selectedPerk]
                    : 0;
            }
            else
            {
                var dbBeast = DB.Get<Beast>(dbPlayer.ActiveBeastId);
                if (dbBeast == null)
                    return;

                rank = dbBeast.Perks.ContainsKey(selectedPerk)
                    ? dbBeast.Perks[selectedPerk]
                    : 0;
            }
            
            var detail = Perk.GetPerkDetails(selectedPerk);
            
            var nextUpgrade = detail.PerkLevels.ContainsKey(rank + 1)
                ? detail.PerkLevels[rank + 1]
                : null;

            ShowModal($"This upgrade will cost {nextUpgrade?.Price} SP. Are you sure you want to buy it?", 
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
                    int unallocatedSP;

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

                    nextUpgrade = detail.PerkLevels.ContainsKey(rank + 1)
                        ? detail.PerkLevels[rank + 1]
                        : null;

                    // Run validation again
                    if (nextUpgrade == null)
                        return;

                    if (rank + 1 > detail.PerkLevels.Count)
                        return;

                    foreach (var req in nextUpgrade.Requirements)
                    {
                        if (!string.IsNullOrWhiteSpace(req.CheckRequirements(Player)))
                        {
                            return;
                        }
                    }

                    if (unallocatedSP < nextUpgrade.Price)
                        return;

                    // Custom purchase validation logic for the perk.
                    var canPurchase = detail.PurchaseRequirement == null
                        ? string.Empty
                        : detail.PurchaseRequirement(Player, selectedPerk, rank);

                    if (!string.IsNullOrWhiteSpace(canPurchase))
                    {
                        SendMessageToPC(Player, ColorToken.Red(canPurchase));
                        return;
                    }

                    // All validation passes. Perform the upgrade.
                    if (IsInMyPerksMode)
                    {
                        dbPlayer.Perks[selectedPerk] = rank + 1;
                        dbPlayer.UnallocatedSP -= nextUpgrade.Price;
                        DB.Set(dbPlayer);

                        unallocatedSP = dbPlayer.UnallocatedSP;
                    }
                    else
                    {
                        var dbBeast = DB.Get<Beast>(dbPlayer.ActiveBeastId);
                        if (dbBeast == null)
                            return;

                        dbBeast.Perks[selectedPerk] = rank + 1;
                        dbBeast.UnallocatedSP -= nextUpgrade.Price;
                        DB.Set(dbBeast);

                        unallocatedSP = dbBeast.UnallocatedSP;
                    }

                    var newRank = rank + 1;
                    GrantFeats(nextUpgrade);
                    ApplyPurchasePerkTriggers(newRank, selectedPerk);

                    FloatingTextStringOnCreature(ColorToken.Green($"You purchase '{detail.Name}' rank {newRank}."), Player, false);

                    EventsPlugin.SignalEvent("SWLOR_BUY_PERK", Player);
                    Gui.PublishRefreshEvent(Player, new PerkAcquiredRefreshEvent(selectedPerk));

                    // Update UI with latest upgrade changes.
                    LoadDetails();

                    var currentUpgrade = detail.PerkLevels.ContainsKey(newRank)
                        ? detail.PerkLevels[newRank]
                        : null;
                    nextUpgrade = detail.PerkLevels.ContainsKey(newRank + 1)
                        ? detail.PerkLevels[newRank + 1]
                        : null;
                    SelectedDetails = BuildSelectedPerkDetailText(detail, currentUpgrade, nextUpgrade);
                    PerkButtonTexts[_selectedPerkIndex] = $"{detail.Name} ({newRank} / {detail.PerkLevels.Count})";

                    var (meetsRequirements, requirements, requirementColors) = BuildRequirements(nextUpgrade);

                    PerkButtonColors[_selectedPerkIndex] = meetsRequirements ? GuiColor.Green : GuiColor.Red;
                    SelectedRequirements = requirements;
                    SelectedRequirementColors = requirementColors;
                    IsBuyEnabled = nextUpgrade != null &&
                                   unallocatedSP >= nextUpgrade.Price &&
                                   meetsRequirements;

                    BuyText = nextUpgrade != null
                        ? $"Buy Upgrade ({nextUpgrade.Price} SP)"
                        : "Buy Upgrade";
                });
        };

        public Action OnClickRefund() => () =>
        {
            ShowModal($"You may only refund one perk per 1 hour (real world time). This will also consume a refund token. Are you sure you want to refund this perk?", () =>
            {
                var playerId = GetObjectUUID(Player);
                var dbPlayer = DB.Get<Player>(playerId);
                var selectedPerk = _filteredPerks[SelectedPerkIndex];
                var perkDetail = Perk.GetPerkDetails(selectedPerk);
                var target = IsInMyPerksMode ? Player : GetAssociate(AssociateType.Henchman, Player);

                if (Currency.GetCurrency(Player, CurrencyType.PerkRefundToken) <= 0)
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
                        : perkDetail.RefundRequirement(target, selectedPerk, Perk.GetEffectivePerkLevel(target, selectedPerk));
                    if (!string.IsNullOrWhiteSpace(canRefund))
                    {
                        FloatingTextStringOnCreature(canRefund, Player, false);
                        return;
                    }

                    if (IsInMyPerksMode)
                    {
                        var perkLevel = dbPlayer.Perks[selectedPerk];
                        var refundAmount = perkDetail.PerkLevels
                            .Where(x => x.Key <= perkLevel)
                            .Sum(x => x.Value.Price);
                        
                        dbPlayer.UnallocatedSP += refundAmount;
                        dbPlayer.Perks.Remove(selectedPerk);

                        Log.Write(LogGroup.PerkRefund, $"REFUND - {playerId} - Refunded Date {DateTime.UtcNow} - Level {perkLevel} - PerkID {selectedPerk}");
                        FloatingTextStringOnCreature($"Perk refunded! You reclaimed {refundAmount} SP.", Player, false);
                    }
                    else
                    {
                        var dbBeast = DB.Get<Beast>(dbPlayer.ActiveBeastId);
                        if (dbBeast == null)
                            return;

                        var perkLevel = dbBeast.Perks[selectedPerk];
                        var refundAmount = perkDetail.PerkLevels
                            .Where(x => x.Key <= perkLevel)
                            .Sum(x => x.Value.Price);

                        dbBeast.UnallocatedSP += refundAmount;
                        dbBeast.Perks.Remove(selectedPerk);

                        DB.Set(dbBeast);

                        Log.Write(LogGroup.PerkRefund, $"REFUND Beast - {dbBeast.Id} (Owner: {dbPlayer.Id}) - Refunded Date {DateTime.UtcNow} - Level {perkLevel} - PerkID {selectedPerk}");
                        FloatingTextStringOnCreature($"Perk refunded! Your beast reclaimed {refundAmount} SP.", Player, false);
                    }

                    dbPlayer.DatePerkRefundAvailable = DateTime.UtcNow.AddHours(1);
                    DB.Set(dbPlayer);
                    Currency.TakeCurrency(Player, CurrencyType.PerkRefundToken, 1);

                    Gui.PublishRefreshEvent(Player, new PerkRefundedRefreshEvent(selectedPerk));

                    // Remove all feats granted by all levels of this perk.
                    var feats = perkDetail.PerkLevels.Values.SelectMany(s => s.GrantedFeats);
                    foreach (var feat in feats)
                    {
                        CreaturePlugin.RemoveFeat(target, feat);
                    }

                    // Run all of the triggers related to refunding this perk.
                    foreach (var action in perkDetail.RefundedTriggers)
                    {
                        action(target, selectedPerk, 0);
                    }

                    LoadDetails();
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
            LoadDetails();
        }

        public void Refresh(PerkResetAcquiredRefreshEvent payload)
        {
            LoadDetails();
        }

        public Action OnClickMyPerks() => () =>
        {
            IsInMyPerksMode = true;
            IsInBeastPerksMode = false;
            SelectedPerkCategoryId = 0;
            LoadCategories();
            LoadDetails();
            LoadPerks();
        };
        public Action OnClickBeastPerks() => () =>
        {
            IsInMyPerksMode = false;
            IsInBeastPerksMode = true;
            SelectedPerkCategoryId = 0;
            LoadCategories();
            LoadDetails();
            LoadPerks();
        };
    }
}
