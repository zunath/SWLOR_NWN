using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CurrencyService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Associate;
using Skill = SWLOR.Game.Server.Service.Skill;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class PerksViewModel : GuiViewModelBase<PerksViewModel, GuiPayloadBase>,
        IGuiRefreshable<SkillXPRefreshEvent>,
        IGuiRefreshable<PerkResetAcquiredRefreshEvent>,
        IGuiRefreshable<PerkRefundCooldownResetRefreshEvent>
    {
        private const int ItemsPerPage = 30;
        private const int AutoAddHotBarSlots = 11;
        private const int TotalHotBarSlots = 36;
        private int _pages;
        private bool _initialLoadDone;

        private enum PerkSortOrder
        {
            AlphabeticalAscending = 0,
            AlphabeticalDescending = 1,
            SkillLevelAscending = 2,
            SkillLevelDescending = 3
        }

        private enum PerkRowStatus
        {
            Maxed,
            Buyable,
            Unaffordable,
            Locked
        }

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

        public string ForceAffinityExplanation
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

        public PerksViewModel()
        {
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
            SelectedSortOrderId = (int)PerkSortOrder.AlphabeticalAscending;
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

        private (PerkRowStatus status, GuiColor color, string iconResref, string tooltip) GetPerkRowStatus(PerkDetail detail, int rank, int unallocatedSP)
        {
            var nextUpgrade = detail.PerkLevels.ContainsKey(rank + 1)
                ? detail.PerkLevels[rank + 1]
                : null;

            if (nextUpgrade == null)
            {
                return (PerkRowStatus.Maxed, new GuiColor(90, 170, 230), PerkRequirementCategoryResolver.MaxedIcon, "Fully upgraded.");
            }

            // Find the first unmet requirement (the active restriction), if any.
            IPerkRequirement gatingRequirement = null;
            var gatingError = string.Empty;
            foreach (var req in nextUpgrade.Requirements)
            {
                var error = req.CheckRequirements(Player);
                if (!string.IsNullOrWhiteSpace(error))
                {
                    gatingRequirement = req;
                    gatingError = error;
                    break;
                }
            }

            // When locked, show the gating requirement's red (inaccessible) icon.
            if (gatingRequirement != null)
            {
                var lockedDetail = PerkRequirementCategoryResolver.GetDetail(gatingRequirement.Category);
                var tooltip = $"Locked - {lockedDetail.Name}: {gatingRequirement.RequirementText}. {gatingError}";
                return (PerkRowStatus.Locked, GuiColor.Grey, lockedDetail.IconResrefLocked, tooltip);
            }

            // Requirements met: show the green check (purchasable, or met but unaffordable).
            if (unallocatedSP >= nextUpgrade.Price)
            {
                return (PerkRowStatus.Buyable, new GuiColor(60, 200, 90), PerkRequirementCategoryResolver.MetIcon, $"Can buy - {nextUpgrade.Price} SP");
            }

            return (PerkRowStatus.Unaffordable, new GuiColor(230, 180, 70), PerkRequirementCategoryResolver.MetIcon,
                $"Costs {nextUpgrade.Price} SP - you have {unallocatedSP}");
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

            LoadForceAffinityDetails(dbPlayer);

            if (IsInMyPerksMode)
            {
                AvailableSP = $"Available SP: {dbPlayer.UnallocatedSP}";
                TotalSP = $"Total SP: {Skill.GetTotalSkillPoints(dbPlayer)} / {Skill.TotalSkillPointCap}";
                WindowTitle = $"Perks - {dbPlayer.UnallocatedSP} SP available";
            }
            else if (IsInBeastPerksMode)
            {
                var dbBeast = DB.Get<Beast>(dbPlayer.ActiveBeastId);
                AvailableSP = $"Available SP: {dbBeast.UnallocatedSP}";
                TotalSP = $"Total SP: {dbBeast.Level} / {BeastMastery.MaxLevel}";
                WindowTitle = $"Beast Perks - {dbBeast.UnallocatedSP} SP available";
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

        private void LoadForceAffinityDetails(Player dbPlayer)
        {
            IsForceAffinityVisible = IsInMyPerksMode &&
                                     dbPlayer.CharacterType == CharacterType.ForceSensitive;
            if (!IsForceAffinityVisible)
            {
                ForceAffinityHeading = string.Empty;
                ForceAffinityExplanation = string.Empty;
                ForceAffinityColor = GuiColor.White;
                return;
            }

            var affinity = Perk.GetForceAffinity(Player);
            var affinityLabel = FormatForceAffinity(affinity);
            ForceAffinityHeading = $"FORCE AFFINITY: {affinityLabel}";
            ForceAffinityExplanation =
                "Owning any rank of a Light power contributes +1; a Dark power contributes -1. " +
                "Each point changes aligned or opposed magnitude by 5%. At full affinity, hit chance shifts by 5%. " +
                "Universal powers and effect durations are unaffected. Select a Force perk for its current result.";
            ForceAffinityColor = affinity > 0
                ? new GuiColor(120, 190, 255)
                : affinity < 0
                    ? new GuiColor(225, 100, 100)
                    : new GuiColor(221, 181, 93);
        }

        private void LoadPerks()
        {
            if (!_initialLoadDone) return;

            var playerId = GetObjectUUID(Player);
            var dbPlayer = DB.Get<Player>(playerId);
            var dbBeast = IsInBeastPerksMode
                ? DB.Get<Beast>(dbPlayer.ActiveBeastId)
                : null;

            _filteredPerks.Clear();
            SelectedPerkIndex = -1;

            var perkButtonColors = new GuiBindingList<GuiColor>();
            var perkButtonIcons = new GuiBindingList<string>();
            var perkButtonTexts = new GuiBindingList<string>();
            var perkDetailSelected = new GuiBindingList<bool>();
            var perkRowReqIcons = new GuiBindingList<string>();
            var perkRowReqTooltips = new GuiBindingList<string>();
            var perkRowCosts = new GuiBindingList<string>();
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

            var sortedPerks = SortPerks(perkList, dbPlayer, dbBeast).ToList();

            var unallocatedSP = IsInMyPerksMode
                ? dbPlayer.UnallocatedSP
                : dbBeast?.UnallocatedSP ?? 0;

            // Row status is computed once per perk and reused between the status filter
            // and the page bindings, since GetPerkRowStatus runs requirement checks that
            // can hit the database.
            var rowStateCache = new Dictionary<PerkType, (int rank, PerkRowStatus status, GuiColor color, string iconResref, string tooltip)>();

            (int rank, PerkRowStatus status, GuiColor color, string iconResref, string tooltip) GetRowState(PerkType type, PerkDetail detail)
            {
                if (!rowStateCache.TryGetValue(type, out var state))
                {
                    var rank = GetCurrentPerkRank(dbPlayer, dbBeast, type);
                    var (status, color, iconResref, tooltip) = GetPerkRowStatus(detail, rank, unallocatedSP);
                    state = (rank, status, color, iconResref, tooltip);
                    rowStateCache[type] = state;
                }

                return state;
            }

            // Apply the status filter to the full sorted list before pagination so that
            // pages, page numbers, and the selection index list all reflect the filtered set.
            if (SelectedStatusFilter != 0)
            {
                sortedPerks = sortedPerks.Where(x =>
                {
                    var (rank, status, _, _, _) = GetRowState(x.Key, x.Value);

                    return SelectedStatusFilter switch
                    {
                        1 => rank >= 1,
                        2 => status == PerkRowStatus.Buyable,
                        3 => status == PerkRowStatus.Maxed,
                        _ => true
                    };
                }).ToList();
            }

            _pages = sortedPerks.Count / ItemsPerPage + (sortedPerks.Count % ItemsPerPage == 0 ? 0 : 1);

            for (var x = 1; x <= _pages; x++)
            {
                pageNumbers.Add(new GuiComboEntry($"Page {x}", x));
            }

            // Paginate the results
            var pagedPerks = sortedPerks
                .Skip((SelectedPage - 1) * ItemsPerPage)
                .Take(ItemsPerPage);

            foreach (var (type, detail) in pagedPerks)
            {
                var (rank, _, color, iconResref, tooltip) = GetRowState(type, detail);

                _filteredPerks.Add(type);
                perkButtonIcons.Add(detail.IconResref);
                perkButtonTexts.Add($"{detail.Name} ({rank}/{detail.PerkLevels.Count})");
                perkDetailSelected.Add(false);
                perkButtonColors.Add(color);
                perkRowReqIcons.Add(iconResref);
                perkRowReqTooltips.Add(tooltip);

                var nextLevel = detail.PerkLevels.ContainsKey(rank + 1)
                    ? detail.PerkLevels[rank + 1]
                    : null;
                perkRowCosts.Add(nextLevel != null ? $"{nextLevel.Price} SP" : string.Empty);
            }

            PerkButtonColors = perkButtonColors;
            PerkButtonIcons = perkButtonIcons;
            PerkButtonTexts = perkButtonTexts;
            PerkDetailSelected = perkDetailSelected;
            PerkRowReqIcons = perkRowReqIcons;
            PerkRowReqTooltips = perkRowReqTooltips;
            PerkRowCosts = perkRowCosts;
            PageNumbers = pageNumbers;

            // Select the first perk so the detail panel shows content immediately
            // instead of leaving an empty pane when the list loads or is re-filtered.
            if (_filteredPerks.Count > 0)
            {
                SelectPerkAt(0);
            }
        }

        private IEnumerable<KeyValuePair<PerkType, PerkDetail>> SortPerks(
            IEnumerable<KeyValuePair<PerkType, PerkDetail>> perks,
            Player dbPlayer,
            Beast dbBeast)
        {
            var sortOrder = (PerkSortOrder)SelectedSortOrderId;

            return sortOrder switch
            {
                PerkSortOrder.AlphabeticalDescending => perks
                    .OrderByDescending(x => x.Value.Name, StringComparer.OrdinalIgnoreCase),
                PerkSortOrder.SkillLevelAscending => perks
                    .OrderBy(x => GetRequiredSkillLevelSortOrder(x.Value, GetCurrentPerkRank(dbPlayer, dbBeast, x.Key)))
                    .ThenBy(x => x.Value.Name, StringComparer.OrdinalIgnoreCase),
                PerkSortOrder.SkillLevelDescending => perks
                    .OrderByDescending(x => GetRequiredSkillLevelSortOrder(x.Value, GetCurrentPerkRank(dbPlayer, dbBeast, x.Key)))
                    .ThenBy(x => x.Value.Name, StringComparer.OrdinalIgnoreCase),
                _ => perks.OrderBy(x => x.Value.Name, StringComparer.OrdinalIgnoreCase)
            };
        }

        private int GetCurrentPerkRank(Player dbPlayer, Beast dbBeast, PerkType perkType)
        {
            if (IsInMyPerksMode)
            {
                return dbPlayer.Perks.ContainsKey(perkType)
                    ? dbPlayer.Perks[perkType]
                    : 0;
            }

            if (dbBeast == null)
                return 0;

            return dbBeast.Perks.ContainsKey(perkType)
                ? dbBeast.Perks[perkType]
                : 0;
        }

        private static int GetRequiredSkillLevelSortOrder(PerkDetail detail, int rank)
        {
            if (detail.PerkLevels.TryGetValue(rank + 1, out var nextUpgrade))
                return GetRequiredSkillLevel(nextUpgrade);

            return detail.PerkLevels
                .Where(x => x.Key <= rank)
                .OrderByDescending(x => x.Key)
                .Select(x => GetRequiredSkillLevel(x.Value))
                .FirstOrDefault(x => x > 0);
        }

        private static int GetRequiredSkillLevel(PerkLevel level)
        {
            return level.Requirements
                .OfType<PerkRequirementSkill>()
                .Select(x => x.RequiredRank)
                .DefaultIfEmpty(0)
                .Max();
        }

        private string BuildSelectedPerkDetailText(PerkDetail detail, PerkLevel currentUpgrade, PerkLevel nextUpgrade, int rank)
        {
            var categoryDetail = Perk.GetPerkCategoryDetails(detail.Category);
            var selectedDetails = detail.Name + "\n\n";

            selectedDetails += $"[{categoryDetail.Name}]\n";

            var forceAffinityText = BuildForceAffinityPerkDetailText(detail);
            if (!string.IsNullOrWhiteSpace(forceAffinityText))
            {
                selectedDetails += forceAffinityText + "\n";
            }

            var recastGroupText = BuildRecastGroupText(detail);
            if (!string.IsNullOrWhiteSpace(recastGroupText))
            {
                selectedDetails += recastGroupText + "\n";
            }

            selectedDetails += "\n";

            selectedDetails += $"Rank {rank} / {detail.PerkLevels.Count}\n\n";

            if (detail.Description != null)
            {
                selectedDetails += "Description:\n" + detail.Description + "\n\n";
            }

            if (currentUpgrade != null)
            {
                selectedDetails += $"Current (rank {rank}):\n" + currentUpgrade.Description + "\n\n";
            }

            if (nextUpgrade != null)
            {
                selectedDetails += $"Next Upgrade (rank {rank + 1}) - {nextUpgrade.Price} SP:\n" +
                                   nextUpgrade.Description + "\n\n";
            }

            return selectedDetails;
        }

        private string BuildForceAffinityPerkDetailText(PerkDetail detail)
        {
            if (!IsForcePerkCategory(detail.Category))
                return string.Empty;

            if (detail.ForceAffinityType == null)
            {
                return "\nUNIVERSAL FORCE POWER\n" +
                       "Does not change Force Affinity and receives no affinity magnitude or hit-chance adjustment.";
            }

            var affinity = Perk.GetForceAffinity(Player);
            var alignment = detail.ForceAffinityType.Value == ForceAffinityType.Light ? "LIGHT" : "DARK";
            var contribution = detail.ForceAffinityType.Value == ForceAffinityType.Light ? "+1 Light" : "-1 Dark";
            var magnitudeAdjustment = (int)Math.Round(
                (Perk.GetForceAffinityMagnitudeMultiplier(Player, detail.Type) - 1f) * 100f,
                MidpointRounding.AwayFromZero);
            var hitChanceAdjustment = Perk.GetForceAffinityHitChanceAdjustment(Player, detail.Type);

            return $"\n{alignment}-ALIGNED FORCE POWER\n" +
                   $"At {FormatForceAffinity(affinity)}: {FormatSignedPercent(magnitudeAdjustment)} magnitude, " +
                   $"{FormatSignedPercent(hitChanceAdjustment)} hit chance.\n" +
                   $"Owning any rank contributes {contribution}; additional ranks do not add more affinity.";
        }

        private static bool IsForcePerkCategory(PerkCategoryType category)
        {
            return category == PerkCategoryType.ForceAlter ||
                   category == PerkCategoryType.ForceControl ||
                   category == PerkCategoryType.ForceSense;
        }

        private static string FormatForceAffinity(int affinity)
        {
            return affinity > 0
                ? $"+{affinity} Light"
                : affinity < 0
                    ? $"{affinity} Dark"
                    : "0 Neutral";
        }

        private static string FormatSignedPercent(int adjustment)
        {
            return adjustment > 0 ? $"+{adjustment}%" : $"{adjustment}%";
        }

        private static string BuildRecastGroupText(PerkDetail detail)
        {
            var recastGroup = Perk.GetActiveAbilityRecastGroup(detail.Type);
            if (recastGroup == RecastGroup.Invalid ||
                !Recast.IsRecastGroupVisible(recastGroup))
            {
                return string.Empty;
            }

            return $"Recast Group: {Recast.GetRecastGroupDisplayName(recastGroup)}";
        }

        private (bool meetsRequirements,
            GuiBindingList<string> texts,
            GuiBindingList<GuiColor> colors,
            GuiBindingList<string> icons,
            GuiBindingList<string> tooltips) BuildRequirements(PerkLevel nextUpgrade)
        {
            var meetsRequirements = true;
            var requirements = new GuiBindingList<string>();
            var requirementColors = new GuiBindingList<GuiColor>();
            var requirementIcons = new GuiBindingList<string>();
            var requirementTooltips = new GuiBindingList<string>();

            if (nextUpgrade == null)
            {
                requirements.Add("MAXED");
                requirementColors.Add(GuiColor.Green);
                requirementIcons.Add(PerkRequirementCategoryResolver.MaxedIcon);
                requirementTooltips.Add("This perk is fully upgraded.");
            }
            else
            {
                foreach (var req in nextUpgrade.Requirements)
                {
                    requirements.Add(req.RequirementText);

                    var categoryDetail = PerkRequirementCategoryResolver.GetDetail(req.Category);
                    var error = req.CheckRequirements(Player);
                    var met = string.IsNullOrWhiteSpace(error);

                    // Show the red locked icon when the requirement is not met.
                    requirementIcons.Add(categoryDetail.GetIcon(met));

                    if (met)
                    {
                        requirementColors.Add(GuiColor.Green);
                        requirementTooltips.Add($"{categoryDetail.Name}: {req.RequirementText} - met.");
                    }
                    else
                    {
                        requirementColors.Add(GuiColor.Red);
                        requirementTooltips.Add($"{categoryDetail.Name}: {req.RequirementText} - {error}");
                        meetsRequirements = false;
                    }
                }

                if (nextUpgrade.Requirements.Count <= 0)
                {
                    requirements.Add("None");
                    requirementColors.Add(GuiColor.Green);
                    requirementIcons.Add(PerkRequirementCategoryResolver.MetIcon);
                    requirementTooltips.Add("This upgrade has no requirements.");
                }
            }

            return (meetsRequirements, requirements, requirementColors, requirementIcons, requirementTooltips);
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

            var currentUpgrade = detail.PerkLevels.ContainsKey(rank)
                ? detail.PerkLevels[rank]
                : null;
            var nextUpgrade = detail.PerkLevels.ContainsKey(rank + 1)
                ? detail.PerkLevels[rank + 1]
                : null;

            var selectedDetails = BuildSelectedPerkDetailText(detail, currentUpgrade, nextUpgrade, rank);

            var (meetsRequirements, requirements, requirementColors, requirementIcons, requirementTooltips) = BuildRequirements(nextUpgrade);
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
                               dbPlayer.DatePerkRefundAvailable <= DateTime.UtcNow) &&
                              Currency.GetCurrency(Player, CurrencyType.PerkRefundToken) > 0 &&
                              currentUpgrade != null;
        }

        private void GrantFeats(PerkType perkType, int rank)
        {
            var target = IsInMyPerksMode ? Player : GetAssociate(AssociateType.Henchman, Player);
            if (!GetIsObjectValid(target))
                return;

            var previousActiveAbilityFeats = Perk.GetCurrentActiveAbilityFeats(perkType, rank - 1);
            var currentActiveAbilityFeats = Perk.GetCurrentActiveAbilityFeats(perkType, rank);

            Perk.SyncGrantedFeats(target, perkType, rank, true);
            SyncHotBarActiveAbilityFeats(previousActiveAbilityFeats, currentActiveAbilityFeats);

            if (rank == 1)
            {
                foreach (var actionMode in Perk.GetPerkDetails(perkType).HotBarActionModes)
                {
                    AddModeToggleToHotBar(actionMode);
                }
            }
        }

        private void SyncHotBarActiveAbilityFeats(
            IReadOnlyList<FeatType> previousActiveAbilityFeats,
            IReadOnlyList<FeatType> currentActiveAbilityFeats)
        {
            if (!IsInMyPerksMode)
                return;

            var currentFeats = currentActiveAbilityFeats
                .Where(CanAddFeatToHotBar)
                .Distinct()
                .ToList();
            var previousFeats = previousActiveAbilityFeats
                .Where(CanAddFeatToHotBar)
                .Distinct()
                .ToList();
            var replacedFeats = previousFeats
                .Except(currentFeats)
                .ToHashSet();
            var addedFeats = currentFeats
                .Except(previousFeats)
                .ToList();
            var replacementIndex = 0;

            if (replacedFeats.Count > 0)
            {
                for (var slot = 0; slot < TotalHotBarSlots; slot++)
                {
                    var quickBarSlot = PlayerPlugin.GetQuickBarSlot(Player, slot);
                    if (!IsFeatHotBarSlot(quickBarSlot, replacedFeats))
                        continue;

                    if (replacementIndex < currentFeats.Count)
                    {
                        PlayerPlugin.SetQuickBarSlot(Player, slot, PlayerQuickBarSlot.UseFeat(currentFeats[replacementIndex]));
                        replacementIndex++;
                    }
                    else
                    {
                        PlayerPlugin.SetQuickBarSlot(Player, slot, PlayerQuickBarSlot.Empty(QuickBarSlotType.Empty));
                    }
                }
            }

            foreach (var feat in addedFeats)
            {
                AddFeatToHotBar(feat);
            }
        }

        private static bool CanAddFeatToHotBar(FeatType feat)
        {
            return Ability.IsFeatRegistered(feat) &&
                   Ability.GetAbilityDetail(feat).ImpactAction != null;
        }

        private static bool IsFeatHotBarSlot(QuickBarSlot quickBarSlot, IReadOnlySet<FeatType> feats)
        {
            return quickBarSlot.ObjectType == QuickBarSlotType.Feat &&
                   feats.Contains((FeatType)quickBarSlot.INTParam1);
        }

        private bool IsFeatOnHotBar(FeatType feat)
        {
            for (var slot = 0; slot < TotalHotBarSlots; slot++)
            {
                var quickBarSlot = PlayerPlugin.GetQuickBarSlot(Player, slot);
                if (quickBarSlot.ObjectType == QuickBarSlotType.Feat &&
                    quickBarSlot.INTParam1 == (int)feat)
                {
                    return true;
                }
            }

            return false;
        }

        private void AddFeatToHotBar(FeatType feat)
        {
            if (!IsInMyPerksMode)
                return;

            if (IsFeatOnHotBar(feat))
                return;

            var qbs = PlayerQuickBarSlot.UseFeat(feat);

            // Try to add the new feat to the player's hotbar.
            for (var slot = 0; slot < AutoAddHotBarSlots; slot++)
            {
                if (PlayerPlugin.GetQuickBarSlot(Player, slot).ObjectType != QuickBarSlotType.Empty)
                    continue;

                PlayerPlugin.SetQuickBarSlot(Player, slot, qbs);
                return;
            }
        }

        private void AddModeToggleToHotBar(ActionMode mode)
        {
            if (!IsInMyPerksMode)
                return;

            if (IsModeOnHotBar(mode))
                return;

            var quickBarSlot = PlayerQuickBarSlot.ToggleMode((int)mode);
            for (var slot = 0; slot < AutoAddHotBarSlots; slot++)
            {
                if (PlayerPlugin.GetQuickBarSlot(Player, slot).ObjectType != QuickBarSlotType.Empty)
                    continue;

                PlayerPlugin.SetQuickBarSlot(Player, slot, quickBarSlot);
                return;
            }
        }

        private bool IsModeOnHotBar(ActionMode mode)
        {
            for (var slot = 0; slot < TotalHotBarSlots; slot++)
            {
                if (IsModeHotBarSlot(PlayerPlugin.GetQuickBarSlot(Player, slot), mode))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsModeHotBarSlot(QuickBarSlot quickBarSlot, ActionMode mode)
        {
            return quickBarSlot.ObjectType == QuickBarSlotType.ModeToggle &&
                   quickBarSlot.INTParam1 == (int)mode;
        }

        private void RemoveModeToggleFromHotBar(ActionMode mode)
        {
            if (!IsInMyPerksMode)
                return;

            for (var slot = 0; slot < TotalHotBarSlots; slot++)
            {
                var quickBarSlot = PlayerPlugin.GetQuickBarSlot(Player, slot);
                if (IsModeHotBarSlot(quickBarSlot, mode))
                {
                    PlayerPlugin.SetQuickBarSlot(
                        Player,
                        slot,
                        PlayerQuickBarSlot.Empty(QuickBarSlotType.Empty));
                }
            }
        }

        private void RemoveFeatsFromHotBar(IEnumerable<FeatType> feats)
        {
            if (!IsInMyPerksMode)
                return;

            var featSet = feats.ToHashSet();
            if (featSet.Count <= 0)
                return;

            for (var slot = 0; slot < TotalHotBarSlots; slot++)
            {
                var quickBarSlot = PlayerPlugin.GetQuickBarSlot(Player, slot);
                if (IsFeatHotBarSlot(quickBarSlot, featSet))
                {
                    PlayerPlugin.SetQuickBarSlot(Player, slot, PlayerQuickBarSlot.Empty(QuickBarSlotType.Empty));
                }
            }
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
                    action(target);
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
                        : detail.PurchaseRequirement(Player);

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
                    GrantFeats(selectedPerk, newRank);
                    ApplyPurchasePerkTriggers(newRank, selectedPerk);

                    FloatingTextStringOnCreature(ColorToken.Green($"You purchase '{detail.Name}' rank {newRank}."), Player, false);

                    EventsPlugin.SignalEvent("SWLOR_BUY_PERK", Player);
                    Gui.PublishRefreshEvent(Player, new PerkAcquiredRefreshEvent(selectedPerk));

                    ExportSingleCharacter(Player);

                    // Update UI with latest upgrade changes.
                    LoadDetails();

                    var currentUpgrade = detail.PerkLevels.ContainsKey(newRank)
                        ? detail.PerkLevels[newRank]
                        : null;
                    nextUpgrade = detail.PerkLevels.ContainsKey(newRank + 1)
                        ? detail.PerkLevels[newRank + 1]
                        : null;
                    SelectedDetails = BuildSelectedPerkDetailText(detail, currentUpgrade, nextUpgrade, newRank);
                    PerkButtonTexts[_selectedPerkIndex] = $"{detail.Name} ({newRank}/{detail.PerkLevels.Count})";

                    var (meetsRequirements, requirements, requirementColors, requirementIcons, requirementTooltips) = BuildRequirements(nextUpgrade);
                    var (_, chipColor, rowIcon, rowTooltip) = GetPerkRowStatus(detail, newRank, unallocatedSP);

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
                        : perkDetail.RefundRequirement(target);
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
                    var feats = perkDetail.PerkLevels.Values.SelectMany(s => s.GrantedFeats).ToList();
                    foreach (var feat in feats)
                    {
                        CreaturePlugin.RemoveFeat(target, feat);
                    }

                    Perk.RemoveStatusEffectsOnPerkRefund(target, selectedPerk);
                    RemoveFeatsFromHotBar(feats);

                    foreach (var actionMode in perkDetail.HotBarActionModes)
                    {
                        RemoveModeToggleFromHotBar(actionMode);
                    }

                    // Run all of the triggers related to refunding this perk.
                    foreach (var action in perkDetail.RefundedTriggers)
                    {
                        action(target);
                    }

                    ExportSingleCharacter(Player);

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
    }
}
