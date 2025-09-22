using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.NWScript.Enum.Associate;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Core.Enums;
using SWLOR.Shared.Dialog.Contracts;
using SWLOR.Shared.Dialog.Model;
using SWLOR.Shared.Dialog.Service;

namespace SWLOR.Game.Server.Feature.DialogDefinition
{
    public class LockedDoorDialog: DialogBase
    {
        private const string MainPageId = "MAIN_PAGE";
        private readonly IKeyItemService _keyItemService;

        public LockedDoorDialog(IKeyItemService keyItemService, IDialogService dialogService) 
            : base(dialogService)
        {
            _keyItemService = keyItemService;
        }

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

            if (_keyItemService.HasAllKeyItems(player, keyItemIds))
            {
                page.AddResponse("Use Key", () =>
                {
                    if (Enmity.HasEnmity(player))
                    {
                        FloatingTextStringOnCreature("An enemy is targeting you. Defeat them before entering!", player, false);
                    }
                    else
                    {
                        var waypointTag = GetLocalString(door, "LOCKED_DOOR_INSIDE_WP");
                        var waypoint = GetWaypointByTag(waypointTag);
                        var waypointLocation = GetLocation(waypoint);

                        AssignCommand(player, () => JumpToLocation(waypointLocation));

                        var henchman = GetAssociate(AssociateType.Henchman, player);
                        if (GetIsObjectValid(henchman))
                        {
                            AssignCommand(henchman, () => JumpToLocation(waypointLocation));
                        }
                    }

                    EndConversation();
                });
            }
        }
    }
}
