using System.Linq;
using NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWScript;
using SWLOR.Game.Server.Scripting.Contracts;
using SWLOR.Game.Server.Service;
using _ = SWLOR.Game.Server.NWScript._;

namespace SWLOR.Game.Server.Scripting.Placeable.TutorialPortal
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
            NWPlaceable warp = (NWGameObject.OBJECT_SELF);
            bool isExit = warp.GetLocalBoolean("IS_EXIT") == true;

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
        }
    }
}
