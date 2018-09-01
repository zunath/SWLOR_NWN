using NWN;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;

using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Placeable.QuestSystem
{
    public class OnUsed: IRegisteredEvent
    {
        private readonly INWScript _;
        private readonly IQuestService _quest;

        public OnUsed(IQuestService quest)
        {
            _quest = quest;
        }

        public bool Run(params object[] args)
        {
            NWPlaceable container = NWPlaceable.Wrap(_.GetLastUsedBy());
            _quest.OnQuestPlaceableUsed(container);
            return true;
        }
    }
}
