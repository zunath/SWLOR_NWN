﻿using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;
using NWN;

namespace SWLOR.Game.Server.Placeable.CraftingForge
{
    public class OnHeartbeat: IRegisteredEvent
    {
        public bool Run(params object[] args)
        {
            NWPlaceable forge = (NWGameObject.OBJECT_SELF);
            int charges = forge.GetLocalInt("FORGE_CHARGES");

            if (charges > 0)
            {
                charges--;
                forge.SetLocalInt("FORGE_CHARGES", charges);
            }

            if (charges <= 0)
            {
                NWPlaceable flames = (forge.GetLocalObject("FORGE_FLAMES"));
                flames.Destroy();
            }
            return true;
        }
    }
}
