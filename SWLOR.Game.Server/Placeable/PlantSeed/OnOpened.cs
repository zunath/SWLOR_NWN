﻿using NWN;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;

using Object = NWN.Object;

namespace SWLOR.Game.Server.Placeable.PlantSeed
{
    public class OnOpened: IRegisteredEvent
    {
        public bool Run(params object[] args)
        {
            NWPlaceable container = (Object.OBJECT_SELF);
            NWPlayer oPC = (_.GetLastUsedBy());
            container.IsUseable = false;
            oPC.SendMessage("Place a seed inside to plant it here. Walk away to cancel planting.");
            return true;
        }
    }
}
