
using NWN;
using SWLOR.Game.Server.Service;


namespace SWLOR.Game.Server.Event.Module
{
    internal class OnModuleAcquireItem: IRegisteredEvent
    {
        public bool Run(params object[] args)
        {
            // Bioware Default
            _.ExecuteScript("x2_mod_def_aqu", Object.OBJECT_SELF);
            KeyItemService.OnModuleItemAcquired();
            QuestService.OnModuleItemAcquired();
            return true;

        }
    }
}
