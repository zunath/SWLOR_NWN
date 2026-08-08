using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.ConversationService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.DialogDefinition
{
    public class XPTomeDialog: ConversationMenuDefinition
    {
        private class Model
        {
            public uint Item { get; set; }
            public SkillCategoryType Category { get; set; }
            public SkillType Skill { get; set; }
        }

        private const string CategoryPageId = "CATEGORY_PAGE";
        private const string SkillListPageId = "SKILL_LIST_PAGE";
        private const string ConfirmationPageId = "CONFIRMATION_PAGE";

        public override ConversationMenuSpec Build()
        {
            var builder = new ConversationMenuBuilder()
                .WithDataModel(new Model())
                .AddInitializationAction(Initialize)
                .AddPage(CategoryPageId, CategoryPageInit)
                .AddPage(SkillListPageId, SkillListPageInit)
                .AddPage(ConfirmationPageId, ConfirmationPageInit);


            return builder.Build();
        }

        private void Initialize()
        {
            var model = Data<Model>();
            var player = Player;
            model.Item = GetLocalObject(player, "XP_TOME_OBJECT");
        }

        private void CategoryPageInit(ConversationMenuPage page)
        {
            page.Header = "This tome holds techniques lost to the ages. Select a skill to earn experience in that art.";

            foreach (var (type, detail) in Skill.GetAllActiveSkillCategories())
            {
                page.AddResponse(detail.Name, () =>
                {
                    var model = Data<Model>();
                    model.Category = type;
                    GoToPage(SkillListPageId);
                });
            }

        }

        private void SkillListPageInit(ConversationMenuPage page)
        {
            var model = Data<Model>();
            page.Header = "This tome holds techniques lost to the ages. Select a skill to earn experience in that art.";

            foreach (var (type, detail) in Skill.GetAllSkillsByCategory(model.Category))
            {
                page.AddResponse(detail.Name, () =>
                {
                    model.Skill = type;
                    GoToPage(ConfirmationPageId);
                });
            }
        }

        private void ConfirmationPageInit(ConversationMenuPage page)
        {
            var player = Player;
            var model = Data<Model>();
            var skillDetail = Skill.GetSkillDetails(model.Skill);
            page.Header = $"Are you sure you want to improve your {skillDetail.Name} skill?";

            page.AddResponse("Select this skill.", () =>
            {
                var amount = GetLocalInt(model.Item, "XP_TOME_AMOUNT");
                Skill.GiveSkillXP(player, model.Skill, amount, false, false);
                DestroyObject(model.Item);

                Close();
            });
        }
    }
}
