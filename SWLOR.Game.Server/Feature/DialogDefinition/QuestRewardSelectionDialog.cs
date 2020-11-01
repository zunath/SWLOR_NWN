using System.Linq;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DialogService;
using SWLOR.Game.Server.Service.QuestService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

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
                .AddPage(MainPageId, MainPageInit);

            return builder.Build();
        }

        private void MainPageInit(DialogPage page)
        {
            Model model = GetDataModel<Model>();
            var quest = Quest.GetQuestById(model.QuestId);

            void HandleRewardSelection(IQuestReward reward)
            {
                quest.Complete(GetPC(), GetPC(), reward);
                EndConversation();
            }
            page.Header = "Please select a reward.";

            var player = GetPC();
            string questId = GetLocalString(player, "QST_REWARD_SELECTION_QUEST_ID");
            DeleteLocalString(player, "QST_REWARD_SELECTION_QUEST_ID");
            var rewardItems = quest.GetRewards().Where(x => x.IsSelectable);
            model.QuestId = questId;

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
