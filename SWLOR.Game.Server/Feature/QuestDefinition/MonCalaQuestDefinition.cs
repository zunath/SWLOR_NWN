using System.Collections.Generic;
using SWLOR.Game.Server.Service.QuestService;

namespace SWLOR.Game.Server.Feature.QuestDefinition
{
    public class MonCalaQuestDefinition : IQuestListDefinition
    {
        public Dictionary<string, QuestDetail> BuildQuests()
        {
            var builder = new QuestBuilder();

            return builder.Build();
        }
    }
}