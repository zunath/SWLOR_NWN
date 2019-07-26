using System.Linq;
using NWN;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Scripting.Contracts;

namespace SWLOR.Game.Server.Scripts.Placeable.Quests.AbandonedStation
{
    public class StationKeyCard: IScript
    {
        public void SubscribeEvents()
        {
        }

        public void UnsubscribeEvents()
        {
        }

        public void Main()
        {
            NWPlaceable keyCard = NWGameObject.OBJECT_SELF;
            NWArea area = keyCard.Area;
            NWPlayer player = _.GetLastUsedBy();
            int remaining = area.GetLocalInt("KEY_CARDS_REMAINING") - 1;
            area.SetLocalInt("KEY_CARDS_REMAINING", remaining);
            
            var members = player.PartyMembers.Where(x => x.Area == area);
            if (remaining <= 0)
            {
                foreach(var member in members)
                {
                    member.FloatingText("All key cards have been found. The elevator to the next level can now be unlocked.");
                }
            }
            else
            {
                foreach(var member in members)
                {
                    member.FloatingText("A key card was found by " + player.Name + ".");
                }
            }

            keyCard.Destroy();
        }
    }
}
