using SWLOR.Game.Server.Core.NWScript.Enum;

namespace SWLOR.Game.Server.Core.NWScript
{
    public partial class NWScript
    {
        /// <summary>
        ///   * Returns TRUE if oObject (which is a placeable or a door) is currently open.
        /// </summary>
        public static bool GetIsOpen(uint oObject)
        {
            VM.StackPush(oObject);
            VM.Call(443);
            return VM.StackPopInt() != 0;
        }

        /// <summary>
        ///   The action subject will unlock oTarget, which can be a door or a placeable
        ///   object.
        /// </summary>
        public static void ActionUnlockObject(uint oTarget)
        {
            VM.StackPush(oTarget);
            VM.Call(483);
        }

        /// <summary>
        ///   The action subject will lock oTarget, which can be a door or a placeable
        ///   object.
        /// </summary>
        public static void ActionLockObject(uint oTarget)
        {
            VM.StackPush(oTarget);
            VM.Call(484);
        }


        /// <summary>
        ///   Cause the action subject to open oDoor
        /// </summary>
        public static void ActionOpenDoor(uint oDoor)
        {
            VM.StackPush(oDoor);
            VM.Call(43);
        }

        /// <summary>
        ///   Cause the action subject to close oDoor
        /// </summary>
        public static void ActionCloseDoor(uint oDoor)
        {
            VM.StackPush(oDoor);
            VM.Call(44);
        }

        /// <summary>
        ///   Get the last blocking door encountered by the caller of this function.
        ///   * Returns OBJECT_INVALID if the caller is not a valid creature.
        /// </summary>
        public static uint GetBlockingDoor()
        {
            VM.Call(336);
            return VM.StackPopObject();
        }

        /// <summary>
        ///   - oTargetDoor
        ///   - nDoorAction: DOOR_ACTION_*
        ///   * Returns TRUE if nDoorAction can be performed on oTargetDoor.
        /// </summary>
        public static bool GetIsDoorActionPossible(uint oTargetDoor, DoorAction nDoorAction)
        {
            VM.StackPush((int)nDoorAction);
            VM.StackPush(oTargetDoor);
            VM.Call(337);
            return VM.StackPopInt() == 1;
        }

        /// <summary>
        ///   Perform nDoorAction on oTargetDoor.
        /// </summary>
        public static void DoDoorAction(uint oTargetDoor, DoorAction nDoorAction)
        {
            VM.StackPush((int)nDoorAction);
            VM.StackPush(oTargetDoor);
            VM.Call(338);
        }
    }
}