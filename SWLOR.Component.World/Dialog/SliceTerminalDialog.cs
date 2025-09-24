using SWLOR.Component.World.Contracts;
using SWLOR.NWN.API.NWNX.Enum;
using SWLOR.Shared.Dialog.Contracts;
using SWLOR.Shared.Dialog.Model;
using SWLOR.Shared.Dialog.Service;
using SWLOR.Shared.Domain.Common.Enums;

namespace SWLOR.Component.World.Dialog
{
    public class SliceTerminalDialog: DialogBase
    {
        private const string MainPageId = "MAIN_PAGE";
        private readonly IKeyItemService _keyItemService;
        private readonly IObjectVisibilityService _objectVisibilityService;

        public SliceTerminalDialog(IKeyItemService keyItemService, IObjectVisibilityService objectVisibilityService, IDialogService dialogService) 
            : base(dialogService)
        {
            _keyItemService = keyItemService;
            _objectVisibilityService = objectVisibilityService;
        }

        public override PlayerDialog SetUp(uint player)
        {
            var builder = new DialogBuilder()
                .AddPage(MainPageId, MainPageInit);

            return builder.Build();
        }

        private void MainPageInit(DialogPage page)
        {
            page.Header = "You can slice this terminal. What would you like to do?";

            page.AddResponse("Slice the terminal", () =>
            {
                var player = GetPC();
                var self = OBJECT_SELF;
                var keyItemId = GetLocalInt(self, "KEY_ITEM_ID");

                if (keyItemId <= 0)
                {
                    FloatingTextStringOnCreature("ERROR: Improperly configured key item. Id is not set. Notify an admin this quest is broken.", player, false);
                    return;
                }

                var keyItemType = (KeyItemType) keyItemId;
                _keyItemService.GiveKeyItem(player, keyItemType);
                _objectVisibilityService.AdjustVisibility(player, self, VisibilityType.Hidden);
                
                EndConversation();
            });
        }
    }
}
