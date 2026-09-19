using System.Collections.Generic;
using SWLOR.Game.Server.Service.KeyItemService;
using SWLOR.Game.Server.Service.QuestService;

namespace SWLOR.Game.Server.Feature.QuestDefinition
{
    public class HiddenAccessQuestDefinition : IQuestListDefinition
    {
        private readonly QuestBuilder _builder = new();
        public Dictionary<string, QuestDetail> BuildQuests()
        {
            SithBasementQuest();
            return _builder.Build();
        }

        private void SithBasementQuest()
        {
            _builder.Create("sith_basement", "Viscara Sith Basement")

                .AddState()
                .SetStateJournalText("Speak to your contact again to receive the Viscara Lake Basement Key.")

                .AddKeyItemReward(KeyItemType.SithBasementKey);
        }
    }
}
