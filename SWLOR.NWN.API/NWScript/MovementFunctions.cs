using SWLOR.NWN.API.Engine;

namespace SWLOR.NWN.API.NWScript
{
    public partial class NWScript
    {
        /// <summary>
        ///   Cause the caller to face fDirection.
        ///   - fDirection is expressed as anticlockwise degrees from Due East.
        ///   DIRECTION_EAST, DIRECTION_NORTH, DIRECTION_WEST and DIRECTION_SOUTH are
        ///   predefined. (0.0f=East, 90.0f=North, 180.0f=West, 270.0f=South)
        /// </summary>
        public static void SetFacing(float fDirection)
        {
            global::NWN.Core.NWScript.SetFacing(fDirection);
        }

        /// <summary>
        ///   Get the direction in which oTarget is facing, expressed as a float between
        ///   0.0f and 360.0f
        ///   * Return value on error: -1.0f
        /// </summary>
        public static float GetFacing(uint oTarget)
        {
            return global::NWN.Core.NWScript.GetFacing(oTarget);
        }

        /// <summary>
        ///   Causes the action subject to move away from lMoveAwayFrom.
        /// </summary>
        public static void ActionMoveAwayFromLocation(Location lMoveAwayFrom, bool bRun = false,
            float fMoveAwayRange = 40.0f)
        {
            global::NWN.Core.NWScript.ActionMoveAwayFromLocation(lMoveAwayFrom, bRun ? 1 : 0, fMoveAwayRange);
        }

        /// <summary>
        ///   Jump to oToJumpTo (the action is added to the top of the action queue).
        /// </summary>
        public static void JumpToObject(uint oToJumpTo, bool nWalkStraightLineToPoint = true)
        {
            global::NWN.Core.NWScript.JumpToObject(oToJumpTo, nWalkStraightLineToPoint ? 1 : 0);
        }
    }
}
