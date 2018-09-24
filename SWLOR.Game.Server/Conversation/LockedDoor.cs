using System.Linq;
using NWN;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Entities;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject.Dialog;

namespace SWLOR.Game.Server.Conversation
{
    public class LockedDoor: ConversationBase
    {
        private readonly IDataContext _db;

        public LockedDoor(
            INWScript script, 
            IDialogService dialog,
            IDataContext db) 
            : base(script, dialog)
        {
            _db = db;
        }

        public override PlayerDialog SetUp(NWPlayer player)
        {
            PlayerDialog dialog = new PlayerDialog("MainPage");

            DialogPage mainPage = new DialogPage(
                "This door is locked. It looks like it needs a key to be opened.",
                "Use Key");

            dialog.AddPage("MainPage", mainPage);
            return dialog;
        }

        public override void Initialize()
        {
            NWObject door = Object.OBJECT_SELF;
            NWPlayer player = GetPC();
            int keyItemID = door.GetLocalInt("LOCKED_DOOR_REQUIRED_KEY_ITEM_ID");
            PCKeyItem keyItem = _db.PCKeyItems.SingleOrDefault(x => x.PlayerID == player.GlobalID && x.KeyItemID == keyItemID);
            bool hasKeyItem = keyItem != null;

            if (hasKeyItem)
            {
                SetResponseText("MainPage", 1, "Open With: " + keyItem.KeyItem.Name);
            }
            else
            {
                SetResponseVisible("MainPage", 1, false);
            }
        }

        public override void DoAction(NWPlayer player, string pageName, int responseID)
        {
            if (responseID != 1) return;

            NWObject door = Object.OBJECT_SELF;
            string insideWP = door.GetLocalString("LOCKED_DOOR_INSIDE_WP");
            NWObject wp = _.GetWaypointByTag(insideWP);
            Location portTo = wp.Location;

            _.AssignCommand(player, () =>
            {
                _.ActionJumpToLocation(portTo);
            });

            EndConversation();
        }

        public override void EndDialog()
        {
        }
    }
}
