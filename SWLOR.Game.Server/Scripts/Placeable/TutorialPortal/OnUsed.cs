﻿using System.Linq;
using System.Numerics;
using SWLOR.Game.Server.NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Scripts.Placeable.TutorialPortal
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
            NWPlayer player = (_.GetLastUsedBy());
            NWPlaceable warp = (_.OBJECT_SELF);
            bool isExit = warp.GetLocalBool("IS_EXIT") == true;

            if (isExit)
            {
                Player entity = PlayerService.GetPlayerEntity(player.GlobalID);
                NWArea area = NWModule.Get().Areas.Single(x => x.Resref == entity.LocationAreaResref);
                Vector3 position = _.Vector3((float) entity.LocationX, (float) entity.LocationY, (float) entity.LocationZ);
                Location location = _.Location(area.Object,
                    position,
                    (float) entity.LocationOrientation);

                player.AssignCommand(() => _.ActionJumpToLocation(location));
            }
            else
            {
                PlayerService.SaveLocation(player);
                NWObject waypoint = (_.GetWaypointByTag("TUTORIAL_WP"));
                player.AssignCommand(() => _.ActionJumpToLocation(waypoint.Location));
            }
        }
    }
}
