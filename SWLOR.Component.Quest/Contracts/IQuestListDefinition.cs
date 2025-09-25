using SWLOR.Component.Quest.Service;
using SWLOR.Shared.Domain.Quest.Contracts;

namespace SWLOR.Component.Quest.Contracts
{
    public interface IQuestListDefinition
    {
        public Dictionary<string, IQuestDetail> BuildQuests();
    }
}
