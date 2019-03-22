﻿
using NWN;
using SWLOR.Game.Server.Service.Contracts;
using Object = NWN.Object;

namespace SWLOR.Game.Server.Event.Module
{
    internal class OnModuleActivateItem : IRegisteredEvent
    {
        
        private readonly IItemService _item;

        public OnModuleActivateItem(
            
            IItemService item)
        {
            
            _item = item;
        }

        public bool Run(params object[] args)
        {
            _.ExecuteScript("x2_mod_def_act", Object.OBJECT_SELF);
            _item.OnModuleActivatedItem();
            return true;
        }


    }
}
