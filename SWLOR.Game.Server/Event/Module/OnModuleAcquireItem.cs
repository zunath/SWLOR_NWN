
using NWN;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Event.Module
{
    internal class OnModuleAcquireItem: IRegisteredEvent
    {
        private readonly INWScript _;
        private readonly IKeyItemService _keyItem;
        private readonly IQuestService _quest;

        public OnModuleAcquireItem(
            INWScript script, 
            IKeyItemService keyItem,
            IQuestService quest)
        {
            _ = script;
            _keyItem = keyItem;
            _quest = quest;
        }

        public bool Run(params object[] args)
        {
            // Bioware Default
            _.ExecuteScript("x2_mod_def_aqu", Object.OBJECT_SELF);
            _keyItem.OnModuleItemAcquired();
            _quest.OnModuleItemAcquired();
            return true;

        }
    }
}
