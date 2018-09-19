using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NWN;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject.Dialog;

namespace SWLOR.Game.Server.Conversation
{
    public class EntryConversation: ConversationBase
    {
        public EntryConversation(INWScript script, IDialogService dialog) 
            : base(script, dialog)
        {
        }

        public override PlayerDialog SetUp(NWPlayer player)
        {
            PlayerDialog dialog = new PlayerDialog("MainPage");
            DialogPage mainPage = new DialogPage("Are you ready to enter the game world? This is the LAST chance for you to use the '/customize' chat command to change your appearance.\n\nAre you sure you want to proceed?",
                "Enter the Game");

            dialog.AddPage("MainPage", mainPage);
            return dialog;
        }

        public override void Initialize()
        {
        }

        public override void DoAction(NWPlayer player, string pageName, int responseID)
        {
            switch (responseID)
            {
                case 1:
                    NWObject waypoint = _.GetObjectByTag("ENTRY_STARTING_WP");
                    player.AssignCommand(() =>
                    {
                        _.ActionJumpToLocation(waypoint.Location);
                    });
                    break;
            }
        }

        public override void EndDialog()
        {
        }
    }
}
