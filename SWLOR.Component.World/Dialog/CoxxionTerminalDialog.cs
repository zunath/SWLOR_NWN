using SWLOR.Component.World.Service;
using SWLOR.Shared.Dialog.Contracts;
using SWLOR.Shared.Dialog.Model;
using SWLOR.Shared.Dialog.Service;
using SWLOR.Shared.Domain.Common.Contracts;
using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Events.Module;
using SWLOR.Shared.UI.Service;

namespace SWLOR.Component.World.Dialog
{
    public class CoxxionTerminalDialog: DialogBase
    {
        private readonly List<uint> _areaDoors = new();

        private const string MainPageId = "MAIN_PAGE";
        private readonly IAreaService _areaService;

        public CoxxionTerminalDialog(
            IDialogService dialogService,
            IAreaService areaService) 
            : base(dialogService)
        {
            _areaService = areaService;
        }

        /// <summary>
        /// When the module loads, store the doors for the Coxxion Base dungeon into cache.
        /// </summary>
        [ScriptHandler<OnModuleLoad>]
        public void LoadDoors()
        {
            var area = _areaService.GetAreaByResref("v_cox_base");
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

                foreach (var areaPlayer in _areaService.GetPlayersInArea(area))
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
