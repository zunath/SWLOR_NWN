using System.Linq;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DialogService;
using SWLOR.Game.Server.Service.QuestService;

namespace SWLOR.Game.Server.Feature.DialogDefinition
{
    public class QuestRewardSelectionDialog: DialogBase
    {
        private class Model
        {
            public string QuestId { get; set; }
        }

        private const string MainPageId = "MAIN";

        public override PlayerDialog SetUp(uint player)
        {
            var builder = new DialogBuilder()
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
            var quest = Quest.GetQuestById(model.QuestId);

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
