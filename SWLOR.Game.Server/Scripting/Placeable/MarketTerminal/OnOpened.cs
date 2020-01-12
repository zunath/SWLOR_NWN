﻿using NWN;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWScript;
using SWLOR.Game.Server.Scripting.Contracts;
using SWLOR.Game.Server.Service;
using _ = SWLOR.Game.Server.NWScript._;

namespace SWLOR.Game.Server.Scripting.Placeable.MarketTerminal
{
    public class OnOpened: IScript
    {
        public void SubscribeEvents()
        {
        }

        public void UnsubscribeEvents()
        {
        }

        public void Main()
        {
            NWPlaceable device = NWGameObject.OBJECT_SELF;
            NWPlayer player = _.GetLastOpenedBy();
            var model = MarketService.GetPlayerMarketData(player);

            if (model.IsSellingItem)
            {
                player.FloatingText("Please place an item you wish to sell inside of the terminal.");
            }

            device.IsLocked = true;
        }
    }
}
