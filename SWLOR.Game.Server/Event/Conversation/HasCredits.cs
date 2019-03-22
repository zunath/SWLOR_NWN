using System;
using System.Collections.Generic;
using System.Linq;
using NWN;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;
using Object = NWN.Object;

namespace SWLOR.Game.Server.Event.Conversation
{
    public class HasCredits : IRegisteredEvent
    {
        public bool Run(params object[] args)
        {
            NWPlayer oPC = _.GetPCSpeaker();
            NWObject oNPC = Object.OBJECT_SELF;
            int nGold = _.GetGold(oPC);
            int reqGold = _.GetLocalInt(oNPC,"gold");
            if (nGold > reqGold)
            {
                return true;
            }                     
            return false;
        }
    }
}
