﻿using NWN;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Scripting.Contracts;

namespace SWLOR.Game.Server.Scripting.Scripts.Placeable.DisabledStructure
{
    public class OnUsed: IScript
    {
        public void SubscribeEvents()
        {
        }

        public void UnsubscribeEvents()
        {
        }

        public void Main()
        {
            NWPlayer user = (_.GetLastUsedBy());

            user.SendMessage("The base is currently out of fuel and this object cannot be powered online.");

        }
    }
}
