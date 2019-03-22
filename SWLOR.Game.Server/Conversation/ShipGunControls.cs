using NWN;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject.Dialog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SWLOR.Game.Server.Conversation
{
    class ShipGunControls : ConversationBase
    {
        private ISpaceService _space;

        public ShipGunControls(
            
            IDialogService dialog,
            ISpaceService space) : base(dialog)
        {
            _space = space;
        }

        public override PlayerDialog SetUp(NWPlayer player)
        {
            string header;
            DialogPage mainOptions;

            if (!_space.IsLocationSpace(_space.GetShipLocation(player.Area)))
            {
                header = "You can only crew the guns while the ship is in space.";
                mainOptions = new DialogPage(header);
            }
            else
            {
                _space.CreateShipInSpace(player.Area);

                header = "Crewing the ship's guns allows the ship to fire in any direction using the gunner's Piloting skill. " +
                    "If the target is in front of the ship, both the pilot's and the gunner's skills are added to the shot. " +
                    "Unlike the pilot, the gunner can select targets using the quickbar option.  To stop crewing the guns, " +
                    "type /exit or use the quickbar button.";
                mainOptions = new DialogPage(header, "Crew the guns!");
            }

            PlayerDialog dialog = new PlayerDialog("MainPage");

            dialog.AddPage("MainPage", mainOptions);

            return dialog;
        }

        public override void Initialize()
        {
            // Nothing to do here - everything was set up in SetUp.
            // Check whether we are in space.
        }

        public override void DoAction(NWPlayer player, string pageName, int responseID)
        {
            if (responseID == 1)
            {
                _space.DoCrewGuns(player, player.Area);
            }

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

