using System.Linq;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Service;


namespace SWLOR.Game.Server.Placeable.TutorialPortal
{
    public class OnUsed: IRegisteredEvent
    {
        public bool Run(params object[] args)
        {
            NWPlayer player = (_.GetLastUsedBy());
            NWPlaceable warp = (NWGameObject.OBJECT_SELF);
            bool isExit = warp.GetLocalInt("IS_EXIT") == _.TRUE;

            if (isExit)
            {
                Player entity = PlayerService.GetPlayerEntity(player.GlobalID);
                NWArea area = NWModule.Get().Areas.Single(x => x.Resref == entity.LocationAreaResref);
                Vector position = _.Vector((float) entity.LocationX, (float) entity.LocationY, (float) entity.LocationZ);
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


            return true;
        }
    }
}
