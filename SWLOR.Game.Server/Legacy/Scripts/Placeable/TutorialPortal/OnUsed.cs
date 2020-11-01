using System.Linq;
using SWLOR.Game.Server.Legacy.GameObject;
using SWLOR.Game.Server.Legacy.Service;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Legacy.Scripts.Placeable.TutorialPortal
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
            NWPlayer player = (GetLastUsedBy());
            NWPlaceable warp = (OBJECT_SELF);
            var isExit = warp.GetLocalBool("IS_EXIT") == true;

            if (isExit)
            {
                var entity = PlayerService.GetPlayerEntity(player.GlobalID);
                var area = NWModule.Get().Areas.Single(x => GetResRef(x) == entity.LocationAreaResref);
                var position = Vector3((float) entity.LocationX, (float) entity.LocationY, (float) entity.LocationZ);
                var location = Location(area,
                    position,
                    (float) entity.LocationOrientation);

                player.AssignCommand(() => ActionJumpToLocation(location));
            }
            else
            {
                PlayerService.SaveLocation(player);
                NWObject waypoint = (GetWaypointByTag("TUTORIAL_WP"));
                player.AssignCommand(() => ActionJumpToLocation(waypoint.Location));
            }
        }
    }
}
