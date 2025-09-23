using SWLOR.Shared.Dialog.Contracts;
using SWLOR.Shared.Dialog.Model;
using SWLOR.Shared.Dialog.Service;
using SWLOR.Shared.Domain.Enums;

namespace SWLOR.Component.Inventory.Dialog
{
    public class XPTomeDialog: DialogBase
    {
        private readonly ISkillService _skillService;

        public XPTomeDialog(ISkillService skillService, IDialogService dialogService) 
            : base(dialogService)
        {
            _skillService = skillService;
        }

        private class Model
        {
            public uint Item { get; set; }
            public SkillCategoryType Category { get; set; }
            public SkillType Skill { get; set; }
        }

        private const string CategoryPageId = "CATEGORY_PAGE";
        private const string SkillListPageId = "SKILL_LIST_PAGE";
        private const string ConfirmationPageId = "CONFIRMATION_PAGE";

        public override PlayerDialog SetUp(uint player)
        {
            var builder = new DialogBuilder()
                .WithDataModel(new Model())
                .AddInitializationAction(Initialize)
                .AddPage(CategoryPageId, CategoryPageInit)
                .AddPage(SkillListPageId, SkillListPageInit)
                .AddPage(ConfirmationPageId, ConfirmationPageInit);


            return builder.Build();
        }

        private void Initialize()
        {
            var model = GetDataModel<Model>();
            var player = GetPC();
            model.Item = GetLocalObject(player, "XP_TOME_OBJECT");
        }

        private void CategoryPageInit(DialogPage page)
        {
            page.Header = "This tome holds techniques lost to the ages. Select a skill to earn experience in that art.";

            foreach (var (type, detail) in _skillService.GetAllActiveSkillCategories())
            {
                page.AddResponse(detail.Name, () =>
                {
                    var model = GetDataModel<Model>();
                    model.Category = type;
                    ChangePage(SkillListPageId);
                });
            }

        }

        private void SkillListPageInit(DialogPage page)
        {
            var model = GetDataModel<Model>();
            page.Header = "This tome holds techniques lost to the ages. Select a skill to earn experience in that art.";

            foreach (var (type, detail) in _skillService.GetAllSkillsByCategory(model.Category))
            {
                page.AddResponse(detail.Name, () =>
                {
                    model.Skill = type;
                    ChangePage(ConfirmationPageId);
                });
            }
        }

        private void ConfirmationPageInit(DialogPage page)
        {
            var player = GetPC();
            var model = GetDataModel<Model>();
            var skillDetail = _skillService.GetSkillDetails(model.Skill);
            page.Header = $"Are you sure you want to improve your {skillDetail.Name} skill?";

            page.AddResponse("Select this skill.", () =>
            {
                var amount = GetLocalInt(model.Item, "XP_TOME_AMOUNT");
                _skillService.GiveSkillXP(player, model.Skill, amount, false, false);
                DestroyObject(model.Item);

                EndConversation();
            });
        }
    }
}
