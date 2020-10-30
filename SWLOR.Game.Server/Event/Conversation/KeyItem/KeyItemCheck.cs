using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript;
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
                NWPlayer player = NWScript.GetPCSpeaker();
                NWObject talkingTo = NWScript.OBJECT_SELF;

                var count = 1;
                var requiredKeyItemIDs = new List<int>();

                var keyItemID = talkingTo.GetLocalInt($"KEY_ITEM_{index}_REQ_{count}");

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
