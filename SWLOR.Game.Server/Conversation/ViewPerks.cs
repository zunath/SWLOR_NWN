using System;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.ValueObject.Dialog;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Perk;

namespace SWLOR.Game.Server.Conversation
{
    public class ViewPerks : ConversationBase
    {
        private class Model
        {
            public int SelectedCategoryID { get; set; }
            public PerkType SelectedPerkID { get; set; }
            public bool IsConfirmingPurchase { get; set; }
        }

        public override PlayerDialog SetUp(NWPlayer player)
        {
            PlayerDialog dialog = new PlayerDialog("MainPage");
            DialogPage mainPage = new DialogPage(
                "<SET LATER>",
                "View My Perks",
                "Buy Perks"
            );

            DialogPage categoryPage = new DialogPage(
                "Please select a category. Additional options will appear as you increase your skill ranks."
            );

            DialogPage perkListPage = new DialogPage(
                "Please select a perk. Additional options will appear as you increase your skill ranks."
            );

            DialogPage perkDetailsPage = new DialogPage(
                "<SET LATER>",
                "Purchase Upgrade"
            );

            DialogPage viewMyPerksPage = new DialogPage(
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
            Player pcEntity = PlayerService.GetPlayerEntity(GetPC().GlobalID);

            int totalSP = SkillService.GetPCTotalSkillCount(GetPC());
            int totalPerks = PerkService.GetPCTotalPerkCount(GetPC().GlobalID);

            return ColorTokenService.Green("Total SP: ") + totalSP + " / " + SkillService.SkillCap + "\n" +
                    ColorTokenService.Green("Available SP: ") + pcEntity.UnallocatedSP + "\n" +
                    ColorTokenService.Green("Total Perks: ") + totalPerks + "\n";
        }

        private void BuildViewMyPerks()
        {
            var player = DataService.Player.GetByID(GetPC().GlobalID);
            string header = ColorTokenService.Green("Perks purchased:") + "\n\n";
            foreach (var pcPerk in player.Perks)
            {
                var perk = PerkService.GetPerkHandler(pcPerk.Key);
                header += perk.Name + " (Lvl. " + pcPerk.Value + ") \n";
            }

            SetPageHeader("ViewMyPerksPage", header);
        }


        private void BuildCategoryList()
        {
            var perksAvailable = PerkService.GetPerksAvailableToPC(GetPC());
            var categoryIDs = perksAvailable.Select(x => x.Category).Distinct();
            List<PerkCategory> categories = new List<PerkCategory>();

            foreach (var id in categoryIDs)
            {
                categories.Add(PerkService.GetPerkCategory(id));
            }

            ClearPageResponses("CategoryPage");
            foreach (PerkCategory category in categories)
            {
                AddResponseToPage("CategoryPage", category.Name, true, (int)category.CategoryType);
            }
        }

        private void BuildPerkList()
        {
            Model vm = GetDialogCustomData<Model>();
            var perksAvailable = PerkService.GetPerksAvailableToPC(GetPC());
            List<IPerk> perks = perksAvailable.Where(x => (int)x.Category == vm.SelectedCategoryID).ToList();

            ClearPageResponses("PerkListPage");
            foreach (var perk in perks)
            {
                AddResponseToPage("PerkListPage", perk.Name, true, (int)perk.PerkType);
            }
        }

        private void BuildPerkDetails()
        {
            Model vm = GetDialogCustomData<Model>();
            var perk = PerkService.GetPerkHandler(vm.SelectedPerkID);
            var player = DataService.Player.GetByID(GetPC().GlobalID);
            var perkLevels = perk.PerkLevels;

            var rank = player.Perks.ContainsKey(vm.SelectedPerkID) ?
                player.Perks[vm.SelectedPerkID] : 
                0;

            int maxRank = perkLevels.Count();
            string currentBonus = "N/A";
            string currentFPCost = string.Empty;
            string currentConcentrationCost = string.Empty;
            string currentSpecializationRequired = "None";
            string nextBonus = "N/A";
            string nextFPCost = "N/A";
            string nextConcentrationCost = string.Empty;
            string price = "N/A";
            string nextSpecializationRequired = "None";
            PerkLevel currentPerkLevel = perkLevels.ContainsKey(rank) ? perkLevels[rank] : null;
            PerkLevel nextPerkLevel = perkLevels.ContainsKey(rank + 1) ? perkLevels[rank + 1] : null;
            SetResponseVisible("PerkDetailsPage", 1, PerkService.CanPerkBeUpgraded(GetPC(), vm.SelectedPerkID));

            // Player has purchased at least one rank in this perk. Show their current bonuses.
            if (rank > 0 && currentPerkLevel != null)
            {
                var currentPerkFeat = perk.PerkFeats.ContainsKey(rank) ? perk.PerkFeats[rank].First() : null;
                currentBonus = currentPerkLevel.Description;

                // Not every perk is going to have a perk feat. Don't display this information if not necessary.
                if (currentPerkFeat != null)
                {
                    currentFPCost = currentPerkFeat.BaseFPCost > 0 ? (ColorTokenService.Green("Current FP: ") + currentPerkFeat.BaseFPCost + "\n") : string.Empty;

                    // If this perk level has a concentration cost and interval, display it on the menu.
                    if (currentPerkFeat.ConcentrationFPCost > 0 && currentPerkFeat.ConcentrationTickInterval > 0)
                    {
                        currentConcentrationCost = ColorTokenService.Green("Current Concentration FP: ") + currentPerkFeat.ConcentrationFPCost + " / " + currentPerkFeat.ConcentrationTickInterval + "s\n";
                    }
                }

                // If this perk level has required specialization, change the text to that.
                if (currentPerkLevel.Specialization != SpecializationType.None)
                {
                    // Convert ID to enum, then get the string of the enum value. If we ever get a specialization with
                    // more than one word, another process will need to be used.
                    currentSpecializationRequired = (currentPerkLevel.Specialization).ToString();
                }
            }

            // Player hasn't reached max rank and this perk has another perk level to display.
            if (rank + 1 <= maxRank && nextPerkLevel != null)
            {
                var nextPerkFeat = perk.PerkFeats.ContainsKey(rank + 1) ? perk.PerkFeats[rank + 1].First() : null; 
                nextBonus = nextPerkLevel.Description;
                price = nextPerkLevel.Price + " SP (Available: " + player.UnallocatedSP + " SP)";

                if (nextPerkFeat != null)
                {
                    nextFPCost = nextPerkFeat.BaseFPCost > 0 ? (ColorTokenService.Green("Next FP: ") + nextPerkFeat.BaseFPCost + "\n") : string.Empty;

                    // If this perk level has a concentration cost and interval, display it on the menu.
                    if (nextPerkFeat.ConcentrationFPCost > 0 && nextPerkFeat.ConcentrationTickInterval > 0)
                    {
                        nextConcentrationCost = ColorTokenService.Green("Next Concentration FP: ") + nextPerkFeat.ConcentrationFPCost + " / " + nextPerkFeat.ConcentrationTickInterval + "s\n";
                    }
                }

                if (nextPerkLevel.Specialization > 0)
                {
                    nextSpecializationRequired = (nextPerkLevel.Specialization).ToString();
                }
            }

            var perkCategory = PerkService.GetPerkCategory(perk.Category);
            var cooldownDelay = perk.CooldownGroup == PerkCooldownGroup.None ?
                0.0f :
                perk.CooldownGroup.GetDelay();

            string header = ColorTokenService.Green("Name: ") + perk.Name + "\n" +
                            ColorTokenService.Green("Category: ") + perkCategory.Name + "\n" +
                            ColorTokenService.Green("Rank: ") + rank + " / " + maxRank + "\n" +
                            ColorTokenService.Green("Price: ") + price + "\n" +
                            currentFPCost +
                            currentConcentrationCost +
                            (cooldownDelay > 0.0f ? ColorTokenService.Green("Cooldown: ") + cooldownDelay + "s" : "") + "\n" +
                            ColorTokenService.Green("Description: ") + perk.Description + "\n" +
                            ColorTokenService.Green("Current Bonus: ") + currentBonus + "\n" +
                            ColorTokenService.Green("Requires Specialization: ") + currentSpecializationRequired + "\n" +
                            nextFPCost +
                            nextConcentrationCost +
                            ColorTokenService.Green("Next Bonus: ") + nextBonus + "\n" +
                            ColorTokenService.Green("Requires Specialization: ") + nextSpecializationRequired + "\n";
                

            if (nextPerkLevel != null)
            {
                var requirements = nextPerkLevel.SkillRequirements; 
                if (requirements.Count > 0)
                {
                    header += "\n" + ColorTokenService.Green("Next Upgrade Skill Requirements:\n\n");

                    bool hasRequirement = false;
                    foreach (var req in requirements)
                    {
                        if (req.Value > 0)
                        {
                            PCSkill pcSkill = player.Skills[req.Key];
                            var skill = SkillService.GetSkill(req.Key);

                            string detailLine = skill.Name + " Rank " + req.Value;

                            if (pcSkill.Rank >= req.Value)
                            {
                                header += ColorTokenService.Green(detailLine) + "\n";
                            }
                            else
                            {
                                header += ColorTokenService.Red(detailLine) + "\n";
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
                    Model vm = GetDialogCustomData<Model>();
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
            Model vm = GetDialogCustomData<Model>();
            DialogResponse response = GetResponseByID("CategoryPage", responseID);

            vm.SelectedCategoryID = (int)response.CustomData;
            BuildPerkList();
            ChangePage("PerkListPage");
        }

        private void HandlePerkListResponses(int responseID)
        {
            Model vm = GetDialogCustomData<Model>();
            DialogResponse response = GetResponseByID("PerkListPage", responseID);

            vm.SelectedPerkID = (PerkType)response.CustomData;
            BuildPerkDetails();
            ChangePage("PerkDetailsPage");
        }

        private void HandlePerkDetailsResponses(int responseID)
        {
            Model vm = GetDialogCustomData<Model>();

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
