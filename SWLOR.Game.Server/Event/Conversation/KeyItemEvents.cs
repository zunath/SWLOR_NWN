using System;
using System.Collections.Generic;
using System.Text;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Legacy;
using SWLOR.Game.Server.ValueObject;

namespace SWLOR.Game.Server.Event.Conversation.KeyItem
{
    public static class KeyItemEvents
    {
        [NWNEventHandler("any_keyitems_1")]
        public static int AnyKeyItems1()
        {
            return Check(1, 2) ? 1 : 0;
        }

        [NWNEventHandler("any_keyitems_2")]
        public static int AnyKeyItems2()
        {
            return Check(2, 2) ? 1 : 0;
        }
        [NWNEventHandler("any_keyitems_3")]
        public static int AnyKeyItems3()
        {
            return Check(3, 2) ? 1 : 0;
        }
        [NWNEventHandler("any_keyitems_4")]
        public static int AnyKeyItems4()
        {
            return Check(4, 2) ? 1 : 0;
        }
        [NWNEventHandler("any_keyitems_5")]
        public static int AnyKeyItems5()
        {
            return Check(5, 2) ? 1 : 0;
        }
        [NWNEventHandler("any_keyitems_6")]
        public static int AnyKeyItems6()
        {
            return Check(6, 2) ? 1 : 0;
        }
        [NWNEventHandler("any_keyitems_7")]
        public static int AnyKeyItems7()
        {
            return Check(7, 2) ? 1 : 0;
        }
        [NWNEventHandler("any_keyitems_8")]
        public static int AnyKeyItems8()
        {
            return Check(8, 2) ? 1 : 0;
        }
        [NWNEventHandler("any_keyitems_9")]
        public static int AnyKeyItems9()
        {
            return Check(9, 2) ? 1 : 0;
        }
        [NWNEventHandler("any_keyitems_10")]
        public static int AnyKeyItems10()
        {
            return Check(10, 2) ? 1 : 0;
        }


        [NWNEventHandler("has_keyitems_1")]
        public static int HasKeyItems1()
        {
            return Check(1, 1) ? 1 : 0;
        }

        [NWNEventHandler("has_keyitems_2")]
        public static int HasKeyItems2()
        {
            return Check(2, 1) ? 1 : 0;
        }
        [NWNEventHandler("has_keyitems_3")]
        public static int HasKeyItems3()
        {
            return Check(3, 1) ? 1 : 0;
        }
        [NWNEventHandler("has_keyitems_4")]
        public static int HasKeyItems4()
        {
            return Check(4, 1) ? 1 : 0;
        }
        [NWNEventHandler("has_keyitems_5")]
        public static int HasKeyItems5()
        {
            return Check(5, 1) ? 1 : 0;
        }
        [NWNEventHandler("has_keyitems_6")]
        public static int HasKeyItems6()
        {
            return Check(6, 1) ? 1 : 0;
        }
        [NWNEventHandler("has_keyitems_7")]
        public static int HasKeyItems7()
        {
            return Check(7, 1) ? 1 : 0;
        }
        [NWNEventHandler("has_keyitems_8")]
        public static int HasKeyItems8()
        {
            return Check(8, 1) ? 1 : 0;
        }
        [NWNEventHandler("has_keyitems_9")]
        public static int HasKeyItems9()
        {
            return Check(9, 1) ? 1 : 0;
        }
        [NWNEventHandler("has_keyitems_10")]
        public static int HasKeyItems10()
        {
            return Check(10, 1) ? 1 : 0;
        }


        private static bool Check(int index, int type)
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
