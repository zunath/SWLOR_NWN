using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.ValueObject.Dialog;

namespace SWLOR.Game.Server.Conversation
{
    public class ViewSkills: ConversationBase
    {
        private class Model
        {
            public SkillCategory SelectedCategoryID { get; set; }
            public Skill SelectedSkillID { get; set; }
            public int RPXPDistributing { get; set; }
            public bool IsConfirming { get; set; }
        }
        
        public override PlayerDialog SetUp(NWPlayer player)
        {
            PlayerDialog dialog = new PlayerDialog("CategoryPage");
            DialogPage mainPage = new DialogPage(
                "Please select a skill category."
            );

            DialogPage skillListPage = new DialogPage(
                "Please select a skill."
            );

            DialogPage skillDetailsPage = new DialogPage(
                "<SET LATER>",
                "Distribute Roleplay XP",
                "Toggle Decay Lock"
            );

            DialogPage distributeRPXPPage = new DialogPage(
                "<SET LATER>",
                "Select All RP XP",
                "Increase by 1000",
                "Increase by 100",
                "Increase by 10",
                "Increase by 1",
                "Decrease by 1000",
                "Decrease by 100",
                "Decrease by 10",
                "Decrease by 1",
                "Distribute Roleplay XP");

            dialog.AddPage("CategoryPage", mainPage);
            dialog.AddPage("SkillListPage", skillListPage);
            dialog.AddPage("SkillDetailsPage", skillDetailsPage);
            dialog.AddPage("DistributeRPXPPage", distributeRPXPPage);
            return dialog;
        }

        public override void Initialize()
        {
            LoadCategoryResponses();
        }

        private void LoadCategoryResponses()
        {
            List<SkillCategory> categories = SkillService.GetActiveCategories();
            ClearPageResponses("CategoryPage");

            // If player has skill levels to distribute, display the option to distribute them.
            var player = DataService.Player.GetByID(GetPC().GlobalID);
            var showDistribution = player.SkillPools.Any(x => x.Value > 0);
            AddResponseToPage("CategoryPage", ColorTokenService.Green("Distribute Skill Ranks"), showDistribution);
            
            foreach (SkillCategory category in categories)
            {
                var categoryAttr = category.GetAttribute<SkillCategory, SkillCategoryAttribute>();

                if (categoryAttr.IsActive)
                {
                    AddResponseToPage("CategoryPage", categoryAttr.Name, true, category);
                }
            }
        }

        private void LoadSkillResponses()
        {
            var vm = GetDialogCustomData<Model>();
            var player = DataService.Player.GetByID(GetPC().GlobalID);
            var skills = SkillService.GetAllSkillsInCategory(vm.SelectedCategoryID);

            ClearPageResponses("SkillListPage");
            foreach (var skillID in skills)
            {
                var skill = SkillService.GetSkill(skillID);
                var pcSkill = player.Skills[skillID];

                if(skill.IsActive)
                {
                    AddResponseToPage("SkillListPage", skill.Name + " (Lvl. " + pcSkill.Rank + ")", true, skillID);
                }
            }
        }

        private void LoadSkillDetails()
        {
            Model vm = GetDialogCustomData<Model>();
            var player = DataService.Player.GetByID(GetPC().GlobalID);
            var skill = SkillService.GetSkill(vm.SelectedSkillID);
            var pcSkill = player.Skills[vm.SelectedSkillID];
            var req = SkillService.SkillXPRequirements[pcSkill.Rank];
            var header = CreateSkillDetailsHeader(vm.SelectedSkillID, pcSkill, req);
            SetPageHeader("SkillDetailsPage", header);

            if(!skill.ContributesToSkillCap)
            {
                SetResponseVisible("SkillDetailsPage", 2, false);
            }
        }

        private string CreateSkillDetailsHeader(Skill skillID, PCSkill pcSkill, int req)
        {
            var playerID = GetPC().GlobalID;
            Player player = DataService.Player.GetByID(playerID);
            var skill = SkillService.GetSkill(skillID);
            string title;
            if (pcSkill.Rank <= 3) title = "Untrained";
            else if (pcSkill.Rank <= 7) title = "Neophyte";
            else if (pcSkill.Rank <= 13) title = "Novice";
            else if (pcSkill.Rank <= 20) title = "Apprentice";
            else if (pcSkill.Rank <= 35) title = "Journeyman";
            else if (pcSkill.Rank <= 50) title = "Expert";
            else if (pcSkill.Rank <= 65) title = "Adept";
            else if (pcSkill.Rank <= 80) title = "Master";
            else if (pcSkill.Rank <= 100) title = "Grandmaster";
            else title = "Unknown";

            title += " (" + pcSkill.Rank + ")";

            string decayLock = ColorTokenService.Green("Decay Lock: ") + ColorTokenService.White("Unlocked");
            if (pcSkill.IsLocked)
            {
                decayLock = ColorTokenService.Green("Decay Lock: ") + ColorTokenService.Red("Locked");
            }
            
            // Skills which don't contribute to the cap cannot be locked (there's no reason for it.)
            // Display a message explaining this to the player instead.
            string noContributeMessage = string.Empty;
            if (!skill.ContributesToSkillCap)
            {
                decayLock = string.Empty;
                noContributeMessage = ColorTokenService.Green("This skill does not contribute to your cumulative skill cap.") + "\n\n";
            }

            string rpXP = ColorTokenService.Green("Roleplay XP: ") + player.RoleplayXP + "\n";

            string primary = ColorTokenService.Green("Primary (+" + PlayerStatService.PrimaryIncrease + "): ") + skill.Primary.GetDescriptionAttribute() + "\n";
            string secondary = ColorTokenService.Green("Secondary (+" + PlayerStatService.SecondaryIncrease + "): ") + skill.Secondary.GetDescriptionAttribute() + "\n";
            string tertiary = ColorTokenService.Green("Tertiary (+" + PlayerStatService.TertiaryIncrease + "): ") + skill.Tertiary.GetDescriptionAttribute() + "\n";

            return
                    ColorTokenService.Green("Skill: ") + skill.Name + "\n" +
                    ColorTokenService.Green("Rank: ") + title + "\n" +
                    ColorTokenService.Green("Exp: ") + MenuService.BuildBar(pcSkill.XP, req, 100, ColorTokenService.TokenStart(255, 127, 0)) + "\n" +
                    rpXP +
                    primary +
                    secondary +
                    tertiary +
                    noContributeMessage +
                    decayLock + "\n\n" +
                    ColorTokenService.Green("Description: ") + skill.Description + "\n";
        }

        public override void DoAction(NWPlayer player, string pageName, int responseID)
        {
            switch (pageName)
            {
                case "CategoryPage":
                    HandleCategoryResponse(responseID);
                    break;
                case "SkillListPage":
                    HandleSkillListResponse(responseID);
                    break;
                case "SkillDetailsPage":
                    HandleSkillDetailsResponse(responseID);
                    break;
                case "DistributeRPXPPage":
                    HandleDistributeRPXPResponse(responseID);
                    break;
            }
        }

        public override void Back(NWPlayer player, string beforeMovePage, string afterMovePage)
        {
            Model vm = GetDialogCustomData<Model>();
            vm.IsConfirming = false;
            vm.RPXPDistributing = 0;
        }

        private void HandleCategoryResponse(int responseID)
        {
            // Selected the "Distribute Skill Ranks" option. Send player to that menu.
            if (responseID == 1)
            {
                SwitchConversation("DistributeSkillRanks");
                return;
            }

            Model vm = GetDialogCustomData<Model>();
            DialogResponse response = GetResponseByID("CategoryPage", responseID);
            
            vm.SelectedCategoryID = (SkillCategory)response.CustomData;
            LoadSkillResponses();
            ChangePage("SkillListPage");
        }

        private void HandleSkillListResponse(int responseID)
        {
            Model vm = GetDialogCustomData<Model>();
            DialogResponse response = GetResponseByID("SkillListPage", responseID);
            var skillID = (Skill)response.CustomData;
            
            vm.SelectedSkillID = skillID;
            LoadSkillDetails();
            ChangePage("SkillDetailsPage");
        }

        private void HandleSkillDetailsResponse(int responseID)
        {
            Model vm = GetDialogCustomData<Model>();
            
            switch (responseID)
            {
                case 1: // Distribute Roleplay XP
                    LoadDistributeRPXPPage();
                    ChangePage("DistributeRPXPPage");
                    break;
                case 2: // Toggle Decay Lock
                    SkillService.ToggleSkillLock(GetPC().GlobalID, vm.SelectedSkillID);
                    LoadSkillDetails();
                    break;
            }
        }

        private void LoadDistributeRPXPPage()
        {
            NWPlayer player = GetPC();
            Player dbPlayer = DataService.Player.GetByID(player.GlobalID);
            Model vm = GetDialogCustomData<Model>();
            var skill = SkillService.GetSkill(vm.SelectedSkillID);

            string header = ColorTokenService.Green("Roleplay XP Distribution") + "\n\n";
            header += ColorTokenService.Green("Skill: ") + skill.Name + "\n";
            header += ColorTokenService.Green("Available RP XP: ") + dbPlayer.RoleplayXP + "\n";
            header += ColorTokenService.Green("Currently Distributing: ") + vm.RPXPDistributing + " RP XP\n";

            if(vm.IsConfirming)
            {
                SetResponseText("DistributeRPXPPage", 10, "CONFIRM DISTRIBUTE ROLEPLAY XP (" + vm.RPXPDistributing + ")");
            }
            else
            {
                SetResponseText("DistributeRPXPPage", 10, "Distribute Roleplay XP (" + vm.RPXPDistributing + ")");
            }

            SetPageHeader("DistributeRPXPPage", header);
        }

        private void HandleDistributeRPXPResponse(int responseID)
        {
            NWPlayer player = GetPC();
            Player dbPlayer = DataService.Player.GetByID(player.GlobalID);
            Model vm = GetDialogCustomData<Model>();

            switch (responseID)
            {
                case 1: // Select All RP XP
                    vm.RPXPDistributing = dbPlayer.RoleplayXP;
                    break;
                case 2: // Increase by 1000
                    vm.RPXPDistributing += 1000;
                    break;
                case 3: // Increase by 100
                    vm.RPXPDistributing += 100;
                    break;
                case 4: // Increase by 10
                    vm.RPXPDistributing += 10;
                    break;
                case 5: // Increase by 1
                    vm.RPXPDistributing += 1;
                    break;
                case 6: // Decrease by 1000
                    vm.RPXPDistributing -= 1000;
                    break;
                case 7: // Decrease by 100
                    vm.RPXPDistributing -= 100;
                    break;
                case 8: // Decrease by 10
                    vm.RPXPDistributing -= 10;
                    break;
                case 9: // Decrease by 1
                    vm.RPXPDistributing -= 1;
                    break;
                case 10: // Distribute Roleplay XP

                    // Make sure the player specified how much they want to distribute.
                    if (vm.RPXPDistributing <= 0)
                    {
                        player.SendMessage("Please specify how much RP XP you'd like to distribute into this skill.");
                        vm.IsConfirming = false;
                    }
                    else
                    {
                        if (vm.IsConfirming)
                        {
                            // Give the distributed XP to a particular skill.
                            // We disable residency bonuses, DM bonuses, and skill penalties during this distribution because
                            // those are calculated when we give the player RP XP.
                            SkillService.GiveSkillXP(player, vm.SelectedSkillID, vm.RPXPDistributing, false, false);

                            dbPlayer = DataService.Player.GetByID(player.GlobalID);
                            dbPlayer.RoleplayXP -= vm.RPXPDistributing;
                            DataService.SubmitDataChange(dbPlayer, DatabaseActionType.Set);
                            vm.IsConfirming = false;
                            vm.RPXPDistributing = 0;
                        }
                        else
                        {
                            vm.IsConfirming = true;
                        }
                    }
                    break;
            }

            if (vm.RPXPDistributing > dbPlayer.RoleplayXP)
                vm.RPXPDistributing = dbPlayer.RoleplayXP;
            else if (vm.RPXPDistributing < 0)
                vm.RPXPDistributing = 0;

            LoadDistributeRPXPPage();
        }

        public override void EndDialog()
        {
        }
    }
}
