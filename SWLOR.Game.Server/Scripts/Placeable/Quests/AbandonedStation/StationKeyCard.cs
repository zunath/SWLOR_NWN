using System.Linq;
using SWLOR.Game.Server.GameObject;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

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
            NWPlaceable keyCard = OBJECT_SELF;
            var area = keyCard.Area;
            NWPlayer player = GetLastUsedBy();
            var remaining = GetLocalInt(area, "KEY_CARDS_REMAINING") - 1;
            SetLocalInt(area, "KEY_CARDS_REMAINING", remaining);
            
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
