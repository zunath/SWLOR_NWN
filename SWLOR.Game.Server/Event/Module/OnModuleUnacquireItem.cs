
using NWN;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Event.Module
{
    internal class OnModuleUnacquireItem : IRegisteredEvent
    {
        private readonly INWScript _;
        private readonly IItemService _item;

        public OnModuleUnacquireItem(INWScript script, IItemService item)
        {
            _ = script;
            _item = item;
        }

        public bool Run(params object[] args)
        {
            _.ExecuteScript("x2_mod_def_unaqu", Object.OBJECT_SELF);
            _item.OnModuleItemAcquired();
            return true;
        }
    }
}
