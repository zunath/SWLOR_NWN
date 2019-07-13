using System.Collections.Generic;
using NWN;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.ValueObject;

namespace SWLOR.Game.Server.Event.Conversation.KeyItem
{
    public static class KeyItemCheck
    {
        public static bool Check(int index, int type)
        {
            using (new Profiler(nameof(KeyItemCheck)))
            {
                NWPlayer player = _.GetPCSpeaker();
                NWObject talkingTo = NWGameObject.OBJECT_SELF;

                int count = 1;
                List<int> requiredKeyItemIDs = new List<int>();

                int keyItemID = talkingTo.GetLocalInt($"KEY_ITEM_{index}_REQ_{count}");

                while (keyItemID > 0)
                {
                    requiredKeyItemIDs.Add(keyItemID);

                    count++;
                    keyItemID = talkingTo.GetLocalInt($"KEY_ITEM_{index}_REQ_{count}");
                }

                // Type 1 = ALL
                // Anything else = ANY
                return type == 1 ?
                    KeyItemService.PlayerHasAllKeyItems(player, requiredKeyItemIDs.ToArray()) :
                    KeyItemService.PlayerHasAnyKeyItem(player, requiredKeyItemIDs.ToArray());
            }
        }
    }
}
