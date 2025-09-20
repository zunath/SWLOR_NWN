using System.Collections.Generic;

using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DialogService;
using SWLOR.Shared.Core.Event;
using SWLOR.Shared.Core.Service;

namespace SWLOR.Game.Server.Feature.DialogDefinition
{
    public class CoxxionTerminalDialog: DialogBase
    {
        private static readonly List<uint> _areaDoors = new List<uint>();

        private const string MainPageId = "MAIN_PAGE";

        /// <summary>
        /// When the module loads, store the doors for the Coxxion Base dungeon into cache.
        /// </summary>
        [ScriptHandler(ScriptName.OnModuleLoad)]
        public static void LoadDoors()
        {
            var area = Area.GetAreaByResref("v_cox_base");
            if (!GetIsObjectValid(area)) return;

            for (var obj = GetFirstObjectInArea(area); GetIsObjectValid(obj); obj = GetNextObjectInArea(area))
            {
                var colorId = GetLocalInt(obj, "DOOR_COLOR");

                if (colorId > 0)
                {
                    _areaDoors.Add(obj);
                }
            }
        }

        public override PlayerDialog SetUp(uint player)
        {
            var builder = new DialogBuilder()
                .AddPage(MainPageId, MainPageInit);


            return builder.Build();
        }

        private void MainPageInit(DialogPage page)
        {
            var player = GetPC();
            var device = OBJECT_SELF;
            var area = GetArea(device);
            var terminalColorId = GetLocalInt(device, "TERMINAL_COLOR");
            var doorStatus = GetLocalInt(area, "DOOR_STATUS");
            var openColor = GetColorString(doorStatus);
            var terminalColor = GetColorString(terminalColorId);

            if (string.IsNullOrWhiteSpace(terminalColor))
            {
                FloatingTextStringOnCreature("ERROR: Couldn't ID color. Inform an admin that this dungeon is broken.", player, false);
                return;
            }

            if (string.IsNullOrWhiteSpace(openColor))
            {
                openColor = "no";
            }

            page.Header = $"Currently, {openColor} doors are unlocked.\n\n" +
                         $"This terminal can unlock {terminalColor} doors";

            page.AddResponse($"Open {terminalColor} doors", () =>
            {
                SetLocalInt(area, "DOOR_STATUS", terminalColorId);

                foreach (var door in _areaDoors)
                {
                    if (GetLocalInt(door, "DOOR_COLOR") == terminalColorId)
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

                foreach (var areaPlayer in Area.GetPlayersInArea(area))
                {
                    FloatingTextStringOnCreature($"{terminalColor} doors are now unlocked.", areaPlayer, false);
                }

                EndConversation();
            });
        }


        private string GetColorString(int colorId)
        {
            string colorText;
            switch (colorId)
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
    }
}
