using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Legacy.GameObject;
using SWLOR.Game.Server.Legacy.Service;
using SWLOR.Game.Server.Legacy.ValueObject.Dialog;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Legacy.Conversation
{
    public class LockedDoor: ConversationBase
    {
        public override PlayerDialog SetUp(NWPlayer player)
        {
            var dialog = new PlayerDialog("MainPage");

            var mainPage = new DialogPage(
                "This door is locked. It looks like it needs a key to be opened.",
                "Use Key");

            dialog.AddPage("MainPage", mainPage);
            return dialog;
        }

        public override void Initialize()
        {
            NWObject door = NWScript.OBJECT_SELF;
            var player = GetPC();
            var keyItemIDs = new List<int>();

            var count = 1;
            var keyItemID = door.GetLocalInt("REQUIRED_KEY_ITEM_ID_" + count);
            while (keyItemID > 0)
            {
                keyItemIDs.Add(keyItemID);

                count++;
                keyItemID = door.GetLocalInt("REQUIRED_KEY_ITEM_ID_" + count);
            }

            var hasKeyItems = true;

            foreach (var keyItemId in keyItemIDs)
            {
                var keyItemType = (KeyItemType) keyItemId;
                if (!KeyItem.HasKeyItem(player, keyItemType))
                {
                    hasKeyItems = false;
                    break;
                }
            }

            var doorDialogue = door.GetLocalString("DOOR_DIALOGUE");

            if (!string.IsNullOrWhiteSpace(doorDialogue))
            {
                SetPageHeader("MainPage", doorDialogue);
            }

            if (hasKeyItems)
            {
                SetResponseText("MainPage", 1, "Open Door");
            }
            else
            {
                SetResponseVisible("MainPage", 1, false);
            }
        }

        public override void DoAction(NWPlayer player, string pageName, int responseID)
        {
            if (responseID != 1) return;

            NWObject door = NWScript.OBJECT_SELF;
            var insideWP = door.GetLocalString("LOCKED_DOOR_INSIDE_WP");
            NWObject wp = NWScript.GetWaypointByTag(insideWP);
            var portTo = wp.Location;

            NWScript.AssignCommand(player, () =>
            {
                NWScript.ActionJumpToLocation(portTo);
            });

            EndConversation();
        }

        public override void Back(NWPlayer player, string beforeMovePage, string afterMovePage)
        {
        }

        public override void EndDialog()
        {
        }
    }
}
