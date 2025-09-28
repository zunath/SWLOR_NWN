using Microsoft.Extensions.DependencyInjection;
using SWLOR.Shared.Domain.Dialog.Contracts;
using SWLOR.Shared.Domain.Dialog.ValueObjects;
using SWLOR.Shared.Domain.Skill.Contracts;
using SWLOR.Shared.Domain.Skill.Enums;

namespace SWLOR.Component.Inventory.Dialog
{
    public class XPTomeDialog: DialogBase
    {
        private readonly IServiceProvider _serviceProvider;

        public XPTomeDialog(IServiceProvider serviceProvider, IDialogService dialogService, IDialogBuilder dialogBuilder) : base(dialogService, dialogBuilder)
        {
            _serviceProvider = serviceProvider;
            
            // Initialize lazy services
            _skillService = new Lazy<ISkillService>(() => _serviceProvider.GetRequiredService<ISkillService>());
        }

        // Lazy-loaded service to break circular dependency
        private readonly Lazy<ISkillService> _skillService;
        
        private ISkillService SkillService => _skillService.Value;

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
            var builder = DialogBuilder
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

            foreach (var (type, detail) in SkillService.GetAllActiveSkillCategories())
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

            foreach (var (type, detail) in SkillService.GetAllSkillsByCategory(model.Category))
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
            var skillDetail = SkillService.GetSkillDetails(model.Skill);
            page.Header = $"Are you sure you want to improve your {skillDetail.Name} skill?";

            page.AddResponse("Select this skill.", () =>
            {
                var amount = GetLocalInt(model.Item, "XP_TOME_AMOUNT");
                SkillService.GiveSkillXP(player, model.Skill, amount, false, false);
                DestroyObject(model.Item);

                EndConversation();
            });
        }
    }
}
