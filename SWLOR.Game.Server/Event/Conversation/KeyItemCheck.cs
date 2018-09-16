using System;
using System.Collections.Generic;
using System.Linq;
using NWN;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;
using Object = NWN.Object;

namespace SWLOR.Game.Server.Event.Conversation
{
    public class KeyItemCheck : IRegisteredEvent
    {
        private readonly INWScript _;
        private readonly IKeyItemService _keyItem;

        public KeyItemCheck(
            INWScript script,
            IKeyItemService keyItem)
        {
            _ = script;
            _keyItem = keyItem;
        }

        public bool Run(params object[] args)
        {
            int index = (int)args[0];
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

            return _keyItem.PlayerHasAllKeyItems(player, requiredKeyItemIDs.ToArray());
        }
    }
}
