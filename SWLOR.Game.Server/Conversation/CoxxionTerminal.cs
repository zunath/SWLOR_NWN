using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;

using SWLOR.Game.Server.ValueObject.Dialog;

namespace SWLOR.Game.Server.Conversation
{
    public class CoxxionTerminal: ConversationBase
    {
        public override PlayerDialog SetUp(NWPlayer player)
        {
            var dialog = new PlayerDialog("MainPage");

            var mainPage = new DialogPage();

            dialog.AddPage("MainPage", mainPage);
            return dialog;
        }

        public override void Initialize()
        {
            NWPlaceable device = NWScript.OBJECT_SELF;
            var area = device.Area;

            var terminalColorID = device.GetLocalInt("TERMINAL_COLOR");
            var doorStatus = area.GetLocalInt("DOOR_STATUS");
            var openColor = GetColorString(doorStatus);
            var terminalColor = GetColorString(terminalColorID);

            if (string.IsNullOrWhiteSpace(terminalColor))
            {
                device.SpeakString("ERROR: Couldn't ID color. Inform an admin that this dungeon is broken.");
                return;
            }

            if (string.IsNullOrWhiteSpace(openColor))
            {
                openColor = "no";
            }

            var header = "Currently, " + openColor + " doors are unlocked.\n\n";
            header += "This terminal can unlock " + terminalColor + " doors.";

            SetPageHeader("MainPage", header);

            if (doorStatus != terminalColorID)
            {
                AddResponseToPage("MainPage", "Open " + terminalColor + " doors");
            }
        }

        public override void DoAction(NWPlayer player, string pageName, int responseID)
        {
            NWPlaceable device = NWScript.OBJECT_SELF;
            var area = device.Area;
            var terminalColorID = device.GetLocalInt("TERMINAL_COLOR");
            var terminalColor = GetColorString(terminalColorID);

            area.SetLocalInt("DOOR_STATUS", terminalColorID);

            List<NWObject> doors = area.Data["DOORS"];

            foreach (var door in doors)
            {
                if (door.GetLocalInt("DOOR_COLOR") == terminalColorID)
                {
                    NWScript.SetLocked(door, false);
                    door.AssignCommand(() => NWScript.ActionOpenDoor(door));
                }
                else
                {
                    door.AssignCommand(() => NWScript.ActionCloseDoor(door));
                    NWScript.SetLocked(door, true);
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
