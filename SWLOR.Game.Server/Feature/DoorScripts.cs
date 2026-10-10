using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Feature
{
    public static class DoorScripts
    {
        private const string OpenVersionVariable = "AUTO_RELOCK_OPEN_VERSION";

        [NWNEventHandler(ScriptName.OnDoorAutoRelockOpen)]
        public static void ScheduleCloseAndRelock()
        {
            var door = OBJECT_SELF;
            var openVersion = GetLocalInt(door, OpenVersionVariable) + 1;
            SetLocalInt(door, OpenVersionVariable, openVersion);

            DelayCommand(5.0f, () =>
            {
                if (!GetIsObjectValid(door) || GetLocalInt(door, OpenVersionVariable) != openVersion)
                    return;

                if (GetIsOpen(door))
                {
                    // The close event locks the door after the close action completes.
                    AssignCommand(door, () => ActionCloseDoor(door));
                }
                else
                {
                    SetLocked(door, true);
                }
            });
        }

        [NWNEventHandler(ScriptName.OnDoorAutoRelockClosed)]
        public static void RelockClosedDoor()
        {
            SetLocked(OBJECT_SELF, true);
        }
    }
}
