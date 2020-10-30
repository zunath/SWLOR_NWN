using System.Linq;
using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Legacy;

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
            NWPlayer player = (NWScript.GetLastUsedBy());
            NWPlaceable warp = (NWScript.OBJECT_SELF);
            var isExit = warp.GetLocalBool("IS_EXIT") == true;

            if (isExit)
            {
                var entity = PlayerService.GetPlayerEntity(player.GlobalID);
                var area = NWModule.Get().Areas.Single(x => x.Resref == entity.LocationAreaResref);
                var position = NWScript.Vector3((float) entity.LocationX, (float) entity.LocationY, (float) entity.LocationZ);
                var location = NWScript.Location(area.Object,
                    position,
                    (float) entity.LocationOrientation);

                player.AssignCommand(() => NWScript.ActionJumpToLocation(location));
            }
            else
            {
                PlayerService.SaveLocation(player);
                NWObject waypoint = (NWScript.GetWaypointByTag("TUTORIAL_WP"));
                player.AssignCommand(() => NWScript.ActionJumpToLocation(waypoint.Location));
            }
        }
    }
}
