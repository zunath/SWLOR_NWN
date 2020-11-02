using System;
using System.Linq;
using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.GameObject;
using SWLOR.Game.Server.Legacy.Service;
using SWLOR.Game.Server.Legacy.ValueObject.Dialog;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Legacy.Conversation
{
    public class ViewPerks : ConversationBase
    {
        private class Model
        {
            public int SelectedCategoryID { get; set; }
            public int SelectedPerkID { get; set; }
            public bool IsConfirmingPurchase { get; set; }
        }

        public override PlayerDialog SetUp(NWPlayer player)
        {
            var dialog = new PlayerDialog("MainPage");
            var mainPage = new DialogPage(
                "<SET LATER>",
                "View My Perks",
                "Buy Perks"
            );

            var categoryPage = new DialogPage(
                "Please select a category. Additional options will appear as you increase your skill ranks."
            );

            var perkListPage = new DialogPage(
                "Please select a perk. Additional options will appear as you increase your skill ranks."
            );

            var perkDetailsPage = new DialogPage(
                "<SET LATER>",
                "Purchase Upgrade"
            );

            var viewMyPerksPage = new DialogPage(
                "<SET LATER>"
            );

            dialog.AddPage("MainPage", mainPage);
            dialog.AddPage("CategoryPage", categoryPage);
            dialog.AddPage("PerkListPage", perkListPage);
            dialog.AddPage("PerkDetailsPage", perkDetailsPage);
            dialog.AddPage("ViewMyPerksPage", viewMyPerksPage);
            return dialog;
        }

        public override void Initialize()
        {
            SetPageHeader("MainPage", GetMainPageHeader());
        }

        private string GetMainPageHeader()
        {
            var pcEntity = PlayerService.GetPlayerEntity(GetPC().GlobalID);

            var totalSP = SkillService.GetPCTotalSkillCount(GetPC());
            var totalPerks = PerkService.GetPCTotalPerkCount(GetPC().GlobalID);

            return ColorToken.Green("Total SP: ") + totalSP + " / " + SkillService.SkillCap + "\n" +
                    ColorToken.Green("Available SP: ") + pcEntity.UnallocatedSP + "\n" +
                    ColorToken.Green("Total Perks: ") + totalPerks + "\n";
        }

        private void BuildViewMyPerks()
        {
            var perks = DataService.PCPerk.GetAllByPlayerID(GetPC().GlobalID).ToList();

            var header = ColorToken.Green("Perks purchased:") + "\n\n";
            foreach (var pcPerk in perks)
            {
                var perk = DataService.Perk.GetByID(pcPerk.PerkID);
                header += perk.Name + " (Lvl. " + pcPerk.PerkLevel + ") \n";
            }

            SetPageHeader("ViewMyPerksPage", header);
        }


        private void BuildCategoryList()
        {
            var perksAvailable = PerkService.GetPerksAvailableToPC(GetPC());
            var categoryIDs = perksAvailable.Select(x => x.PerkCategoryID).Distinct();
            var categories = DataService.PerkCategory.GetAllByIDs(categoryIDs).ToList();

            ClearPageResponses("CategoryPage");
            foreach (var category in categories)
            {
                AddResponseToPage("CategoryPage", category.Name, true, category.ID);
            }
        }

        private void BuildPerkList()
        {
            var vm = GetDialogCustomData<Model>();
            var perksAvailable = PerkService.GetPerksAvailableToPC(GetPC());
            var perks = perksAvailable.Where(x => x.PerkCategoryID == vm.SelectedCategoryID).ToList();

            ClearPageResponses("PerkListPage");
            foreach (var perk in perks)
            {
                AddResponseToPage("PerkListPage", perk.Name, true, perk.ID);
            }
        }

        private void BuildPerkDetails()
        {
            var vm = GetDialogCustomData<Model>();
            var perk = PerkService.GetPerkByID(vm.SelectedPerkID);
            var pcPerk = PerkService.GetPCPerkByID(GetPC().GlobalID, perk.ID);
            var player = PlayerService.GetPlayerEntity(GetPC().GlobalID);
            var perkLevels = DataService.PerkLevel.GetAllByPerkID(perk.ID).ToList();

            var rank = pcPerk?.PerkLevel ?? 0;
            var maxRank = perkLevels.Count();
            var currentBonus = "N/A";
            var currentFPCost = string.Empty;
            var currentConcentrationCost = string.Empty;
            var currentSpecializationRequired = "None";
            var nextBonus = "N/A";
            var nextFPCost = "N/A";
            var nextConcentrationCost = string.Empty;
            var price = "N/A";
            var nextSpecializationRequired = "None";
            var currentPerkLevel = PerkService.FindPerkLevel(perkLevels, rank);
            var nextPerkLevel = PerkService.FindPerkLevel(perkLevels, rank + 1);
            SetResponseVisible("PerkDetailsPage", 1, PerkService.CanPerkBeUpgraded(GetPC(), vm.SelectedPerkID));

            // Player has purchased at least one rank in this perk. Show their current bonuses.
            if (rank > 0 && currentPerkLevel != null)
            {
                var currentPerkFeat = DataService.PerkFeat.GetByPerkIDAndLevelUnlockedOrDefault(vm.SelectedPerkID, currentPerkLevel.Level);
                currentBonus = currentPerkLevel.Description;

                // Not every perk is going to have a perk feat. Don't display this information if not necessary.
                if (currentPerkFeat != null)
                {
                    currentFPCost = currentPerkFeat.BaseFPCost > 0 ? (ColorToken.Green("Current FP: ") + currentPerkFeat.BaseFPCost + "\n") : string.Empty;

                    // If this perk level has a concentration cost and interval, display it on the menu.
                    if (currentPerkFeat.ConcentrationFPCost > 0 && currentPerkFeat.ConcentrationTickInterval > 0)
                    {
                        currentConcentrationCost = ColorToken.Green("Current Concentration FP: ") + currentPerkFeat.ConcentrationFPCost + " / " + currentPerkFeat.ConcentrationTickInterval + "s\n";
                    }
                }

                // If this perk level has required specialization, change the text to that.
                if (currentPerkLevel.SpecializationID > 0)
                {
                    // Convert ID to enum, then get the string of the enum value. If we ever get a specialization with
                    // more than one word, another process will need to be used.
                    currentSpecializationRequired = ((SpecializationType)currentPerkLevel.SpecializationID).ToString();
                }
            }

            // Player hasn't reached max rank and this perk has another perk level to display.
            if (rank + 1 <= maxRank && nextPerkLevel != null)
            {
                var nextPerkFeat = DataService.PerkFeat.GetByPerkIDAndLevelUnlockedOrDefault(vm.SelectedPerkID, rank + 1);
                nextBonus = nextPerkLevel.Description;
                price = nextPerkLevel.Price + " SP (Available: " + player.UnallocatedSP + " SP)";

                if (nextPerkFeat != null)
                {
                    nextFPCost = nextPerkFeat.BaseFPCost > 0 ? (ColorToken.Green("Next FP: ") + nextPerkFeat.BaseFPCost + "\n") : string.Empty;

                    // If this perk level has a concentration cost and interval, display it on the menu.
                    if (nextPerkFeat.ConcentrationFPCost > 0 && nextPerkFeat.ConcentrationTickInterval > 0)
                    {
                        nextConcentrationCost = ColorToken.Green("Next Concentration FP: ") + nextPerkFeat.ConcentrationFPCost + " / " + nextPerkFeat.ConcentrationTickInterval + "s\n";
                    }
                }

                if (nextPerkLevel.SpecializationID > 0)
                {
                    nextSpecializationRequired = ((SpecializationType)nextPerkLevel.SpecializationID).ToString();
                }
            }
            var perkCategory = DataService.PerkCategory.GetByID(perk.PerkCategoryID);
            var cooldownCategory = perk.CooldownCategoryID == null ?
                null :
                DataService.CooldownCategory.GetByID(Convert.ToInt32(perk.CooldownCategoryID));

            var header = ColorToken.Green("Name: ") + perk.Name + "\n" +
                         ColorToken.Green("Category: ") + perkCategory.Name + "\n" +
                         ColorToken.Green("Rank: ") + rank + " / " + maxRank + "\n" +
                         ColorToken.Green("Price: ") + price + "\n" +
                         currentFPCost +
                         currentConcentrationCost +
                         (cooldownCategory != null && cooldownCategory.BaseCooldownTime > 0 ? ColorToken.Green("Cooldown: ") + cooldownCategory.BaseCooldownTime + "s" : "") + "\n" +
                         ColorToken.Green("Description: ") + perk.Description + "\n" +
                         ColorToken.Green("Current Bonus: ") + currentBonus + "\n" +
                         ColorToken.Green("Requires Specialization: ") + currentSpecializationRequired + "\n" +
                         nextFPCost +
                         nextConcentrationCost +
                         ColorToken.Green("Next Bonus: ") + nextBonus + "\n" +
                         ColorToken.Green("Requires Specialization: ") + nextSpecializationRequired + "\n";
                

            if (nextPerkLevel != null)
            {
                var requirements = DataService.PerkLevelSkillRequirement.GetAllByPerkLevelID(nextPerkLevel.ID).ToList();
                if (requirements.Count > 0)
                {
                    header += "\n" + ColorToken.Green("Next Upgrade Skill Requirements:\n\n");

                    var hasRequirement = false;
                    foreach (var req in requirements)
                    {
                        if (req.RequiredRank > 0)
                        {
                            var pcSkill = SkillService.GetPCSkill(GetPC(), req.SkillID);
                            var skill = SkillService.GetSkill(pcSkill.SkillID);

                            var detailLine = skill.Name + " Rank " + req.RequiredRank;

                            if (pcSkill.Rank >= req.RequiredRank)
                            {
                                header += ColorToken.Green(detailLine) + "\n";
                            }
                            else
                            {
                                header += ColorToken.Red(detailLine) + "\n";
                            }

                            hasRequirement = true;
                        }
                    }

                    if (requirements.Count <= 0 || !hasRequirement)
                    {
                        header += "None\n";
                    }
                }
            }

            SetPageHeader("PerkDetailsPage", header);

        }


        public override void DoAction(NWPlayer player, string pageName, int responseID)
        {
            switch (pageName)
            {
                case "MainPage":
                    HandleMainPageResponses(responseID);
                    break;
                case "CategoryPage":
                    HandleCategoryResponses(responseID);
                    break;
                case "PerkListPage":
                    HandlePerkListResponses(responseID);
                    break;
                case "PerkDetailsPage":
                    HandlePerkDetailsResponses(responseID);
                    break;

            }
        }

        public override void Back(NWPlayer player, string beforeMovePage, string afterMovePage)
        {
            switch (beforeMovePage)
            {
                case "PerkDetailsPage":
                    var vm = GetDialogCustomData<Model>();
                    vm.IsConfirmingPurchase = false;
                    SetResponseText("PerkDetailsPage", 1, "Purchase Upgrade");
                    BuildPerkList();
                    break;
            }
        }

        private void HandleMainPageResponses(int responseID)
        {
            switch (responseID)
            {
                case 1: // View My Perks
                    BuildViewMyPerks();
                    ChangePage("ViewMyPerksPage");
                    break;
                case 2: // Buy Perks
                    BuildCategoryList();
                    ChangePage("CategoryPage");
                    break;
            }
        }

        private void HandleCategoryResponses(int responseID)
        {
            var vm = GetDialogCustomData<Model>();
            var response = GetResponseByID("CategoryPage", responseID);

            vm.SelectedCategoryID = (int)response.CustomData;
            BuildPerkList();
            ChangePage("PerkListPage");
        }

        private void HandlePerkListResponses(int responseID)
        {
            var vm = GetDialogCustomData<Model>();
            var response = GetResponseByID("PerkListPage", responseID);

            vm.SelectedPerkID = (int)response.CustomData;
            BuildPerkDetails();
            ChangePage("PerkDetailsPage");
        }

        private void HandlePerkDetailsResponses(int responseID)
        {
            var vm = GetDialogCustomData<Model>();

            switch (responseID)
            {
                case 1: // Purchase Upgrade / Confirm Purchase

                    if (vm.IsConfirmingPurchase)
                    {
                        SetResponseText("PerkDetailsPage", 1, "Purchase Upgrade");
                        PerkService.DoPerkUpgrade(GetPC(), vm.SelectedPerkID);
                        vm.IsConfirmingPurchase = false;
                        BuildPerkDetails();
                        SetPageHeader("MainPage", GetMainPageHeader());
                    }
                    else
                    {
                        vm.IsConfirmingPurchase = true;
                        SetResponseText("PerkDetailsPage", 1, "CONFIRM PURCHASE");
                    }
                    break;
            }
        }

        public override void EndDialog()
        {
        }
    }
}
