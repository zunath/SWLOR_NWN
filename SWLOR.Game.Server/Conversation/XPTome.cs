using System.Linq;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Legacy;
using SWLOR.Game.Server.ValueObject.Dialog;

namespace SWLOR.Game.Server.Conversation
{
    public class XPTome: ConversationBase
    {
        private class Model
        {
            public NWItem Item { get; set; }
            public int SkillID { get; set; }
        }


        public override PlayerDialog SetUp(NWPlayer player)
        {
            var dialog = new PlayerDialog("CategoryPage");

            var categoryPage = new DialogPage(
                "This tome holds techniques lost to the ages. Select a skill to earn experience in that art."
            );

            var skillListPage = new DialogPage(
                "This tome holds techniques lost to the ages. Select a skill to earn experience in that art."
            );

            var confirmPage = new DialogPage(
                "<SET LATER>",
                "Select this skill."
            );

            dialog.AddPage("CategoryPage", categoryPage);
            dialog.AddPage("SkillListPage", skillListPage);
            dialog.AddPage("ConfirmPage", confirmPage);

            return dialog;
        }

        public override void Initialize()
        {
            var categories = SkillService.GetActiveCategories().Where(x =>
            {
                var skills = DataService.Skill.GetByCategoryIDAndContributesToSkillCap(x.ID);
                return skills.Any();
            }).ToList();

            foreach (var category in categories)
            {
                AddResponseToPage("CategoryPage", category.Name, true, category.ID);
            }

            var vm = GetDialogCustomData<Model>();
            vm.Item = (GetPC().GetLocalObject("XP_TOME_OBJECT"));
            SetDialogCustomData(vm);
        }

        public override void DoAction(NWPlayer player, string pageName, int responseID)
        {
            switch (pageName)
            {
                case "CategoryPage":
                    HandleCategoryPageResponse(responseID);
                    break;
                case "SkillListPage":
                    HandleSkillListResponse(responseID);
                    break;
                case "ConfirmPage":
                    HandleConfirmPageResponse();
                    break;
            }
        }

        public override void Back(NWPlayer player, string beforeMovePage, string afterMovePage)
        {
        }

        private void HandleCategoryPageResponse(int responseID)
        {
            var response = GetResponseByID("CategoryPage", responseID);
            var categoryID = (int)response.CustomData;
            
            var pcSkills = SkillService.GetPCSkillsForCategory(GetPC().GlobalID, categoryID);

            ClearPageResponses("SkillListPage");
            foreach (var pcSkill in pcSkills)
            {
                var skill = SkillService.GetSkill(pcSkill.SkillID);
                AddResponseToPage("SkillListPage", skill.Name, true, pcSkill.SkillID);
            }

            ChangePage("SkillListPage");
        }

        private void HandleSkillListResponse(int responseID)
        {
            var response = GetResponseByID("SkillListPage", responseID);
            var skillID = (int)response.CustomData;
            var skill = SkillService.GetSkill(skillID);
            var header = "Are you sure you want to improve your " + skill.Name + " skill?";
            SetPageHeader("ConfirmPage", header);

            var vm = GetDialogCustomData<Model>();
            vm.SkillID = skillID;
            SetDialogCustomData(vm);
            ChangePage("ConfirmPage");
        }

        private void HandleConfirmPageResponse()
        {
            var vm = GetDialogCustomData<Model>();

            if (vm.Item != null && vm.Item.IsValid)
            {
                var skill = DataService.Skill.GetByID(vm.SkillID);
                if (!skill.ContributesToSkillCap)
                {
                    GetPC().FloatingText("You cannot raise that skill with this tome.");
                    return;
                }

                var xp = vm.Item.GetLocalInt("XP_TOME_AMOUNT");
                SkillService.GiveSkillXP(GetPC(), vm.SkillID, xp, false, false);
                vm.Item.Destroy();
            }

            EndConversation();
        }

        public override void EndDialog()
        {
        }
    }
}
