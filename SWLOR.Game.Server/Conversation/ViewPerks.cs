using System;
using System.Collections.Generic;
using System.Linq;
using NWN;
using SWLOR.Game.Server.Data;
using SWLOR.Game.Server.Data.Entity;
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
        private readonly IDataService _data;

        public ViewPerks(
            INWScript script, 
            IDialogService dialog,
            IPerkService perk,
            ISkillService skill,
            IPlayerService player,
            IColorTokenService color,
            IDataService data) 
            : base(script, dialog)
        {
            _perk = perk;
            _skill = skill;
            _player = player;
            _color = color;
            _data = data;
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
            PlayerCharacter pcEntity = _player.GetPlayerEntity(GetPC().GlobalID);

            int totalSP = _skill.GetPCTotalSkillCount(GetPC());
            int totalPerks = _perk.GetPCTotalPerkCount(GetPC().GlobalID);

            return _color.Green("Total SP: ") + totalSP + " / " + _skill.SkillCap + "\n" +
                    _color.Green("Available SP: ") + pcEntity.UnallocatedSP + "\n" +
                    _color.Green("Total Perks: ") + totalPerks + "\n";
        }

        private void BuildViewMyPerks()
        {
            List<PCPerk> perks = _data.Where<PCPerk>(x => x.PlayerID == GetPC().GlobalID).ToList();

            string header = _color.Green("Perks purchased:") + "\n\n";
            foreach (PCPerk pcPerk in perks)
            {
                var perk = _data.Get<Data.Entity.Perk>(pcPerk.PerkID);
                header += perk.Name + " (Lvl. " + pcPerk.PerkLevel + ") \n";
            }

            SetPageHeader("ViewMyPerksPage", header);
        }


        private void BuildCategoryList()
        {
            var perksAvailable = _perk.GetPerksAvailableToPC(GetPC());
            var categoryIDs = perksAvailable.Select(x => x.PerkCategoryID).Distinct();
            List<PerkCategory> categories = _data.Where<PerkCategory>(x => categoryIDs.Contains(x.PerkCategoryID)).ToList();

            ClearPageResponses("CategoryPage");
            foreach (PerkCategory category in categories)
            {
                AddResponseToPage("CategoryPage", category.Name, true, category.PerkCategoryID);
            }
        }

        private void BuildPerkList()
        {
            Model vm = GetDialogCustomData<Model>();
            var perksAvailable = _perk.GetPerksAvailableToPC(GetPC());
            List<Data.Entity.Perk> perks = perksAvailable.Where(x => x.PerkCategoryID == vm.SelectedCategoryID).ToList();

            ClearPageResponses("PerkListPage");
            foreach (Data.Entity.Perk perk in perks)
            {
                AddResponseToPage("PerkListPage", perk.Name, true, perk.PerkID);
            }
        }

        private void BuildPerkDetails()
        {
            Model vm = GetDialogCustomData<Model>();
            Data.Entity.Perk perk = _perk.GetPerkByID(vm.SelectedPerkID);
            PCPerk pcPerk = _perk.GetPCPerkByID(GetPC().GlobalID, perk.PerkID);
            PlayerCharacter player = _player.GetPlayerEntity(GetPC().GlobalID);
            var perkLevels = _data.Where<PerkLevel>(x => x.PerkID == perk.PerkID).ToList();

            int rank = pcPerk?.PerkLevel ?? 0;
            int maxRank = perkLevels.Count();
            string currentBonus = "N/A";
            string nextBonus = "N/A";
            string price = "N/A";
            PerkLevel currentPerkLevel = _perk.FindPerkLevel(perkLevels, rank);
            PerkLevel nextPerkLevel = _perk.FindPerkLevel(perkLevels, rank + 1);

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

            var perkCategory = _data.Get<PerkCategory>(perk.PerkCategoryID);
            var cooldownCategory = _data.Get<CooldownCategory>(perk.CooldownCategoryID);

            string header = _color.Green("Name: ") + perk.Name + "\n" +
                    _color.Green("Category: ") + perkCategory.Name + "\n" +
                    _color.Green("Rank: ") + rank + " / " + maxRank + "\n" +
                    _color.Green("Price: ") + price + "\n" +
                    (perk.BaseFPCost > 0 ? _color.Green("FP: ") + perk.BaseFPCost : "") + "\n" +
                    (cooldownCategory != null && cooldownCategory.BaseCooldownTime > 0 ? _color.Green("Cooldown: ") + cooldownCategory.BaseCooldownTime + "s" : "") + "\n" +
                    _color.Green("Description: ") + perk.Description + "\n" +
                    _color.Green("Current Bonus: ") + currentBonus + "\n" +
                    _color.Green("Next Bonus: ") + nextBonus + "\n";

            if (nextPerkLevel != null)
            {
                List<PerkLevelSkillRequirement> requirements = 
                    _data.Where<PerkLevelSkillRequirement>(x => x.PerkLevelID == nextPerkLevel.PerkLevelID).ToList();
                if (requirements.Count > 0)
                {
                    header += "\n" + _color.Green("Next Upgrade Skill Requirements:\n\n");
                    
                    bool hasRequirement = false;
                    foreach (PerkLevelSkillRequirement req in requirements)
                    {
                        if (req.RequiredRank > 0)
                        {
                            PCSkill pcSkill = _skill.GetPCSkill(GetPC(), req.SkillID);
                            Skill skill = _skill.GetSkill(pcSkill.SkillID);

                            string detailLine = skill.Name + " Rank " + req.RequiredRank;
                            
                            if (pcSkill.Rank >= req.RequiredRank)
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
            }
        }

        public override void EndDialog()
        {
        }
    }
}
