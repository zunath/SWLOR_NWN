using System;
using System.Collections.Generic;
using NWN;
using SWLOR.Game.Server.Data.Entities;
using SWLOR.Game.Server.GameObject;

using SWLOR.Game.Server.Service.Contracts;
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
                "Toggle Decay Lock",
                "Back"
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
            AddResponseToPage("CategoryPage", "Back", true, -1);
        }

        private void LoadSkillResponses()
        {
            Model vm = GetDialogCustomData<Model>();
            List<PCSkill> skills = _skill.GetPCSkillsForCategory(GetPC().GlobalID, vm.SelectedCategoryID);

            ClearPageResponses("SkillListPage");
            foreach (PCSkill skill in skills)
            {
                AddResponseToPage("SkillListPage", skill.Skill.Name + " (Lvl. " + skill.Rank + ")", true, skill.Skill.SkillID);
            }
            AddResponseToPage("SkillListPage", "Back", true, -1);
        }

        private void LoadSkillDetails()
        {
            Model vm = GetDialogCustomData<Model>();
            PCSkill pcSkill = _skill.GetPCSkillByID(GetPC().GlobalID, vm.SelectedSkillID);
            SkillXPRequirement req = _skill.GetSkillXPRequirementByRank(vm.SelectedSkillID, pcSkill.Rank);
            SetPageHeader("SkillDetailsPage", CreateSkillDetailsHeader(pcSkill, req));
        }

        private string CreateSkillDetailsHeader(PCSkill pcSkill, SkillXPRequirement req)
        {
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

            string decayLock = _color.White("Unlocked");
            if (pcSkill.IsLocked)
            {
                decayLock = _color.Red("Locked");
            }

            return
                    _color.Green("Skill: ") + pcSkill.Skill.Name + "\n" +
                    _color.Green("Rank: ") + title + "\n" +
                    _color.Green("Exp: ") + _menu.BuildBar(pcSkill.XP, req.XP, 100, _color.TokenStart(255, 127, 0)) + "\n" +
                    _color.Green("Decay Lock: ") + decayLock + "\n\n" +
                    _color.Green("Description: ") + pcSkill.Skill.Description + "\n";
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

        private void HandleCategoryResponse(int responseID)
        {
            Model vm = GetDialogCustomData<Model>();
            DialogResponse response = GetResponseByID("CategoryPage", responseID);
            int categoryID = (int)response.CustomData;
            if (categoryID == -1) // Back
            {
                SwitchConversation("RestMenu");
                return;
            }

            vm.SelectedCategoryID = categoryID;
            LoadSkillResponses();
            ChangePage("SkillListPage");
        }

        private void HandleSkillListResponse(int responseID)
        {
            Model vm = GetDialogCustomData<Model>();
            DialogResponse response = GetResponseByID("SkillListPage", responseID);
            int skillID = (int)response.CustomData;
            if (skillID == -1) // Back
            {
                ChangePage("CategoryPage");
                return;
            }
            
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
                case 2: // Back
                    ChangePage("SkillListPage");
                    break;
            }

        }

        public override void EndDialog()
        {
        }
    }
}
