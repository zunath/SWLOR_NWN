using NWN;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Scripting.Contracts;
using static NWN._;

namespace SWLOR.Game.Server.Scripts.Placeable.Quests.AbandonedStation
{
    public class ElevatorDoor: IScript
    {
        public void SubscribeEvents()
        {
        }

        public void UnsubscribeEvents()
        {
        }

        public void Main()
        {
            NWObject door = NWGameObject.OBJECT_SELF;
            NWArea area = door.Area;
            NWPlayer player = GetClickingObject();
            if (!player.IsValid)
                player = GetLastUsedBy();

            int remainingKeyCards = area.GetLocalInt("KEY_CARDS_REMAINING");

            if (remainingKeyCards > 0)
            {
                player.FloatingText("You need " + remainingKeyCards + "x key card(s) to access this elevator.");
            }
            else
            {
                player.FloatingText("You access the elevator with the found key cards.");
                string destinationWPTag = door.GetLocalString("DESTINATION_WAYPOINT");
                string destinationAreaTag = door.GetLocalString("DESTINATION_AREA_TAG");
                NWArea destinationArea = area.GetLocalObject(destinationAreaTag);
                NWLocation destinationLocation = GetLocation(GetNearestObjectByTag(destinationWPTag, GetFirstObjectInArea(destinationArea)));
                
                player.AssignCommand(() => { ActionJumpToLocation(destinationLocation); });
            }
        }
    }
}
