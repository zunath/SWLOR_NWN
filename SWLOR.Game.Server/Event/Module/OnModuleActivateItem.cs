
using NWN;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.Contracts;
using Object = NWN.Object;

namespace SWLOR.Game.Server.Event.Module
{
    internal class OnModuleActivateItem : IRegisteredEvent
    {
        
        

        public OnModuleActivateItem(
            
            )
        {
            
            
        }

        public bool Run(params object[] args)
        {
            _.ExecuteScript("x2_mod_def_act", Object.OBJECT_SELF);
            ItemService.OnModuleActivatedItem();
            return true;
        }


    }
}
