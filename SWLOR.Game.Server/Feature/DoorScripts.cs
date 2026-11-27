using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature
{
    public static class DoorScripts
    {
        private const string OpenVersionVariable = "AUTO_RELOCK_OPEN_VERSION";

        [NWNEventHandler(ScriptName.OnDoorAutoRelockOpen)]
        public static void ScheduleCloseAndRelock()
        {
            CloseAndRelockLater(OBJECT_SELF);
        }

        /// <summary>Closes and relocks <paramref name="door"/> five seconds from now, unless it is opened again first.</summary>
        public static void CloseAndRelockLater(uint door)
        {
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

        /// <summary>
        /// When a hot reload has activated a new build: an auto-relocking door still open (its close-and-relock timer was a
        /// closure the reload dropped) gets the timer again, so it closes and locks as it would have. A door that has auto-relocked
        /// before carries its open count, which no other door has.
        /// </summary>
        [ReloadHook(ReloadHookPhase.Resume)]
        public static void ResumeAutoRelockForReload(IReadOnlyList<uint> players)
        {
            var doors = new List<uint>();
            for (var area = GetFirstArea(); GetIsObjectValid(area); area = GetNextArea())
            {
                for (var door = GetFirstObjectInArea(area, ObjectType.Door); GetIsObjectValid(door); door = GetNextObjectInArea(area, ObjectType.Door))
                {
                    if (GetLocalInt(door, OpenVersionVariable) > 0 && GetIsOpen(door))
                        doors.Add(door);
                }
            }

            foreach (var door in doors)
                CloseAndRelockLater(door);
        }

        [NWNEventHandler(ScriptName.OnDoorAutoRelockClosed)]
        public static void RelockClosedDoor()
        {
            SetLocked(OBJECT_SELF, true);
        }
    }
}
