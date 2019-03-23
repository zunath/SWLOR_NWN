using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.ValueObject.Dialog;

namespace SWLOR.Game.Server.Conversation
{
    public class ViewPerks: ConversationBase
    {
        private class Model
        {
            public int SelectedCategoryID { get; set; }
            public int SelectedPerkID { get; set; }
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
            List<PCPerk> perks = DataService.Where<PCPerk>(x => x.PlayerID == GetPC().GlobalID).ToList();

            string header = ColorTokenService.Green("Perks purchased:") + "\n\n";
            foreach (PCPerk pcPerk in perks)
            {
                var perk = DataService.Get<Data.Entity.Perk>(pcPerk.PerkID);
                header += perk.Name + " (Lvl. " + pcPerk.PerkLevel + ") \n";
            }

            SetPageHeader("ViewMyPerksPage", header);
        }


        private void BuildCategoryList()
        {
            var perksAvailable = PerkService.GetPerksAvailableToPC(GetPC());
            var categoryIDs = perksAvailable.Select(x => x.PerkCategoryID).Distinct();
            List<PerkCategory> categories = DataService.Where<PerkCategory>(x => categoryIDs.Contains(x.ID)).ToList();

            ClearPageResponses("CategoryPage");
            foreach (PerkCategory category in categories)
            {
                AddResponseToPage("CategoryPage", category.Name, true, category.ID);
            }
        }

        private void BuildPerkList()
        {
            Model vm = GetDialogCustomData<Model>();
            var perksAvailable = PerkService.GetPerksAvailableToPC(GetPC());
            List<Data.Entity.Perk> perks = perksAvailable.Where(x => x.PerkCategoryID == vm.SelectedCategoryID).ToList();

            ClearPageResponses("PerkListPage");
            foreach (Data.Entity.Perk perk in perks)
            {
                AddResponseToPage("PerkListPage", perk.Name, true, perk.ID);
            }
        }

        private void BuildPerkDetails()
        {
            Model vm = GetDialogCustomData<Model>();
            Data.Entity.Perk perk = PerkService.GetPerkByID(vm.SelectedPerkID);
            PCPerk pcPerk = PerkService.GetPCPerkByID(GetPC().GlobalID, perk.ID);
            Player player = PlayerService.GetPlayerEntity(GetPC().GlobalID);
            var perkLevels = DataService.Where<PerkLevel>(x => x.PerkID == perk.ID).ToList();

            int rank = pcPerk?.PerkLevel ?? 0;
            int maxRank = perkLevels.Count();
            string currentBonus = "N/A";
            string nextBonus = "N/A";
            string price = "N/A";
            PerkLevel currentPerkLevel = PerkService.FindPerkLevel(perkLevels, rank);
            PerkLevel nextPerkLevel = PerkService.FindPerkLevel(perkLevels, rank + 1);
            SetResponseVisible("PerkDetailsPage", 1, PerkService.CanPerkBeUpgraded(GetPC(), vm.SelectedPerkID));

            if (rank > 0)
            {
                if (currentPerkLevel != null)
                {
                    currentBonus = currentPerkLevel.Description;
                }
            }
            if (rank + 1 <= maxRank)
            {
                if (nextPerkLevel != null)
                {
                    nextBonus = nextPerkLevel.Description;
                    price = nextPerkLevel.Price + " SP (Available: " + player.UnallocatedSP + " SP)";
                }
            }
            var perkCategory = DataService.Get<PerkCategory>(perk.PerkCategoryID);
            var cooldownCategory = perk.CooldownCategoryID == null ? 
                null : 
                DataService.Get<CooldownCategory>(perk.CooldownCategoryID);

            string header = ColorTokenService.Green("Name: ") + perk.Name + "\n" +
                    ColorTokenService.Green("Category: ") + perkCategory.Name + "\n" +
                    ColorTokenService.Green("Rank: ") + rank + " / " + maxRank + "\n" +
                    ColorTokenService.Green("Price: ") + price + "\n" +
                    (perk.BaseFPCost > 0 ? ColorTokenService.Green("FP: ") + perk.BaseFPCost : "") + "\n" +
                    (cooldownCategory != null && cooldownCategory.BaseCooldownTime > 0 ? ColorTokenService.Green("Cooldown: ") + cooldownCategory.BaseCooldownTime + "s" : "") + "\n" +
                    ColorTokenService.Green("Description: ") + perk.Description + "\n" +
                    ColorTokenService.Green("Current Bonus: ") + currentBonus + "\n" +
                    ColorTokenService.Green("Next Bonus: ") + nextBonus + "\n";

            if (nextPerkLevel != null)
            {
                List<PerkLevelSkillRequirement> requirements = 
                    DataService.Where<PerkLevelSkillRequirement>(x => x.PerkLevelID == nextPerkLevel.ID).ToList();
                if (requirements.Count > 0)
                {
                    header += "\n" + ColorTokenService.Green("Next Upgrade Skill Requirements:\n\n");
                    
                    bool hasRequirement = false;
                    foreach (PerkLevelSkillRequirement req in requirements)
                    {
                        if (req.RequiredRank > 0)
                        {
                            PCSkill pcSkill = SkillService.GetPCSkill(GetPC(), req.SkillID);
                            Skill skill = SkillService.GetSkill(pcSkill.SkillID);

                            string detailLine = skill.Name + " Rank " + req.RequiredRank;
                            
                            if (pcSkill.Rank >= req.RequiredRank)
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
            
            vm.SelectedCategoryID = (int) response.CustomData;
            BuildPerkList();
            ChangePage("PerkListPage");
        }

        private void HandlePerkListResponses(int responseID)
        {
            Model vm = GetDialogCustomData<Model>();
            DialogResponse response = GetResponseByID("PerkListPage", responseID);
            
            vm.SelectedPerkID = (int) response.CustomData;
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
