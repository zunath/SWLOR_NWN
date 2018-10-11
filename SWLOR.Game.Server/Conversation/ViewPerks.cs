using System;
using System.Collections.Generic;
using System.Linq;
using NWN;
using SWLOR.Game.Server.Data.Entities;
using SWLOR.Game.Server.GameObject;

using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject;
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

        private readonly IPerkService _perk;
        private readonly ISkillService _skill;
        private readonly IPlayerService _player;
        private readonly IColorTokenService _color;

        public ViewPerks(
            INWScript script, 
            IDialogService dialog,
            IPerkService perk,
            ISkillService skill,
            IPlayerService player,
            IColorTokenService color) 
            : base(script, dialog)
        {
            _perk = perk;
            _skill = skill;
            _player = player;
            _color = color;
        }

        public override PlayerDialog SetUp(NWPlayer player)
        {
            PlayerDialog dialog = new PlayerDialog("MainPage");
            DialogPage mainPage = new DialogPage(
                "<SET LATER>",
                "View My Perks",
                "Buy Perks",
                "Back"
            );

            DialogPage categoryPage = new DialogPage(
                "Please select a category. Additional options will appear as you increase your skill ranks."
            );

            DialogPage perkListPage = new DialogPage(
                "Please select a perk. Additional options will appear as you increase your skill ranks."
            );

            DialogPage perkDetailsPage = new DialogPage(
                "<SET LATER>",
                "Purchase Upgrade",
                "Back"
            );

            DialogPage viewMyPerksPage = new DialogPage(
                "<SET LATER>",
                "Back"
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
            PlayerCharacter pcEntity = _player.GetPlayerEntity(GetPC().GlobalID);

            int totalSP = _skill.GetPCTotalSkillCount(GetPC().GlobalID);
            int totalPerks = _perk.GetPCTotalPerkCount(GetPC().GlobalID);

            return _color.Green("Total SP: ") + totalSP + " / " + _skill.SkillCap + "\n" +
                    _color.Green("Available SP: ") + pcEntity.UnallocatedSP + "\n" +
                    _color.Green("Total Perks: ") + totalPerks + "\n";
        }

        private void BuildViewMyPerks()
        {
            List<PCPerkHeader> perks = _perk.GetPCPerksForMenuHeader(GetPC().GlobalID);

            string header = _color.Green("Perks purchased:") + "\n\n";
            foreach (PCPerkHeader perk in perks)
            {
                header += perk.Name + " (Lvl. " + perk.Level + ") \n";
            }

            SetPageHeader("ViewMyPerksPage", header);
        }

        private void BuildCategoryList()
        {
            List<PerkCategory> categories = _perk.GetPerkCategoriesForPC(GetPC().GlobalID);

            ClearPageResponses("CategoryPage");
            foreach (PerkCategory category in categories)
            {
                AddResponseToPage("CategoryPage", category.Name, true, category.PerkCategoryID);
            }
            AddResponseToPage("CategoryPage", "Back", true, -1);
        }

        private void BuildPerkList()
        {
            Model vm = GetDialogCustomData<Model>();
            List<Data.Entities.Perk> perks = _perk.GetPerksForPC(GetPC().GlobalID, vm.SelectedCategoryID);

            ClearPageResponses("PerkListPage");
            foreach (Data.Entities.Perk perk in perks)
            {
                AddResponseToPage("PerkListPage", perk.Name, true, perk.PerkID);
            }
            AddResponseToPage("PerkListPage", "Back", true, -1);
        }

        private void BuildPerkDetails()
        {
            Model vm = GetDialogCustomData<Model>();
            Data.Entities.Perk perk = _perk.GetPerkByID(vm.SelectedPerkID);
            PCPerk pcPerk = _perk.GetPCPerkByID(GetPC().GlobalID, perk.PerkID);
            PlayerCharacter player = _player.GetPlayerEntity(GetPC().GlobalID);

            int rank = pcPerk?.PerkLevel ?? 0;
            int maxRank = perk.PerkLevels.Count;
            string currentBonus = "N/A";
            string nextBonus = "N/A";
            string price = "N/A";
            PerkLevel currentPerkLevel = _perk.FindPerkLevel(perk.PerkLevels, rank);
            PerkLevel nextPerkLevel = _perk.FindPerkLevel(perk.PerkLevels, rank + 1);

            SetResponseVisible("PerkDetailsPage", 1, _perk.CanPerkBeUpgraded(perk, pcPerk, player));

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

            string header = _color.Green("Name: ") + perk.Name + "\n" +
                    _color.Green("Category: ") + perk.PerkCategory.Name + "\n" +
                    _color.Green("Rank: ") + rank + " / " + maxRank + "\n" +
                    _color.Green("Price: ") + price + "\n" +
                    (perk.BaseFPCost > 0 ? _color.Green("FP: ") + perk.BaseFPCost : "") + "\n" +
                    (perk.CooldownCategory != null && perk.CooldownCategory.BaseCooldownTime > 0 ? _color.Green("Cooldown: ") + perk.CooldownCategory.BaseCooldownTime + "s" : "") + "\n" +
                    _color.Green("Description: ") + perk.Description + "\n" +
                    _color.Green("Current Bonus: ") + currentBonus + "\n" +
                    _color.Green("Next Bonus: ") + nextBonus + "\n";

            if (nextPerkLevel != null)
            {
                List<PerkLevelSkillRequirement> requirements = nextPerkLevel.PerkLevelSkillRequirements.ToList();
                if (requirements.Count > 0)
                {
                    header += "\n" + _color.Green("Next Upgrade Skill Requirements:\n\n");
                    
                    bool hasRequirement = false;
                    foreach (PerkLevelSkillRequirement req in requirements)
                    {
                        if (req.RequiredRank > 0)
                        {
                            string detailLine = req.Skill.Name + " Rank " + req.RequiredRank;
                            
                            PCSkill skill = _skill.GetPCSkillByID(GetPC().GlobalID, req.SkillID);
                            if (skill.Rank >= req.RequiredRank)
                            {
                                header += _color.Green(detailLine) + "\n";
                            }
                            else
                            {
                                header += _color.Red(detailLine) + "\n";
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
                case "ViewMyPerksPage":
                    HandleViewMyPerksResponses(responseID);
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
                case 3: // Back
                    SwitchConversation("RestMenu");
                    break;
            }
        }

        private void HandleViewMyPerksResponses(int responseID)
        {
            ChangePage("MainPage");
        }

        private void HandleCategoryResponses(int responseID)
        {
            Model vm = GetDialogCustomData<Model>();
            DialogResponse response = GetResponseByID("CategoryPage", responseID);
            if ((int)response.CustomData == -1)
            {
                ChangePage("MainPage");
                return;
            }

            vm.SelectedCategoryID = (int) response.CustomData;
            BuildPerkList();
            ChangePage("PerkListPage");
        }

        private void HandlePerkListResponses(int responseID)
        {
            Model vm = GetDialogCustomData<Model>();
            DialogResponse response = GetResponseByID("PerkListPage", responseID);
            if ((int)response.CustomData == -1)
            {
                ChangePage("CategoryPage");
                return;
            }

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
                        _perk.DoPerkUpgrade(GetPC(), vm.SelectedPerkID);
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
                case 2: // Back
                    vm.IsConfirmingPurchase = false;
                    SetResponseText("PerkDetailsPage", 1, "Purchase Upgrade");
                    BuildPerkList();
                    ChangePage("PerkListPage");
                    break;
            }
        }

        public override void EndDialog()
        {
        }
    }
}
