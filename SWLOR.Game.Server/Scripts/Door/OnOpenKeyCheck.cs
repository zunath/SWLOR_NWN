using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWN.Enum;
using SWLOR.Game.Server.NWN.Enum.Item;
using SWLOR.Game.Server.Service;
using System.Linq;
using static SWLOR.Game.Server.NWN._;

namespace SWLOR.Game.Server.Scripts.Door
{
    public class OnOpenKeyCheck : IScript
    {
        public void SubscribeEvents()
        {
        }

        public void UnsubscribeEvents()
        {
        }

        public void Main()
        {
            NWPlayer player = GetLastOpenedBy();
            var door = OBJECT_SELF;
            var keyTag = GetLocalString(door, "REQ_KEY_TAG");
            int highestMatchingKeyTier = 0;

            if (!string.IsNullOrEmpty(keyTag))
            {
                var keys = player.InventoryItems.ToList().FindAll(i => i.Tag == keyTag);
                if (keys.Count > 0)
                {
                    highestMatchingKeyTier = keys.Aggregate(0, (currentHighest, key) => 
                    {
                        var tier = GetLocalInt(key, "TIER");
                        return tier > currentHighest ? tier : currentHighest;
                    });    
                }
            }

            var expectedKeyTier = GetLocalInt(door, "REQ_KEY_TIER");

            // if user not allowed, close the door and optionally send a message
            if (highestMatchingKeyTier < expectedKeyTier)
            {
                ActionCloseDoor(door);
                var msg = GetLocalString(door, "REQ_KEY_FAIL_MSG");
                if (!string.IsNullOrEmpty(msg)) SpeakString(msg);
            }
        }
    }
}
