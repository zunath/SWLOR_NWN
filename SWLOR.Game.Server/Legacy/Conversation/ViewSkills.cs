using System.Linq;
using SWLOR.Game.Server.Legacy.Data.Entity;
using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.GameObject;
using SWLOR.Game.Server.Legacy.Service;
using SWLOR.Game.Server.Legacy.ValueObject.Dialog;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Legacy.Conversation
{
    public class ViewSkills: ConversationBase
    {
        private class Model
        {
            public int SelectedCategoryID { get; set; }
            public int SelectedSkillID { get; set; }
            public int RPXPDistributing { get; set; }
            public bool IsConfirming { get; set; }
        }
        
        public override PlayerDialog SetUp(NWPlayer player)
        {
            var dialog = new PlayerDialog("CategoryPage");
            var mainPage = new DialogPage(
                "Please select a skill category."
            );

            var skillListPage = new DialogPage(
                "Please select a skill."
            );

            var skillDetailsPage = new DialogPage(
                "<SET LATER>",
                "Distribute Roleplay XP",
                "Toggle Decay Lock"
            );

            var distributeRPXPPage = new DialogPage(
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
            var categories = SkillService.GetActiveCategories();
            ClearPageResponses("CategoryPage");

            // If player has skill levels to distribute, display the option to distribute them.
            var showDistribution = DataService.PCSkillPool.GetByPlayerIDWithLevelsUndistributed(GetPC().GlobalID).Any();
            AddResponseToPage("CategoryPage", ColorToken.Green("Distribute Skill Ranks"), showDistribution);
            
            foreach (var category in categories)
            {
                AddResponseToPage("CategoryPage", category.Name, true, category.ID);
            }
        }

        private void LoadSkillResponses()
        {
            var vm = GetDialogCustomData<Model>();
            var skills = SkillService.GetPCSkillsForCategory(GetPC().GlobalID, vm.SelectedCategoryID);

            ClearPageResponses("SkillListPage");
            foreach (var pcSkill in skills)
            {
                var skill = SkillService.GetSkill(pcSkill.SkillID);
                AddResponseToPage("SkillListPage", skill.Name + " (Lvl. " + pcSkill.Rank + ")", true, skill.ID);
            }
        }

        private void LoadSkillDetails()
        {
            var vm = GetDialogCustomData<Model>();
            var skill = SkillService.GetSkill(vm.SelectedSkillID);
            var pcSkill = SkillService.GetPCSkill(GetPC(), vm.SelectedSkillID);
            var req = SkillService.SkillXPRequirements[pcSkill.Rank];
            var header = CreateSkillDetailsHeader(pcSkill, req);
            SetPageHeader("SkillDetailsPage", header);

            if(!skill.ContributesToSkillCap)
            {
                SetResponseVisible("SkillDetailsPage", 2, false);
            }
        }

        private string CreateSkillDetailsHeader(PCSkill pcSkill, int req)
        {
            var player = DataService.Player.GetByID(pcSkill.PlayerID);
            var skill = SkillService.GetSkill(pcSkill.SkillID);
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

            var decayLock = ColorToken.Green("Decay Lock: ") + ColorToken.White("Unlocked");
            if (pcSkill.IsLocked)
            {
                decayLock = ColorToken.Green("Decay Lock: ") + ColorToken.Red("Locked");
            }
            
            // Skills which don't contribute to the cap cannot be locked (there's no reason for it.)
            // Display a message explaining this to the player instead.
            var noContributeMessage = string.Empty;
            if (!skill.ContributesToSkillCap)
            {
                decayLock = string.Empty;
                noContributeMessage = ColorToken.Green("This skill does not contribute to your cumulative skill cap.") + "\n\n";
            }

            var rpXP = ColorToken.Green("Roleplay XP: ") + player.RoleplayXP + "\n";

            var primaryAttribute = DataService.Attribute.GetByID(skill.Primary);
            var secondaryAttribute = DataService.Attribute.GetByID(skill.Secondary);
            var tertiaryAttribute = DataService.Attribute.GetByID(skill.Tertiary);
            var primary = ColorToken.Green("Primary (+" + PlayerStatService.PrimaryIncrease + "): ") + primaryAttribute.Name + "\n";
            var secondary = ColorToken.Green("Secondary (+" + PlayerStatService.SecondaryIncrease + "): ") + secondaryAttribute.Name + "\n";
            var tertiary = ColorToken.Green("Tertiary (+" + PlayerStatService.TertiaryIncrease + "): ") + tertiaryAttribute.Name + "\n";

            return
                    ColorToken.Green("Skill: ") + skill.Name + "\n" +
                    ColorToken.Green("Rank: ") + title + "\n" +
                    ColorToken.Green("Exp: ") + MenuService.BuildBar(pcSkill.XP, req, 100, ColorToken.TokenStart(255, 127, 0)) + "\n" +
                    rpXP +
                    primary +
                    secondary +
                    tertiary +
                    noContributeMessage +
                    decayLock + "\n\n" +
                    ColorToken.Green("Description: ") + skill.Description + "\n";
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
            var vm = GetDialogCustomData<Model>();
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

            var vm = GetDialogCustomData<Model>();
            var response = GetResponseByID("CategoryPage", responseID);
            
            vm.SelectedCategoryID = (int)response.CustomData;
            LoadSkillResponses();
            ChangePage("SkillListPage");
        }

        private void HandleSkillListResponse(int responseID)
        {
            var vm = GetDialogCustomData<Model>();
            var response = GetResponseByID("SkillListPage", responseID);
            var skillID = (int)response.CustomData;
            
            vm.SelectedSkillID = skillID;
            LoadSkillDetails();
            ChangePage("SkillDetailsPage");
        }

        private void HandleSkillDetailsResponse(int responseID)
        {
            var vm = GetDialogCustomData<Model>();
            
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
            var player = GetPC();
            var dbPlayer = DataService.Player.GetByID(player.GlobalID);
            var vm = GetDialogCustomData<Model>();
            var skill = SkillService.GetSkill(vm.SelectedSkillID);

            var header = ColorToken.Green("Roleplay XP Distribution") + "\n\n";
            header += ColorToken.Green("Skill: ") + skill.Name + "\n";
            header += ColorToken.Green("Available RP XP: ") + dbPlayer.RoleplayXP + "\n";
            header += ColorToken.Green("Currently Distributing: ") + vm.RPXPDistributing + " RP XP\n";

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
            var player = GetPC();
            var dbPlayer = DataService.Player.GetByID(player.GlobalID);
            var vm = GetDialogCustomData<Model>();

            var rpXpToDistribute = dbPlayer.RoleplayXP;

            var pcSkill = SkillService.GetPCSkill(player, vm.SelectedSkillID);
            var skill = SkillService.GetSkill(vm.SelectedSkillID);
            // Get all player skills and then sum them up by the rank.
            var totalSkillCount = DataService.PCSkill
                .GetAllByPlayerID(player.GlobalID)
                .Where(x => DataService.Skill.GetByID(x.SkillID).ContributesToSkillCap)
                .Sum(s => s.Rank);
            var totalSkillXpToMaxRank = 0;
            //totalSkillCount < SkillService.SkillCap
            for (var x = pcSkill.Rank + 1; x < skill.MaxRank; x++)
            {
                int tempValue;
                if (SkillService.SkillXPRequirements.TryGetValue(x, out tempValue))
                {
                    totalSkillXpToMaxRank += tempValue;
                }
            }

            switch (responseID)
            {
                case 1: // Select All RP XP
                    vm.RPXPDistributing = totalSkillXpToMaxRank;
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
                    else if(vm.RPXPDistributing > totalSkillXpToMaxRank)
                    {
                        player.SendMessage("Please lower your distribution amount, current max for this skill is " + totalSkillXpToMaxRank + ".");
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
                            DataService.SubmitDataChange(dbPlayer, DatabaseActionType.Update);
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
