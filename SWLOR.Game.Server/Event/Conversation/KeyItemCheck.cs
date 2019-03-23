using System.Collections.Generic;
using NWN;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;

using Object = NWN.Object;

namespace SWLOR.Game.Server.Event.Conversation
{
    public class KeyItemCheck : IRegisteredEvent
    {
        public bool Run(params object[] args)
        {
            int index = (int)args[0];
            int type = (int) args[1];
            NWPlayer player = _.GetPCSpeaker();
            NWObject talkingTo = Object.OBJECT_SELF;

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
