using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DialogService;
using SWLOR.Game.Server.Service.PerkService;
using Player = SWLOR.Game.Server.Entity.Player;
using Skill = SWLOR.Game.Server.Service.Skill;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature.DialogDefinition
{
    public class ViewPerksDialog: DialogBase
    {
        private class Model
        {
            public PerkCategoryType SelectedCategory { get; set; }
            public PerkType SelectedPerk { get; set; }
            public bool IsConfirmingPurchase { get; set; }
        }

        private const string MainPageId = "MAIN";
        private const string CategoryPageId = "CATEGORY";
        private const string PerkListPageId = "PERK_LIST";
        private const string PerkDetailsPageId = "PERK_DETAILS";
        private const string ViewMyPerksPageId = "VIEW_MY_PERKS";

        public override PlayerDialog SetUp(uint player)
        {
            var builder = new DialogBuilder()
                .WithDataModel(new Model())
                .AddBackAction((oldPage, newPage) =>
                {
                    var model = GetDataModel<Model>();
                    model.IsConfirmingPurchase = false;
                })
                .AddPage(MainPageId, MainPageInit)
                .AddPage(CategoryPageId, CategoryPageInit)
                .AddPage(PerkListPageId, PerkListPageInit)
                .AddPage(PerkDetailsPageId, PerkDetailsPageInit)
                .AddPage(ViewMyPerksPageId, ViewMyPerksPageInit);

            return builder.Build();
        }

        private void MainPageInit(DialogPage page)
        {
            var player = GetPC();
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
            var perkCount = dbPlayer.Perks.Count;

            page.Header = ColorToken.Green("Total SP: ") + dbPlayer.TotalSPAcquired + " / " + Skill.SkillCap + "\n" +
                   ColorToken.Green("Available SP: ") + dbPlayer.UnallocatedSP + "\n" +
                   ColorToken.Green("Total Perks: ") + perkCount + "\n";

            page.AddResponse("View My Perks", () => ChangePage(ViewMyPerksPageId));
            page.AddResponse("Buy Perks", () => ChangePage(CategoryPageId));
        }

        private void CategoryPageInit(DialogPage page)
        {
            void SelectCategory(PerkCategoryType category)
            {
                var model = GetDataModel<Model>();
                model.SelectedCategory = category;

                ChangePage(PerkListPageId);
            }

            page.Header = "Please select a category.";

            var player = GetPC();
            var categoriesAvailable = GetPerksAvailableToPC(player)
                .Select(s => s.Category)
                .Distinct();
            
            foreach(var category in categoriesAvailable)
            {
                var categoryDetail = Perk.GetPerkCategoryDetails(category);

                page.AddResponse(categoryDetail.Name, () => SelectCategory(category));
            }

        }

        private void PerkListPageInit(DialogPage page)
        {
            var model = GetDataModel<Model>();
            void SelectPerk(PerkType perk)
            {
                model.SelectedPerk = perk;

                ChangePage(PerkDetailsPageId);
            }

            page.Header = "Please select a perk.";

            var player = GetPC();
            var perksAvailable = GetPerksAvailableToPC(player)
                .Where(x => x.Category == model.SelectedCategory);
            foreach (var perk in perksAvailable)
            {
                page.AddResponse(perk.Name, () => SelectPerk(perk.Type));
            }
        }

        private void PerkDetailsPageInit(DialogPage page)
        {
            var player = GetPC();
            var model = GetDataModel<Model>();
            var perkDetail = Perk.GetPerkDetails(model.SelectedPerk);
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
            var categoryDetail = Perk.GetPerkCategoryDetails(model.SelectedCategory);
            var rank = dbPlayer.Perks.ContainsKey(model.SelectedPerk) ? dbPlayer.Perks[model.SelectedPerk] : 0;
            var maxRank = perkDetail.PerkLevels.Count;
            var currentLevel = perkDetail.PerkLevels.ContainsKey(rank) ? perkDetail.PerkLevels[rank] : null;
            var nextLevel = perkDetail.PerkLevels.ContainsKey(rank + 1) ? perkDetail.PerkLevels[rank + 1] : null;
            var price = nextLevel == null ? "N/A" : $"{nextLevel.Price} SP (Available: {dbPlayer.UnallocatedSP} SP)";
            var meetsRequirements = true;

            string BuildPerkSection(bool isCurrent, PerkLevel perkLevel)
            {
                var currentOrNext = isCurrent ? "Current" : "Next";
                var bonus = perkLevel == null ? "N/A" : perkLevel.Description;

                return ColorToken.Green($"{currentOrNext} Bonus: ") + bonus + "\n";
            }

            string BuildRequirements(List<IPerkRequirement> requirements)
            {
                if (requirements == null) return string.Empty;

                string text = ColorToken.Green("Requirements:\n\n");

                if (requirements.Count <= 0)
                    text += "N/A";

                foreach (var req in requirements)
                {
                    var requirementMet = string.IsNullOrWhiteSpace(req.CheckRequirements(player));
                    var reqText = requirementMet ? ColorToken.Green(req.RequirementText) : ColorToken.Red(req.RequirementText);
                    text += reqText + "\n";

                    if (!requirementMet) meetsRequirements = false;
                }

                return text;
            }

            void PurchaseUpgrade()
            {
                if (model.IsConfirmingPurchase)
                {
                    model.IsConfirmingPurchase = false;
                    DoPerkUpgrade();
                }
                else
                {
                    model.IsConfirmingPurchase = true;
                }
            }

            bool CanUpgrade()
            {
                if (!meetsRequirements) return false;
                if (rank + 1 > maxRank) return false;
                if (nextLevel == null) return false;
                if (dbPlayer.UnallocatedSP < nextLevel.Price) return false;

                return true;
            }

            void DoPerkUpgrade()
            {
                if (!CanUpgrade() || nextLevel == null)
                {
                    FloatingTextStringOnCreature("You do not meet the requirements to purchase this perk upgrade.", player, false);
                    return;
                }

                int perkRank = dbPlayer.Perks.ContainsKey(model.SelectedPerk) ? dbPlayer.Perks[model.SelectedPerk] : 0;
                dbPlayer.Perks[model.SelectedPerk] = perkRank + 1;
                dbPlayer.UnallocatedSP -= nextLevel.Price;
                DB.Set(playerId, dbPlayer);

                GrantFeats();
                ApplyPurchasePerkTriggers(dbPlayer.Perks[model.SelectedPerk]);

                Events.SignalEvent("SWLOR_BUY_PERK", player);
            }

            void GrantFeats()
            {
                foreach (var feat in nextLevel.GrantedFeats)
                {
                    if (GetHasFeat(feat, player)) continue;

                    // If feat isn't registered or the ability doesn't have an impact action,
                    // don't add the feat to the player's hot bar.
                    if (!Ability.IsFeatRegistered(feat)) continue;
                    var abilityDetail = Ability.GetAbilityDetail(feat);
                    if (abilityDetail.ImpactAction == null) continue;

                    Creature.AddFeatByLevel(player, feat, 1);
                    AddFeatToHotBar(feat);
                }
            }

            void AddFeatToHotBar(FeatType feat)
            {
                var qbs = PlayerQuickBarSlot.UseFeat(feat);

                // Try to add the new feat to the player's hotbar.
                if (Core.NWNX.Player.GetQuickBarSlot(player, 0).ObjectType == QuickBarSlotType.Empty)
                    Core.NWNX.Player.SetQuickBarSlot(player, 0, qbs);
                else if (Core.NWNX.Player.GetQuickBarSlot(player, 1).ObjectType == QuickBarSlotType.Empty)
                    Core.NWNX.Player.SetQuickBarSlot(player, 1, qbs);
                else if (Core.NWNX.Player.GetQuickBarSlot(player, 2).ObjectType == QuickBarSlotType.Empty)
                    Core.NWNX.Player.SetQuickBarSlot(player, 2, qbs);
                else if (Core.NWNX.Player.GetQuickBarSlot(player, 3).ObjectType == QuickBarSlotType.Empty)
                    Core.NWNX.Player.SetQuickBarSlot(player, 3, qbs);
                else if (Core.NWNX.Player.GetQuickBarSlot(player, 4).ObjectType == QuickBarSlotType.Empty)
                    Core.NWNX.Player.SetQuickBarSlot(player, 4, qbs);
                else if (Core.NWNX.Player.GetQuickBarSlot(player, 5).ObjectType == QuickBarSlotType.Empty)
                    Core.NWNX.Player.SetQuickBarSlot(player, 5, qbs);
                else if (Core.NWNX.Player.GetQuickBarSlot(player, 6).ObjectType == QuickBarSlotType.Empty)
                    Core.NWNX.Player.SetQuickBarSlot(player, 6, qbs);
                else if (Core.NWNX.Player.GetQuickBarSlot(player, 7).ObjectType == QuickBarSlotType.Empty)
                    Core.NWNX.Player.SetQuickBarSlot(player, 7, qbs);
                else if (Core.NWNX.Player.GetQuickBarSlot(player, 8).ObjectType == QuickBarSlotType.Empty)
                    Core.NWNX.Player.SetQuickBarSlot(player, 8, qbs);
                else if (Core.NWNX.Player.GetQuickBarSlot(player, 9).ObjectType == QuickBarSlotType.Empty)
                    Core.NWNX.Player.SetQuickBarSlot(player, 9, qbs);
                else if (Core.NWNX.Player.GetQuickBarSlot(player, 10).ObjectType == QuickBarSlotType.Empty)
                    Core.NWNX.Player.SetQuickBarSlot(player, 10, qbs);
            }

            // Applies any Purchase triggers associated with this perk.
            void ApplyPurchasePerkTriggers(int perkLevel)
            {
                if (perkDetail.PurchasedTriggers.Count > 0)
                {
                    foreach (var action in perkDetail.PurchasedTriggers)
                    {
                        action(player, model.SelectedPerk, perkLevel);
                    }
                }
            }

            var currentPerkLevelDetails = BuildPerkSection(true, currentLevel);
            var nextPerkLevelDetails = BuildPerkSection(false, nextLevel);
            var requirementsSection = BuildRequirements(nextLevel?.Requirements);
            page.Header = ColorToken.Green("Name: ") + perkDetail.Name + "\n" +
                          ColorToken.Green("Description: ") + perkDetail.Description + "\n" +
                          ColorToken.Green("Category: ") + categoryDetail.Name + "\n" +
                          ColorToken.Green("Rank: ") + rank + " / " + maxRank + "\n" +
                          ColorToken.Green("Price: ") + price + "\n" + 
                          currentPerkLevelDetails + "\n" +
                          nextPerkLevelDetails + "\n" +
                          requirementsSection;

            if (CanUpgrade())
            {
                page.AddResponse(model.IsConfirmingPurchase ? "CONFIRM PURCHASE" : "Purchase Upgrade", PurchaseUpgrade);
            }
        }

        private void ViewMyPerksPageInit(DialogPage page)
        {
            var player = GetPC();
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
            var header = ColorToken.Green("Perks purchased:\n\n");

            foreach (var pcPerk in dbPlayer.Perks)
            {
                var perkDetail = Perk.GetPerkDetails(pcPerk.Key);
                header += $"{perkDetail.Name} (Lvl. {pcPerk.Value})\n";
            }

            page.Header = header;
        }

        private static IEnumerable<PerkDetail> GetPerksAvailableToPC(uint player)
        {
            var activePerks = Perk.GetAllActivePerks();

            foreach (var perk in activePerks.Values)
            {
                // Determination for whether a player can see a perk in the menu is based on whether they meet the
                // requirements for the first level in that perk.
                var perkLevel = perk.PerkLevels[1];
                var meetsRequirements = true;

                foreach (var req in perkLevel.Requirements)
                {
                    var meetsRequirement = string.IsNullOrWhiteSpace(req.CheckRequirements(player));
                    if (!meetsRequirement)
                        meetsRequirements = false;
                }

                if(meetsRequirements)
                    yield return perk;
            }
        }
    }
}
