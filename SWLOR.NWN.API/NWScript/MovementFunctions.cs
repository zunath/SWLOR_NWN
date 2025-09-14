using SWLOR.NWN.API.Engine;

namespace SWLOR.NWN.API.NWScript
{
    public partial class NWScript
    {
        /// <summary>
        /// Causes the caller to face the specified direction.
        /// fDirection is expressed as anticlockwise degrees from Due East.
        /// DIRECTION_EAST, DIRECTION_NORTH, DIRECTION_WEST and DIRECTION_SOUTH are
        /// predefined. (0.0f=East, 90.0f=North, 180.0f=West, 270.0f=South)
        /// </summary>
        /// <param name="fDirection">The direction to face in anticlockwise degrees from Due East</param>
        public static void SetFacing(float fDirection)
        {
            global::NWN.Core.NWScript.SetFacing(fDirection);
        }

        /// <summary>
        /// Gets the direction in which the target is facing, expressed as a float between
        /// 0.0f and 360.0f.
        /// </summary>
        /// <param name="oTarget">The target to get the facing direction for</param>
        /// <returns>The facing direction, or -1.0f on error</returns>
        public static float GetFacing(uint oTarget)
        {
            return global::NWN.Core.NWScript.GetFacing(oTarget);
        }

        /// <summary>
        /// Causes the action subject to move away from the specified location.
        /// </summary>
        /// <param name="lMoveAwayFrom">The location to move away from</param>
        /// <param name="bRun">Whether to run (defaults to false)</param>
        /// <param name="fMoveAwayRange">The range to move away (defaults to 40.0f)</param>
        public static void ActionMoveAwayFromLocation(Location lMoveAwayFrom, bool bRun = false,
            float fMoveAwayRange = 40.0f)
        {
            global::NWN.Core.NWScript.ActionMoveAwayFromLocation(lMoveAwayFrom, bRun ? 1 : 0, fMoveAwayRange);
        }

        /// <summary>
        /// Jumps to the specified object (the action is added to the top of the action queue).
        /// </summary>
        /// <param name="oToJumpTo">The object to jump to</param>
        /// <param name="nWalkStraightLineToPoint">Whether to walk in a straight line to the point (defaults to true)</param>
        public static void JumpToObject(uint oToJumpTo, bool nWalkStraightLineToPoint = true)
        {
            global::NWN.Core.NWScript.JumpToObject(oToJumpTo, nWalkStraightLineToPoint ? 1 : 0);
        }
    }
}
