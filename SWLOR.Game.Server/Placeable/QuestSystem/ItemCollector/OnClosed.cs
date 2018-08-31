using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWN.Contracts;
using SWLOR.Game.Server.NWN.NWScript;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Placeable.QuestSystem.ItemCollector
{
    public class OnClosed : IRegisteredEvent
    {
        private readonly INWScript _;
        private readonly IQuestService _quest;

        public OnClosed(IQuestService quest)
        {
            _quest = quest;
        }

        public bool Run(params object[] args)
        {
            NWPlaceable container = NWPlaceable.Wrap(Object.OBJECT_SELF);
            _quest.OnItemCollectorClosed(container);
            return true;
        }
    }
}
