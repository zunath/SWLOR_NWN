using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.NWN.API.NWScript
{
    public partial class NWScript
    {
        /// <summary>
        /// Returns true if the object (which is a placeable or a door) is currently open.
        /// </summary>
        /// <param name="oObject">The object to check</param>
        /// <returns>True if the object is open, false otherwise</returns>
        public static bool GetIsOpen(uint oObject)
        {
            return global::NWN.Core.NWScript.GetIsOpen(oObject) != 0;
        }

        /// <summary>
        /// Makes the action subject unlock the target object.
        /// </summary>
        /// <param name="oTarget">The target object to unlock (can be a door or a placeable object)</param>
        public static void ActionUnlockObject(uint oTarget)
        {
            global::NWN.Core.NWScript.ActionUnlockObject(oTarget);
        }

        /// <summary>
        /// Makes the action subject lock the target object.
        /// </summary>
        /// <param name="oTarget">The target object to lock (can be a door or a placeable object)</param>
        public static void ActionLockObject(uint oTarget)
        {
            global::NWN.Core.NWScript.ActionLockObject(oTarget);
        }


        /// <summary>
        /// Makes the action subject open the specified door.
        /// </summary>
        /// <param name="oDoor">The door to open</param>
        public static void ActionOpenDoor(uint oDoor)
        {
            global::NWN.Core.NWScript.ActionOpenDoor(oDoor);
        }

        /// <summary>
        /// Makes the action subject close the specified door.
        /// </summary>
        /// <param name="oDoor">The door to close</param>
        public static void ActionCloseDoor(uint oDoor)
        {
            global::NWN.Core.NWScript.ActionCloseDoor(oDoor);
        }

        /// <summary>
        /// Gets the last blocking door encountered by the caller of this function.
        /// </summary>
        /// <returns>The last blocking door. Returns OBJECT_INVALID if the caller is not a valid creature</returns>
        public static uint GetBlockingDoor()
        {
            return global::NWN.Core.NWScript.GetBlockingDoor();
        }

        /// <summary>
        /// Returns true if the specified door action can be performed on the target door.
        /// </summary>
        /// <param name="oTargetDoor">The target door</param>
        /// <param name="nDoorAction">The door action to check (DOOR_ACTION_* constants)</param>
        /// <returns>True if the door action can be performed, false otherwise</returns>
        public static bool GetIsDoorActionPossible(uint oTargetDoor, DoorAction nDoorAction)
        {
            return global::NWN.Core.NWScript.GetIsDoorActionPossible(oTargetDoor, (int)nDoorAction) == 1;
        }

        /// <summary>
        /// Performs the specified door action on the target door.
        /// </summary>
        /// <param name="oTargetDoor">The target door</param>
        /// <param name="nDoorAction">The door action to perform (DOOR_ACTION_* constants)</param>
        public static void DoDoorAction(uint oTargetDoor, DoorAction nDoorAction)
        {
            global::NWN.Core.NWScript.DoDoorAction(oTargetDoor, (int)nDoorAction);
        }
    }
}