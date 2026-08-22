using System.Linq;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.ConversationService;
using SWLOR.Game.Server.Service.QuestService;

namespace SWLOR.Game.Server.Feature.DialogDefinition
{
    public class QuestRewardSelectionDialog: ConversationMenuDefinition
    {
        private class Model
        {
            public string QuestId { get; set; }
        }

        private const string MainPageId = "MAIN";

        public override ConversationMenuSpec Build()
        {
            var builder = new ConversationMenuBuilder()
                .WithDataModel(new Model())
                .AddInitializationAction(Initialize)
                .AddPage(MainPageId, MainPageInit);

            return builder.Build();
        }

        private void Initialize()
        {
            var player = Player;
            var questId = GetLocalString(player, "QST_REWARD_SELECTION_QUEST_ID");
            var model = Data<Model>();

            model.QuestId = questId;
            DeleteLocalString(player, "QST_REWARD_SELECTION_QUEST_ID");
        }

        private void MainPageInit(ConversationMenuPage page)
        {
            var model = Data<Model>();
            var quest = Quest.GetQuestById(model.QuestId);

            void HandleRewardSelection(IQuestReward reward)
            {
                quest.Complete(Player, Player, reward);
                Close();
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
