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
    public class ViewSkills: ConversationBase
    {
        private class Model
        {
            public int SelectedCategoryID { get; set; }
            public int SelectedSkillID { get; set; }
        }

        private readonly ISkillService _skill;
        private readonly IColorTokenService _color;
        private readonly IMenuService _menu;

        public ViewSkills(
            INWScript script, 
            IDialogService dialog,
            ISkillService skill,
            IColorTokenService color,
            IMenuService menu) 
            : base(script, dialog)
        {
            _skill = skill;
            _color = color;
            _menu = menu;
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
                "Toggle Decay Lock"
            );

            dialog.AddPage("CategoryPage", mainPage);
            dialog.AddPage("SkillListPage", skillListPage);
            dialog.AddPage("SkillDetailsPage", skillDetailsPage);
            return dialog;
        }

        public override void Initialize()
        {
            LoadCategoryResponses();
        }

        private void LoadCategoryResponses()
        {
            List<SkillCategory> categories = _skill.GetActiveCategories();

            ClearPageResponses("CategoryPage");
            foreach (SkillCategory category in categories)
            {
                AddResponseToPage("CategoryPage", category.Name, true, category.SkillCategoryID);
            }
        }

        private void LoadSkillResponses()
        {
            Model vm = GetDialogCustomData<Model>();
            List<PCSkill> skills = _skill.GetPCSkillsForCategory(GetPC().GlobalID, vm.SelectedCategoryID);

            ClearPageResponses("SkillListPage");
            foreach (PCSkill pcSkill in skills)
            {
                Skill skill = _skill.GetSkill(pcSkill.SkillID);
                AddResponseToPage("SkillListPage", skill.Name + " (Lvl. " + pcSkill.Rank + ")", true, skill.SkillID);
            }
        }

        private void LoadSkillDetails()
        {
            Model vm = GetDialogCustomData<Model>();
            Skill skill = _skill.GetSkill(vm.SelectedSkillID);
            PCSkill pcSkill = _skill.GetPCSkill(GetPC(), vm.SelectedSkillID);
            SkillXPRequirement req = skill.SkillXPRequirements.Single(x => x.Rank == pcSkill.Rank && x.SkillID == skill.SkillID); 
            string header = CreateSkillDetailsHeader(pcSkill, req);
            SetPageHeader("SkillDetailsPage", header);

            if(!skill.ContributesToSkillCap)
            {
                SetResponseVisible("SkillDetailsPage", 1, false);
            }
        }

        private string CreateSkillDetailsHeader(PCSkill pcSkill, SkillXPRequirement req)
        {
            Skill skill = _skill.GetSkill(pcSkill.SkillID);
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

            string decayLock = _color.Green("Decay Lock: ") + _color.White("Unlocked");
            if (pcSkill.IsLocked)
            {
                decayLock = _color.Green("Decay Lock: ") + _color.Red("Locked");
            }


            // Skills which don't contribute to the cap cannot be locked (there's no reason for it.)
            // Display a message explaining this to the player instead.
            string noContributeMessage = string.Empty;
            if (!skill.ContributesToSkillCap)
            {
                decayLock = string.Empty;
                noContributeMessage = _color.Green("This skill does not contribute to your cumulative skill cap.") + "\n\n";
            }

            return
                    _color.Green("Skill: ") + skill.Name + "\n" +
                    _color.Green("Rank: ") + title + "\n" +
                    _color.Green("Exp: ") + _menu.BuildBar(pcSkill.XP, req.XP, 100, _color.TokenStart(255, 127, 0)) + "\n" +
                    noContributeMessage +
                    decayLock + "\n\n" +
                    _color.Green("Description: ") + skill.Description + "\n";
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
            }
        }

        public override void Back(NWPlayer player, string beforeMovePage, string afterMovePage)
        {
        }

        private void HandleCategoryResponse(int responseID)
        {
            Model vm = GetDialogCustomData<Model>();
            DialogResponse response = GetResponseByID("CategoryPage", responseID);
            int categoryID = (int)response.CustomData;
            
            vm.SelectedCategoryID = categoryID;
            LoadSkillResponses();
            ChangePage("SkillListPage");
        }

        private void HandleSkillListResponse(int responseID)
        {
            Model vm = GetDialogCustomData<Model>();
            DialogResponse response = GetResponseByID("SkillListPage", responseID);
            int skillID = (int)response.CustomData;
            
            vm.SelectedSkillID = skillID;
            LoadSkillDetails();
            ChangePage("SkillDetailsPage");
        }

        private void HandleSkillDetailsResponse(int responseID)
        {
            Model vm = GetDialogCustomData<Model>();
            
            switch (responseID)
            {
                case 1: // Toggle Lock
                    _skill.ToggleSkillLock(GetPC().GlobalID, vm.SelectedSkillID);
                    LoadSkillDetails();
                    break;
            }

        }

        public override void EndDialog()
        {
        }
    }
}
