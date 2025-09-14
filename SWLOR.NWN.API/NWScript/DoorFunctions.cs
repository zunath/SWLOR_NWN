using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.NWN.API.NWScript
{
    public partial class NWScript
    {
        /// <summary>
        ///   * Returns TRUE if oObject (which is a placeable or a door) is currently open.
        /// </summary>
        public static bool GetIsOpen(uint oObject)
        {
            return global::NWN.Core.NWScript.GetIsOpen(oObject) != 0;
        }

        /// <summary>
        ///   The action subject will unlock oTarget, which can be a door or a placeable
        ///   object.
        /// </summary>
        public static void ActionUnlockObject(uint oTarget)
        {
            global::NWN.Core.NWScript.ActionUnlockObject(oTarget);
        }

        /// <summary>
        ///   The action subject will lock oTarget, which can be a door or a placeable
        ///   object.
        /// </summary>
        public static void ActionLockObject(uint oTarget)
        {
            global::NWN.Core.NWScript.ActionLockObject(oTarget);
        }


        /// <summary>
        ///   Cause the action subject to open oDoor
        /// </summary>
        public static void ActionOpenDoor(uint oDoor)
        {
            global::NWN.Core.NWScript.ActionOpenDoor(oDoor);
        }

        /// <summary>
        ///   Cause the action subject to close oDoor
        /// </summary>
        public static void ActionCloseDoor(uint oDoor)
        {
            global::NWN.Core.NWScript.ActionCloseDoor(oDoor);
        }

        /// <summary>
        ///   Get the last blocking door encountered by the caller of this function.
        ///   * Returns OBJECT_INVALID if the caller is not a valid creature.
        /// </summary>
        public static uint GetBlockingDoor()
        {
            return global::NWN.Core.NWScript.GetBlockingDoor();
        }

        /// <summary>
        ///   - oTargetDoor
        ///   - nDoorAction: DOOR_ACTION_*
        ///   * Returns TRUE if nDoorAction can be performed on oTargetDoor.
        /// </summary>
        public static bool GetIsDoorActionPossible(uint oTargetDoor, DoorAction nDoorAction)
        {
            return global::NWN.Core.NWScript.GetIsDoorActionPossible(oTargetDoor, (int)nDoorAction) == 1;
        }

        /// <summary>
        ///   Perform nDoorAction on oTargetDoor.
        /// </summary>
        public static void DoDoorAction(uint oTargetDoor, DoorAction nDoorAction)
        {
            global::NWN.Core.NWScript.DoDoorAction(oTargetDoor, (int)nDoorAction);
        }
    }
}