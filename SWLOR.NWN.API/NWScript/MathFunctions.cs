using System.Numerics;

namespace SWLOR.NWN.API.NWScript
{
    public partial class NWScript
    {
        /// <summary>
        ///   math operations
        ///   Maths operation: absolute value of fValue
        /// </summary>
        public static float fabs(float fValue)
        {
            return global::NWN.Core.NWScript.fabs(fValue);
        }

        /// <summary>
        ///   Maths operation: cosine of fValue
        /// </summary>
        public static float cos(float fValue)
        {
            return global::NWN.Core.NWScript.cos(fValue);
        }

        /// <summary>
        ///   Maths operation: sine of fValue
        /// </summary>
        public static float sin(float fValue)
        {
            return global::NWN.Core.NWScript.sin(fValue);
        }

        /// <summary>
        ///   Maths operation: tan of fValue
        /// </summary>
        public static float tan(float fValue)
        {
            return global::NWN.Core.NWScript.tan(fValue);
        }

        /// <summary>
        ///   Maths operation: arccosine of fValue
        ///   * Returns zero if fValue > 1 or fValue < -1
        /// </summary>
        public static float acos(float fValue)
        {
            return global::NWN.Core.NWScript.acos(fValue);
        }

        /// <summary>
        ///   Maths operation: arcsine of fValue
        ///   * Returns zero if fValue >1 or fValue < -1
        /// </summary>
        public static float asin(float fValue)
        {
            return global::NWN.Core.NWScript.asin(fValue);
        }

        /// <summary>
        ///   Maths operation: arctan of fValue
        /// </summary>
        public static float atan(float fValue)
        {
            return global::NWN.Core.NWScript.atan(fValue);
        }

        /// <summary>
        ///   Maths operation: log of fValue
        ///   * Returns zero if fValue <= zero
        /// </summary>
        public static float log(float fValue)
        {
            return global::NWN.Core.NWScript.log(fValue);
        }

        /// <summary>
        ///   Maths operation: fValue is raised to the power of fExponent
        ///   * Returns zero if fValue ==0 and fExponent <0
        /// </summary>
        public static float pow(float fValue, float fExponent)
        {
            return global::NWN.Core.NWScript.pow(fValue, fExponent);
        }

        /// <summary>
        ///   Maths operation: square root of fValue
        ///   * Returns zero if fValue <0
        /// </summary>
        public static float sqrt(float fValue)
        {
            return global::NWN.Core.NWScript.sqrt(fValue);
        }

        /// <summary>
        ///   Maths operation: integer absolute value of nValue
        ///   * Return value on error: 0
        /// </summary>
        public static int abs(int nValue)
        {
            return global::NWN.Core.NWScript.abs(nValue);
        }

        /// <summary>
        ///   Normalize vVector
        /// </summary>
        public static Vector3 VectorNormalize(Vector3 vVector)
        {
            return global::NWN.Core.NWScript.VectorNormalize(vVector);
        }

        /// <summary>
        ///   Get the magnitude of vVector; this can be used to determine the
        ///   distance between two points.
        ///   * Return value on error: 0.0f
        /// </summary>
        public static float VectorMagnitude(Vector3 vVector)
        {
            return global::NWN.Core.NWScript.VectorMagnitude(vVector);
        }

        /// <summary>
        ///   Convert fFeet into a number of meters.
        /// </summary>
        public static float FeetToMeters(float fFeet)
        {
            return global::NWN.Core.NWScript.FeetToMeters(fFeet);
        }

        /// <summary>
        ///   Convert fYards into a number of meters.
        /// </summary>
        public static float YardsToMeters(float fYards)
        {
            return global::NWN.Core.NWScript.YardsToMeters(fYards);
        }

        /// <summary>
        ///   Get the distance from the caller to oObject in metres.
        ///   * Return value on error: -1.0f
        /// </summary>
        public static float GetDistanceToObject(uint oObject)
        {
            return global::NWN.Core.NWScript.GetDistanceToObject(oObject);
        }

        /// <summary>
        ///   Returns whether or not there is a direct line of sight
        ///   between the two objects. (Not blocked by any geometry).
        ///   PLEASE NOTE: This is an expensive function and may
        ///   degrade performance if used frequently.
        /// </summary>
        public static bool LineOfSightObject(uint oSource, uint oTarget)
        {
            return global::NWN.Core.NWScript.LineOfSightObject(oSource, oTarget) != 0;
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
            return global::NWN.Core.NWScript.LineOfSightVector(vSource, vTarget) != 0;
        }

        /// <summary>
        ///   Convert fAngle to a vector
        /// </summary>
        public static Vector3 AngleToVector(float fAngle)
        {
            return global::NWN.Core.NWScript.AngleToVector(fAngle);
        }

        /// <summary>
        ///   Convert vVector to an angle
        /// </summary>
        public static float VectorToAngle(Vector3 vVector)
        {
            return global::NWN.Core.NWScript.VectorToAngle(vVector);
        }

        /// <summary>
        ///   Get the total from rolling (nNumDice x d2 dice).
        ///   - nNumDice: If this is less than 1, the value 1 will be used.
        /// </summary>
        public static int d2(int nNumDice = 1)
        {
            return global::NWN.Core.NWScript.d2(nNumDice);
        }

        /// <summary>
        ///   Get the total from rolling (nNumDice x d3 dice).
        ///   - nNumDice: If this is less than 1, the value 1 will be used.
        /// </summary>
        public static int d3(int nNumDice = 1)
        {
            return global::NWN.Core.NWScript.d3(nNumDice);
        }

        /// <summary>
        ///   Get the total from rolling (nNumDice x d4 dice).
        ///   - nNumDice: If this is less than 1, the value 1 will be used.
        /// </summary>
        public static int d4(int nNumDice = 1)
        {
            return global::NWN.Core.NWScript.d4(nNumDice);
        }

        /// <summary>
        ///   Get the total from rolling (nNumDice x d6 dice).
        ///   - nNumDice: If this is less than 1, the value 1 will be used.
        /// </summary>
        public static int d6(int nNumDice = 1)
        {
            return global::NWN.Core.NWScript.d6(nNumDice);
        }

        /// <summary>
        ///   Get the total from rolling (nNumDice x d8 dice).
        ///   - nNumDice: If this is less than 1, the value 1 will be used.
        /// </summary>
        public static int d8(int nNumDice = 1)
        {
            return global::NWN.Core.NWScript.d8(nNumDice);
        }

        /// <summary>
        ///   Get the total from rolling (nNumDice x d10 dice).
        ///   - nNumDice: If this is less than 1, the value 1 will be used.
        /// </summary>
        public static int d10(int nNumDice = 1)
        {
            return global::NWN.Core.NWScript.d10(nNumDice);
        }

        /// <summary>
        ///   Get the total from rolling (nNumDice x d12 dice).
        ///   - nNumDice: If this is less than 1, the value 1 will be used.
        /// </summary>
        public static int d12(int nNumDice = 1)
        {
            return global::NWN.Core.NWScript.d12(nNumDice);
        }

        /// <summary>
        ///   Get the total from rolling (nNumDice x d20 dice).
        ///   - nNumDice: If this is less than 1, the value 1 will be used.
        /// </summary>
        public static int d20(int nNumDice = 1)
        {
            return global::NWN.Core.NWScript.d20(nNumDice);
        }

        /// <summary>
        ///   Get the total from rolling (nNumDice x d100 dice).
        ///   - nNumDice: If this is less than 1, the value 1 will be used.
        /// </summary>
        public static int d100(int nNumDice = 1)
        {
            return global::NWN.Core.NWScript.d100(nNumDice);
        }

        /// <summary>
        ///   Output vVector to the logfile.
        ///   - vVector
        ///   - bPrepend: if this is TRUE, the message will be prefixed with "PRINTVECTOR:"
        /// </summary>
        public static void PrintVector(Vector3 vVector, bool bPrepend = false)
        {
            global::NWN.Core.NWScript.PrintVector(vVector, bPrepend ? 1 : 0);
        }

        /// <summary>
        ///   Create a vector with the specified values for x, y and z
        /// </summary>
        public static Vector3 Vector3(float x = 0.0f, float y = 0.0f, float z = 0.0f)
        {
            return global::NWN.Core.NWScript.Vector(x, y, z);
        }

        /// <summary>
        ///   Convert nInteger into a floating point number.
        /// </summary>
        public static float IntToFloat(int nInteger)
        {
            return global::NWN.Core.NWScript.IntToFloat(nInteger);
        }

        /// <summary>
        ///   Convert fFloat into the nearest integer.
        /// </summary>
        public static int FloatToInt(float fFloat)
        {
            return global::NWN.Core.NWScript.FloatToInt(fFloat);
        }

        /// <summary>
        ///   Convert sNumber into an integer.
        /// </summary>
        public static int StringToInt(string sNumber)
        {
            return global::NWN.Core.NWScript.StringToInt(sNumber);
        }

        /// <summary>
        ///   Convert sNumber into a floating point number.
        /// </summary>
        public static float StringToFloat(string sNumber)
        {
            return global::NWN.Core.NWScript.StringToFloat(sNumber);
        }
    }
}