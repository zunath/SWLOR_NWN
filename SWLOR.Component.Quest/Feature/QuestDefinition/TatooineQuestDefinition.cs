using SWLOR.Component.Quest.Contracts;
using SWLOR.Component.Quest.Service;
using SWLOR.Shared.Domain.Common.Enums;
using SWLOR.Shared.Domain.Quest.Contracts;

namespace SWLOR.Component.Quest.Feature.QuestDefinition
{
    public class TatooineQuestDefinition : IQuestListDefinition
    {
        private readonly IQuestBuilderFactory _questBuilderFactory;

        public TatooineQuestDefinition(IQuestBuilderFactory questBuilderFactory)
        {
            _questBuilderFactory = questBuilderFactory;
        }

        public Dictionary<string, IQuestDetail> BuildQuests()
        {
            var builder = _questBuilderFactory.Create();
            WorkinForTheMan(builder);
            StinkyWomprats(builder);
            TuskenRampage(builder);

            return builder.Build();
        }

        private static void WorkinForTheMan(IQuestBuilder builder)
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

        private static void StinkyWomprats(IQuestBuilder builder)
        {
            builder.Create("stinky_womprats", "Stinky Womprats")

                .AddState()
                .SetStateJournalText("You've agreed to take care of those pesky, stinky, womprats. Slay them and return 10 hides to Haderach in Anchorhead.")
                .AddCollectItemObjective("womprathide", 10)

                .AddGoldReward(1000)
                .AddXPReward(1750);
        }

        private static void TuskenRampage(IQuestBuilder builder)
        {
            builder.Create("tusken_rampage", "Tusken Rampage")

                .AddState()
                .SetStateJournalText("The Militia wants you to kill one hundred and fifty Tusken Raiders.")
                .AddKillObjective(NPCGroupType.Tatooine_TuskenRaider, 150)

                .AddState()
                .SetStateJournalText("That's all those Tuskens taken care of. Go talk to that man!")

                .AddGoldReward(7500)
                .AddXPReward(7500)
                .AddItemReward("recipe_hazrdwall", 1);
        }
    }
}
