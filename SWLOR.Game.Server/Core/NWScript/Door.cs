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
            Internal.NativeFunctions.StackPushObject(oObject);
            Internal.NativeFunctions.CallBuiltIn(443);
            return Internal.NativeFunctions.StackPopInteger() != 0;
        }

        /// <summary>
        ///   The action subject will unlock oTarget, which can be a door or a placeable
        ///   object.
        /// </summary>
        public static void ActionUnlockObject(uint oTarget)
        {
            Internal.NativeFunctions.StackPushObject(oTarget);
            Internal.NativeFunctions.CallBuiltIn(483);
        }

        /// <summary>
        ///   The action subject will lock oTarget, which can be a door or a placeable
        ///   object.
        /// </summary>
        public static void ActionLockObject(uint oTarget)
        {
            Internal.NativeFunctions.StackPushObject(oTarget);
            Internal.NativeFunctions.CallBuiltIn(484);
        }


        /// <summary>
        ///   Cause the action subject to open oDoor
        /// </summary>
        public static void ActionOpenDoor(uint oDoor)
        {
            Internal.NativeFunctions.StackPushObject(oDoor);
            Internal.NativeFunctions.CallBuiltIn(43);
        }

        /// <summary>
        ///   Cause the action subject to close oDoor
        /// </summary>
        public static void ActionCloseDoor(uint oDoor)
        {
            Internal.NativeFunctions.StackPushObject(oDoor);
            Internal.NativeFunctions.CallBuiltIn(44);
        }

        /// <summary>
        ///   Get the last blocking door encountered by the caller of this function.
        ///   * Returns OBJECT_INVALID if the caller is not a valid creature.
        /// </summary>
        public static uint GetBlockingDoor()
        {
            Internal.NativeFunctions.CallBuiltIn(336);
            return Internal.NativeFunctions.StackPopObject();
        }

        /// <summary>
        ///   - oTargetDoor
        ///   - nDoorAction: DOOR_ACTION_*
        ///   * Returns TRUE if nDoorAction can be performed on oTargetDoor.
        /// </summary>
        public static bool GetIsDoorActionPossible(uint oTargetDoor, DoorAction nDoorAction)
        {
            Internal.NativeFunctions.StackPushInteger((int)nDoorAction);
            Internal.NativeFunctions.StackPushObject(oTargetDoor);
            Internal.NativeFunctions.CallBuiltIn(337);
            return Internal.NativeFunctions.StackPopInteger() == 1;
        }

        /// <summary>
        ///   Perform nDoorAction on oTargetDoor.
        /// </summary>
        public static void DoDoorAction(uint oTargetDoor, DoorAction nDoorAction)
        {
            Internal.NativeFunctions.StackPushInteger((int)nDoorAction);
            Internal.NativeFunctions.StackPushObject(oTargetDoor);
            Internal.NativeFunctions.CallBuiltIn(338);
        }
    }
}