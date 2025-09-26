using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Quest.Contracts;
using SWLOR.Shared.Domain.Dialog.Contracts;
using SWLOR.Shared.Domain.Dialog.ValueObjects;
using SWLOR.Shared.Domain.Quest.Contracts;

namespace SWLOR.Component.Quest.Dialog
{
    public class QuestRewardSelectionDialog: DialogBase
    {
        private readonly IServiceProvider _serviceProvider;

        public QuestRewardSelectionDialog(IServiceProvider serviceProvider, IDialogService dialogService, IDialogBuilder dialogBuilder) : base(dialogService, dialogBuilder)
        {
            _serviceProvider = serviceProvider;
        }

        // Lazy-loaded service to break circular dependency
        private IQuestService QuestService => _serviceProvider.GetRequiredService<IQuestService>();

        private class Model
        {
            public string QuestId { get; set; }
        }

        private const string MainPageId = "MAIN";

        public override PlayerDialog SetUp(uint player)
        {
            var builder = DialogBuilder
                .WithDataModel(new Model())
                .AddInitializationAction(Initialize)
                .AddPage(MainPageId, MainPageInit);

            return builder.Build();
        }

        private void Initialize()
        {
            var player = GetPC();
            var questId = GetLocalString(player, "QST_REWARD_SELECTION_QUEST_ID");
            var model = GetDataModel<Model>();

            model.QuestId = questId;
            DeleteLocalString(player, "QST_REWARD_SELECTION_QUEST_ID");
        }

        private void MainPageInit(DialogPage page)
        {
            var model = GetDataModel<Model>();
            var quest = _questService.GetQuestById(model.QuestId);

            void HandleRewardSelection(IQuestReward reward)
            {
                quest.Complete(GetPC(), GetPC(), reward);
                EndConversation();
            }
            page.Header = "Please select a reward.";

            var rewardItems = quest.GetRewards().Where(x => x.IsSelectable);

            foreach (var reward in rewardItems)
            {
                page.AddResponse(reward.MenuName, () =>
                {
                    HandleRewardSelection(reward);
                });
            }
        }
    }
}
