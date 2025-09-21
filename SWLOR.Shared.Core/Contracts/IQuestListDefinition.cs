using System.Collections.Generic;
using SWLOR.Shared.Core.Models;

namespace SWLOR.Shared.Core.Contracts
{
    public interface IQuestListDefinition
    {
        public Dictionary<string, QuestDetail> BuildQuests();
    }
}
