using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NWN;
using SWLOR.Game.Server.GameObject;
using static NWN.NWScript;
using Object = NWN.Object;

namespace SWLOR.Game.Server.Event.Module
{
    public class OnModuleEnterStealthAfter: IRegisteredEvent
    {
        private readonly INWScript _;

        public OnModuleEnterStealthAfter(INWScript script)
        {
            _ = script;
        }

        public bool Run(params object[] args)
        {
            NWObject stealther = Object.OBJECT_SELF;
            _.SetActionMode(stealther, ACTION_MODE_STEALTH, FALSE);
            _.FloatingTextStringOnCreature("NWN stealth mode is disabled on this server.", stealther, FALSE);
            return true;
        }
    }
}
