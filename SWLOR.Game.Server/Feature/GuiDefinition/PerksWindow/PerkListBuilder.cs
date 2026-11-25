using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
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

namespace SWLOR.Game.Server.Feature.GuiDefinition.PerksWindow
{
    /// <summary>
    /// The perk list and header of the Perks window: the categories, the points and refund state of the selected mode, the filtered, sorted and
    /// paged rows, and the requirement rows of an upgrade. It writes the bindings through the host.
    /// </summary>
    public class PerkListBuilder
    {
        private ICurrencyService Currency { get; }
        private IDBService DB { get; }
        private IPerkService Perk { get; }
        private IPerkForceAffinityService PerkForceAffinity { get; }
        private ISkillService Skill { get; }

        public PerkListBuilder(ICurrencyService currency, IDBService db, IPerkService perk, IPerkForceAffinityService perkForceAffinity, ISkillService skill)
        {
            Currency = currency;
            DB = db;
            Perk = perk;
            PerkForceAffinity = perkForceAffinity;
            Skill = skill;
        }

        private const int ItemsPerPage = 30;

        public static (PerkRowStatus status, GuiColor color, string iconResref, string tooltip) GetPerkRowStatus(IPerksHost host, PerkDetail detail, int rank, int unallocatedSP)
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
                var error = req.CheckRequirements(host.Player);
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

        public void LoadCategories(IPerksHost host)
        {
            var groupType = host.IsInMyPerksMode ? PerkGroupType.Player : PerkGroupType.Beast;
            var categories = new GuiBindingList<GuiComboEntry>
            {
                new GuiComboEntry("<All Categories>", 0)
            };

            foreach (var (type, detail) in Perk.GetAllActivePerkCategories(groupType))
            {
                categories.Add(new GuiComboEntry(detail.Name, (int)type));
            }

            host.Categories = categories;
        }

        public void LoadDetails(IPerksHost host)
        {
            var playerId = GetObjectUUID(host.Player);
            var dbPlayer = DB.Get<Player>(playerId);
            var now = Clock.UtcNow;

            LoadForceAffinityDetails(host, dbPlayer);

            if (host.IsInMyPerksMode)
            {
                host.AvailableSP = $"Available SP: {dbPlayer.UnallocatedSP}";
                host.TotalSP = $"Total SP: {Skill.GetTotalSkillPoints(dbPlayer)} / {SkillService.TotalSkillPointCap}";
                host.WindowTitle = $"Perks - {dbPlayer.UnallocatedSP} SP available";
            }
            else if (host.IsInBeastPerksMode)
            {
                var dbBeast = DB.Get<Beast>(dbPlayer.ActiveBeastId);
                host.AvailableSP = $"Available SP: {dbBeast.UnallocatedSP}";
                host.TotalSP = $"Total SP: {dbBeast.Level} / {BeastMasteryService.MaxLevel}";
                host.WindowTitle = $"Beast Perks - {dbBeast.UnallocatedSP} SP available";
            }

            var dateRefundAvailable = dbPlayer.DatePerkRefundAvailable ?? now;
            var isRefundAvailable = dateRefundAvailable <= now;
            var dateRefundAvailableText = isRefundAvailable
                ? "Now"
                : Time.GetTimeToWaitLongIntervals(now, dateRefundAvailable, true);
            host.ResetNextAvailable = $"Reset Available: {dateRefundAvailableText} [# Available: {Currency.GetCurrency(host.Player, CurrencyType.PerkRefundToken)}]";
            host.IsRefundEnabled = false;
            host.HasBeast = !string.IsNullOrWhiteSpace(dbPlayer.ActiveBeastId);
        }

        private void LoadForceAffinityDetails(IPerksHost host, Player dbPlayer)
        {
            // Shown while browsing as well as in My Perks: affinity decides whether an opposing
            // power lands at half strength, so players need it before they spend SP, not after.
            host.IsForceAffinityVisible = !host.IsInBeastPerksMode &&
                                     dbPlayer.CharacterType == CharacterType.ForceSensitive;
            if (!host.IsForceAffinityVisible)
            {
                host.ForceAffinityHeading = string.Empty;
                host.ForceAffinityColor = GuiColor.White;
                return;
            }

            var affinity = PerkForceAffinity.GetForceAffinity(host.Player);
            var affinityLabel = PerkDetailText.FormatForceAffinity(affinity);
            host.ForceAffinityHeading = $"FORCE AFFINITY: {affinityLabel}";
            host.ForceAffinityColor = affinity > 0
                ? new GuiColor(120, 190, 255)
                : affinity < 0
                    ? new GuiColor(225, 100, 100)
                    : new GuiColor(221, 181, 93);
        }

        public void LoadPerks(IPerksHost host)
        {
            if (!host.InitialLoadDone) return;

            var playerId = GetObjectUUID(host.Player);
            var dbPlayer = DB.Get<Player>(playerId);
            var dbBeast = host.IsInBeastPerksMode
                ? DB.Get<Beast>(dbPlayer.ActiveBeastId)
                : null;

            host.FilteredPerks.Clear();
            host.SelectedPerkIndex = -1;

            var perkButtonColors = new GuiBindingList<GuiColor>();
            var perkButtonIcons = new GuiBindingList<string>();
            var perkButtonTexts = new GuiBindingList<string>();
            var perkDetailSelected = new GuiBindingList<bool>();
            var perkRowReqIcons = new GuiBindingList<string>();
            var perkRowReqTooltips = new GuiBindingList<string>();
            var perkRowCosts = new GuiBindingList<string>();
            var pageNumbers = new GuiBindingList<GuiComboEntry>();

            var group = host.IsInMyPerksMode
                ? PerkGroupType.Player
                : PerkGroupType.Beast;
            var perkList = host.SelectedPerkCategoryId == 0
                ? Perk.GetAllActivePerks(group)
                : Perk.GetActivePerksInCategory(group, (PerkCategoryType)host.SelectedPerkCategoryId);

            // Filter down to just perks with a name partially matching the search text
            if (!string.IsNullOrWhiteSpace(host.SearchText))
            {
                perkList = perkList.Where(x => x.Value.Name.ToLower().Contains(host.SearchText.ToLower()))
                    .ToDictionary(x => x.Key, y => y.Value);
            }

            var sortedPerks = SortPerks(host, perkList, dbPlayer, dbBeast).ToList();

            var unallocatedSP = host.IsInMyPerksMode
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
                    var rank = GetDisplayPerkRank(detail, GetCurrentPerkRank(host, dbPlayer, dbBeast, type));
                    var (status, color, iconResref, tooltip) = GetPerkRowStatus(host, detail, rank, unallocatedSP);
                    state = (rank, status, color, iconResref, tooltip);
                    rowStateCache[type] = state;
                }

                return state;
            }

            // Apply the status filter to the full sorted list before pagination so that
            // pages, page numbers, and the selection index list all reflect the filtered set.
            if (host.SelectedStatusFilter != 0)
            {
                sortedPerks = sortedPerks.Where(x =>
                {
                    var (rank, status, _, _, _) = GetRowState(x.Key, x.Value);

                    return host.SelectedStatusFilter switch
                    {
                        1 => rank >= 1,
                        2 => status == PerkRowStatus.Buyable,
                        3 => status == PerkRowStatus.Maxed,
                        _ => true
                    };
                }).ToList();
            }

            host.Pages = sortedPerks.Count / ItemsPerPage + (sortedPerks.Count % ItemsPerPage == 0 ? 0 : 1);

            for (var x = 1; x <= host.Pages; x++)
            {
                pageNumbers.Add(new GuiComboEntry($"Page {x}", x));
            }

            // Paginate the results
            var pagedPerks = sortedPerks
                .Skip((host.SelectedPage - 1) * ItemsPerPage)
                .Take(ItemsPerPage);

            foreach (var (type, detail) in pagedPerks)
            {
                var (rank, _, color, iconResref, tooltip) = GetRowState(type, detail);

                host.FilteredPerks.Add(type);
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

            host.PerkButtonColors = perkButtonColors;
            host.PerkButtonIcons = perkButtonIcons;
            host.PerkButtonTexts = perkButtonTexts;
            host.PerkDetailSelected = perkDetailSelected;
            host.PerkRowReqIcons = perkRowReqIcons;
            host.PerkRowReqTooltips = perkRowReqTooltips;
            host.PerkRowCosts = perkRowCosts;
            host.PageNumbers = pageNumbers;

            // Select the first perk so the detail panel shows content immediately
            // instead of leaving an empty pane when the list loads or is re-filtered.
            if (host.FilteredPerks.Count > 0)
            {
                host.SelectPerkAt(0);
            }
        }

        private static IEnumerable<KeyValuePair<PerkType, PerkDetail>> SortPerks(
            IPerksHost host,
            IEnumerable<KeyValuePair<PerkType, PerkDetail>> perks,
            Player dbPlayer,
            Beast dbBeast)
        {
            var sortOrder = (PerkSortOrder)host.SelectedSortOrderId;

            return sortOrder switch
            {
                PerkSortOrder.AlphabeticalDescending => perks
                    .OrderByDescending(x => x.Value.Name, StringComparer.OrdinalIgnoreCase),
                PerkSortOrder.SkillLevelAscending => perks
                    .OrderBy(x => GetRequiredSkillLevelSortOrder(x.Value, GetCurrentPerkRank(host, dbPlayer, dbBeast, x.Key)))
                    .ThenBy(x => x.Value.Name, StringComparer.OrdinalIgnoreCase),
                PerkSortOrder.SkillLevelDescending => perks
                    .OrderByDescending(x => GetRequiredSkillLevelSortOrder(x.Value, GetCurrentPerkRank(host, dbPlayer, dbBeast, x.Key)))
                    .ThenBy(x => x.Value.Name, StringComparer.OrdinalIgnoreCase),
                _ => perks.OrderBy(x => x.Value.Name, StringComparer.OrdinalIgnoreCase)
            };
        }

        public static int GetCurrentPerkRank(IPerksHost host, Player dbPlayer, Beast dbBeast, PerkType perkType)
        {
            if (host.IsInMyPerksMode)
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

        internal static int GetDisplayPerkRank(PerkDetail detail, int savedRank)
        {
            // Characters awaiting a rebuild can retain ranks removed from the definition.
            // Only normalize the UI; preserve their saved ranks for the rebuild.
            return Math.Clamp(savedRank, 0, detail.PerkLevels.Count);
        }

        internal static int GetRequiredSkillLevelSortOrder(PerkDetail detail, int rank)
        {
            rank = GetDisplayPerkRank(detail, rank);
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

        public static (bool meetsRequirements,
            GuiBindingList<string> texts,
            GuiBindingList<GuiColor> colors,
            GuiBindingList<string> icons,
            GuiBindingList<string> tooltips) BuildRequirements(IPerksHost host, PerkLevel nextUpgrade)
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
                    var error = req.CheckRequirements(host.Player);
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
    }
}
