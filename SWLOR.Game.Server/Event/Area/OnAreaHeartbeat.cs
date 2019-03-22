﻿
using System;
using System.Linq;
using NWN;
using SWLOR.Game.Server.GameObject;
using Object = NWN.Object;

namespace SWLOR.Game.Server.Event.Area
{
    internal class OnAreaHeartbeat: IRegisteredEvent
    {
        public bool Run(params object[] args)
        {
            HideMinimap();
            return true;
        }

        private void HideMinimap()
        {
            NWArea area = Object.OBJECT_SELF;
            if(area.GetLocalInt("HIDE_MINIMAP") == _.TRUE)
            {
                var players = NWModule.Get().Players.Where(x => x.Area.Equals(area) && x.IsPlayer);

                foreach(var player in players)
                {
                    _.ExploreAreaForPlayer(area, player, _.FALSE);
                }
            }

        }

    }
}
