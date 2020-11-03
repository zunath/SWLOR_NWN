using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Legacy.Event.Module;
using SWLOR.Game.Server.Legacy.GameObject;
using SWLOR.Game.Server.Legacy.Messaging;
using SWLOR.Game.Server.Legacy.ValueObject.Dialog;
using SWLOR.Game.Server.Service;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Legacy.Conversation
{
    public class CoxxionTerminal: ConversationBase
    {
        private static readonly Dictionary<uint, List<uint>> _areaDoors = new Dictionary<uint, List<uint>>();

        public static void SubscribeEvents()
        {
            MessageHub.Instance.Subscribe<OnModuleLoad>(m => LoadDoors());
        }

        public static void LoadDoors()
        {
            // This area used to be an instance so this code made sense then.
            // Now, however, it would make more sense to refactor and store them as local objects.
            var area = NWModule.Get().Areas.SingleOrDefault(x => GetResRef(x) == "v_cox_base");
            if (!GetIsObjectValid(area)) return;

            var doors = new List<uint>();

            var obj = GetFirstObjectInArea(area);
            while (GetIsObjectValid(obj))
            {
                var colorID = GetLocalInt(obj, "DOOR_COLOR");

                if (colorID > 0)
                {
                    doors.Add(obj);
                }

                obj = GetNextObjectInArea(area);
            }

            _areaDoors[area] = doors;
        }


        public override PlayerDialog SetUp(NWPlayer player)
        {
            var dialog = new PlayerDialog("MainPage");

            var mainPage = new DialogPage();

            dialog.AddPage("MainPage", mainPage);
            return dialog;
        }

        public override void Initialize()
        {
            NWPlaceable device = OBJECT_SELF;
            var area = device.Area;

            var terminalColorID = device.GetLocalInt("TERMINAL_COLOR");
            var doorStatus = GetLocalInt(area, "DOOR_STATUS");
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
            NWPlaceable device = OBJECT_SELF;
            var area = device.Area;
            var terminalColorID = device.GetLocalInt("TERMINAL_COLOR");
            var terminalColor = GetColorString(terminalColorID);

            SetLocalInt(area, "DOOR_STATUS", terminalColorID);

            var doors = _areaDoors[area];

            foreach (var door in doors)
            {
                if (GetLocalInt(door, "DOOR_COLOR") == terminalColorID)
                {
                    SetLocked(door, false);
                    AssignCommand(door, () => ActionOpenDoor(door));
                }
                else
                {
                    AssignCommand(door, () => ActionCloseDoor(door));
                    SetLocked(door, true);
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
                    colorText = ColorToken.Blue("BLUE");
                    break;
                case 2: // Green
                    colorText = ColorToken.Green("GREEN");
                    break;
                case 3: // Red
                    colorText = ColorToken.Red("RED");
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
