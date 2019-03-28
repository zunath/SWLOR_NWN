﻿using NWN;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;


namespace SWLOR.Game.Server.Placeable
{
    public class ObtainKeyItem: IRegisteredEvent
    {
        public bool Run(params object[] args)
        {
            NWPlayer player = _.GetLastUsedBy();

            if (!player.IsPlayer) return false;

            NWPlaceable placeable = Object.OBJECT_SELF;
            int keyItemID = placeable.GetLocalInt("KEY_ITEM_ID");
            
            if (keyItemID <= 0) return false;

            if (KeyItemService.PlayerHasKeyItem(player, keyItemID))
            {
                player.SendMessage("You already have this key item.");
                return false;
            }

            KeyItemService.GivePlayerKeyItem(player, keyItemID);

            string visibilityGUID = placeable.GetLocalString("VISIBILITY_OBJECT_ID");
            if (!string.IsNullOrWhiteSpace(visibilityGUID))
            {
                ObjectVisibilityService.AdjustVisibility(player, placeable, false);
            }

            return true;
        }
    }
}
