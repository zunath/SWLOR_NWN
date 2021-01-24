using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DialogService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature.DialogDefinition
{
    public class LockedDoorDialog: DialogBase
    {
        private const string MainPageId = "MAIN_PAGE";

        public override PlayerDialog SetUp(uint player)
        {
            var builder = new DialogBuilder()
                .AddPage(MainPageId, MainPageInit);


            return builder.Build();
        }

        private void MainPageInit(DialogPage page)
        {
            page.Header = "This door is locked. It looks like you need a key to open it.";

            var door = OBJECT_SELF;
            var player = GetPC();
            var keyItemIds = new List<KeyItemType>();

            var count = 1;
            var keyItemId = GetLocalInt(door, $"REQUIRED_KEY_ITEM_ID_{count}");
            while (keyItemId > 0)
            {
                keyItemIds.Add((KeyItemType)keyItemId);

                count++;
                keyItemId = GetLocalInt(door, $"REQUIRED_KEY_ITEM_ID_{count}");
            }

            var doorDialogue = GetLocalString(door, "DOOR_DIALOGUE");
            if (!string.IsNullOrWhiteSpace(doorDialogue))
            {
                page.Header = doorDialogue;
            }

            if (KeyItem.HasAllKeyItems(player, keyItemIds))
            {
                page.AddResponse("Use Key", () =>
                {
                    var waypointTag = GetLocalString(door, "LOCKED_DOOR_INSIDE_WP");
                    var waypoint = GetWaypointByTag(waypointTag);
                    var waypointLocation = GetLocation(waypoint);

                    AssignCommand(player, () => ActionJumpToLocation(waypointLocation));

                    EndConversation();
                });
            }
        }
    }
}
