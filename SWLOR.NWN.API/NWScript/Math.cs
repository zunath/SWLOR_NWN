using System.Numerics;

namespace SWLOR.Game.Server.Core.NWScript
{
    public partial class NWScript
    {
        /// <summary>
        ///   math operations
        ///   Maths operation: absolute value of fValue
        /// </summary>
        public static float fabs(float fValue)
        {
            return NWN.Core.NWScript.fabs(fValue);
        }

        /// <summary>
        ///   Maths operation: cosine of fValue
        /// </summary>
        public static float cos(float fValue)
        {
            return NWN.Core.NWScript.cos(fValue);
        }

        /// <summary>
        ///   Maths operation: sine of fValue
        /// </summary>
        public static float sin(float fValue)
        {
            return NWN.Core.NWScript.sin(fValue);
        }

        /// <summary>
        ///   Maths operation: tan of fValue
        /// </summary>
        public static float tan(float fValue)
        {
            return NWN.Core.NWScript.tan(fValue);
        }

        /// <summary>
        ///   Maths operation: arccosine of fValue
        ///   * Returns zero if fValue > 1 or fValue < -1
        /// </summary>
        public static float acos(float fValue)
        {
            return NWN.Core.NWScript.acos(fValue);
        }

        /// <summary>
        ///   Maths operation: arcsine of fValue
        ///   * Returns zero if fValue >1 or fValue < -1
        /// </summary>
        public static float asin(float fValue)
        {
            return NWN.Core.NWScript.asin(fValue);
        }

        /// <summary>
        ///   Maths operation: arctan of fValue
        /// </summary>
        public static float atan(float fValue)
        {
            return NWN.Core.NWScript.atan(fValue);
        }

        /// <summary>
        ///   Maths operation: log of fValue
        ///   * Returns zero if fValue <= zero
        /// </summary>
        public static float log(float fValue)
        {
            return NWN.Core.NWScript.log(fValue);
        }

        /// <summary>
        ///   Maths operation: fValue is raised to the power of fExponent
        ///   * Returns zero if fValue ==0 and fExponent <0
        /// </summary>
        public static float pow(float fValue, float fExponent)
        {
            return NWN.Core.NWScript.pow(fValue, fExponent);
        }

        /// <summary>
        ///   Maths operation: square root of fValue
        ///   * Returns zero if fValue <0
        /// </summary>
        public static float sqrt(float fValue)
        {
            return NWN.Core.NWScript.sqrt(fValue);
        }

        /// <summary>
        ///   Maths operation: integer absolute value of nValue
        ///   * Return value on error: 0
        /// </summary>
        public static int abs(int nValue)
        {
            return NWN.Core.NWScript.abs(nValue);
        }

        /// <summary>
        ///   Normalize vVector
        /// </summary>
        public static Vector3 VectorNormalize(Vector3 vVector)
        {
            return NWN.Core.NWScript.VectorNormalize(vVector);
        }

        /// <summary>
        ///   Get the magnitude of vVector; this can be used to determine the
        ///   distance between two points.
        ///   * Return value on error: 0.0f
        /// </summary>
        public static float VectorMagnitude(Vector3 vVector)
        {
            return NWN.Core.NWScript.VectorMagnitude(vVector);
        }

        /// <summary>
        ///   Convert fFeet into a number of meters.
        /// </summary>
        public static float FeetToMeters(float fFeet)
        {
            return NWN.Core.NWScript.FeetToMeters(fFeet);
        }

        /// <summary>
        ///   Convert fYards into a number of meters.
        /// </summary>
        public static float YardsToMeters(float fYards)
        {
            return NWN.Core.NWScript.YardsToMeters(fYards);
        }

        /// <summary>
        ///   Get the distance from the caller to oObject in metres.
        ///   * Return value on error: -1.0f
        /// </summary>
        public static float GetDistanceToObject(uint oObject)
        {
            return NWN.Core.NWScript.GetDistanceToObject(oObject);
        }

        /// <summary>
        ///   Returns whether or not there is a direct line of sight
        ///   between the two objects. (Not blocked by any geometry).
        ///   PLEASE NOTE: This is an expensive function and may
        ///   degrade performance if used frequently.
        /// </summary>
        public static bool LineOfSightObject(uint oSource, uint oTarget)
        {
            return NWN.Core.NWScript.LineOfSightObject(oSource, oTarget) != 0;
        }

        /// <summary>
        ///   Returns whether or not there is a direct line of sight
        ///   between the two vectors. (Not blocked by any geometry).
        ///   This function must be run on a valid object in the area
        ///   it will not work on the module or area.
        ///   PLEASE NOTE: This is an expensive function and may
        ///   degrade performance if used frequently.
        /// </summary>
        public static bool LineOfSightVector(Vector3 vSource, Vector3 vTarget)
        {
            return NWN.Core.NWScript.LineOfSightVector(vSource, vTarget) != 0;
        }

        /// <summary>
        ///   Convert fAngle to a vector
        /// </summary>
        public static Vector3 AngleToVector(float fAngle)
        {
            return NWN.Core.NWScript.AngleToVector(fAngle);
        }

        /// <summary>
        ///   Convert vVector to an angle
        /// </summary>
        public static float VectorToAngle(Vector3 vVector)
        {
            return NWN.Core.NWScript.VectorToAngle(vVector);
        }
    }
}