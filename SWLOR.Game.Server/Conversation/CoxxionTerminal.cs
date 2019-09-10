using System.Collections.Generic;
using System.Linq;
using NWN;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;

using SWLOR.Game.Server.ValueObject.Dialog;
using static NWN._;

namespace SWLOR.Game.Server.Conversation
{
    public class CoxxionTerminal: ConversationBase
    {
        public override PlayerDialog SetUp(NWPlayer player)
        {
            PlayerDialog dialog = new PlayerDialog("MainPage");

            DialogPage mainPage = new DialogPage();

            dialog.AddPage("MainPage", mainPage);
            return dialog;
        }

        public override void Initialize()
        {
            NWPlaceable device = NWGameObject.OBJECT_SELF;
            NWArea area = device.Area;

            int terminalColorID = device.GetLocalInt("TERMINAL_COLOR");
            int doorStatus = area.GetLocalInt("DOOR_STATUS");
            string openColor = GetColorString(doorStatus);
            string terminalColor = GetColorString(terminalColorID);

            if (string.IsNullOrWhiteSpace(terminalColor))
            {
                device.SpeakString("ERROR: Couldn't ID color. Inform an admin that this dungeon is broken.");
                return;
            }

            if (string.IsNullOrWhiteSpace(openColor))
            {
                openColor = "no";
            }

            string header = "Currently, " + openColor + " doors are unlocked.\n\n";
            header += "This terminal can unlock " + terminalColor + " doors.";

            SetPageHeader("MainPage", header);

            if (doorStatus != terminalColorID)
            {
                AddResponseToPage("MainPage", "Open " + terminalColor + " doors");
            }
        }

        public override void DoAction(NWPlayer player, string pageName, int responseID)
        {
            NWPlaceable device = NWGameObject.OBJECT_SELF;
            NWArea area = device.Area;
            int terminalColorID = device.GetLocalInt("TERMINAL_COLOR");
            string terminalColor = GetColorString(terminalColorID);

            area.SetLocalInt("DOOR_STATUS", terminalColorID);

            List<NWObject> doors = area.Data["DOORS"];

            foreach (var door in doors)
            {
                if (door.GetLocalInt("DOOR_COLOR") == terminalColorID)
                {
                    _.SetLocked(door, FALSE);
                    door.AssignCommand(() => _.ActionOpenDoor(door));
                }
                else
                {
                    door.AssignCommand(() => _.ActionCloseDoor(door));
                    _.SetLocked(door, TRUE);
                }
            }

            var areaPlayers = NWModule.Get().Players.Where(x => Equals(x.Area, area));
            foreach (var areaPlayer in areaPlayers)
            {
                areaPlayer.FloatingText(terminalColor + " doors are now unlocked.");
            }

            EndConversation();
        }

        private string GetColorString(int colorID)
        {
            string colorText;
            switch (colorID)
            {
                case 1: // Blue
                    colorText = ColorTokenService.Blue("BLUE");
                    break;
                case 2: // Green
                    colorText = ColorTokenService.Green("GREEN");
                    break;
                case 3: // Red
                    colorText = ColorTokenService.Red("RED");
                    break;
                default: return string.Empty;
            }

            return colorText;
        }

        public override void Back(NWPlayer player, string beforeMovePage, string afterMovePage)
        {
        }

        public override void EndDialog()
        {
        }
    }
}
