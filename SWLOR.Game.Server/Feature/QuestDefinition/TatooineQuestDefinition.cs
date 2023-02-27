using System.Collections.Generic;
using SWLOR.Game.Server.Service.NPCService;
using SWLOR.Game.Server.Service.QuestService;

namespace SWLOR.Game.Server.Feature.QuestDefinition
{
    public class TatooineQuestDefinition : IQuestListDefinition
    {
        public Dictionary<string, QuestDetail> BuildQuests()
        {
            var builder = new QuestBuilder();
            WorkinForTheMan(builder);
            StinkyWomprats(builder);

            return builder.Build();
        }

        private static void WorkinForTheMan(QuestBuilder builder)
        {
            builder.Create("workin_for_man", "Workin' for the Man")

                .IsRepeatable()

                .AddState()
                .SetStateJournalText("You've been recruited by Czerka to take care of their Tusken problem. Explore the dunes of Tatooine and thin their numbers.")
                .AddKillObjective(NPCGroupType.Tatooine_TuskenRaider, 20)

                .AddState()
                .SetStateJournalText("Report back to the dockmaster.")

                .AddGoldReward(500)
                .AddXPReward(1750);
        }

        private static void StinkyWomprats(QuestBuilder builder)
        {
            builder.Create("stinky_womprats", "Stinky Womprats")

                .AddState()
                .SetStateJournalText("You've agreed to take care of those pesky, stinky, womprats. Track down as many as you can!")
                .AddCollectItemObjective("womprathide", 10)

                .AddState()
                .SetStateJournalText("Return to Haderach in Anchorhead for your reward.")

                .AddGoldReward(1000)
                .AddXPReward(1750);
        }
    }
}