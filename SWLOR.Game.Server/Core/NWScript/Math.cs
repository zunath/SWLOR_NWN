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
            Internal.NativeFunctions.StackPushFloat(fValue);
            Internal.NativeFunctions.CallBuiltIn(67);
            return Internal.NativeFunctions.StackPopFloat();
        }

        /// <summary>
        ///   Maths operation: cosine of fValue
        /// </summary>
        public static float cos(float fValue)
        {
            Internal.NativeFunctions.StackPushFloat(fValue);
            Internal.NativeFunctions.CallBuiltIn(68);
            return Internal.NativeFunctions.StackPopFloat();
        }

        /// <summary>
        ///   Maths operation: sine of fValue
        /// </summary>
        public static float sin(float fValue)
        {
            Internal.NativeFunctions.StackPushFloat(fValue);
            Internal.NativeFunctions.CallBuiltIn(69);
            return Internal.NativeFunctions.StackPopFloat();
        }

        /// <summary>
        ///   Maths operation: tan of fValue
        /// </summary>
        public static float tan(float fValue)
        {
            Internal.NativeFunctions.StackPushFloat(fValue);
            Internal.NativeFunctions.CallBuiltIn(70);
            return Internal.NativeFunctions.StackPopFloat();
        }

        /// <summary>
        ///   Maths operation: arccosine of fValue
        ///   * Returns zero if fValue > 1 or fValue < -1
        /// </summary>
        public static float acos(float fValue)
        {
            Internal.NativeFunctions.StackPushFloat(fValue);
            Internal.NativeFunctions.CallBuiltIn(71);
            return Internal.NativeFunctions.StackPopFloat();
        }

        /// <summary>
        ///   Maths operation: arcsine of fValue
        ///   * Returns zero if fValue >1 or fValue < -1
        /// </summary>
        public static float asin(float fValue)
        {
            Internal.NativeFunctions.StackPushFloat(fValue);
            Internal.NativeFunctions.CallBuiltIn(72);
            return Internal.NativeFunctions.StackPopFloat();
        }

        /// <summary>
        ///   Maths operation: arctan of fValue
        /// </summary>
        public static float atan(float fValue)
        {
            Internal.NativeFunctions.StackPushFloat(fValue);
            Internal.NativeFunctions.CallBuiltIn(73);
            return Internal.NativeFunctions.StackPopFloat();
        }

        /// <summary>
        ///   Maths operation: log of fValue
        ///   * Returns zero if fValue <= zero
        /// </summary>
        public static float log(float fValue)
        {
            Internal.NativeFunctions.StackPushFloat(fValue);
            Internal.NativeFunctions.CallBuiltIn(74);
            return Internal.NativeFunctions.StackPopFloat();
        }

        /// <summary>
        ///   Maths operation: fValue is raised to the power of fExponent
        ///   * Returns zero if fValue ==0 and fExponent <0
        /// </summary>
        public static float pow(float fValue, float fExponent)
        {
            Internal.NativeFunctions.StackPushFloat(fExponent);
            Internal.NativeFunctions.StackPushFloat(fValue);
            Internal.NativeFunctions.CallBuiltIn(75);
            return Internal.NativeFunctions.StackPopFloat();
        }

        /// <summary>
        ///   Maths operation: square root of fValue
        ///   * Returns zero if fValue <0
        /// </summary>
        public static float sqrt(float fValue)
        {
            Internal.NativeFunctions.StackPushFloat(fValue);
            Internal.NativeFunctions.CallBuiltIn(76);
            return Internal.NativeFunctions.StackPopFloat();
        }

        /// <summary>
        ///   Maths operation: integer absolute value of nValue
        ///   * Return value on error: 0
        /// </summary>
        public static int abs(int nValue)
        {
            Internal.NativeFunctions.StackPushInteger(nValue);
            Internal.NativeFunctions.CallBuiltIn(77);
            return Internal.NativeFunctions.StackPopInteger();
        }

        /// <summary>
        ///   Normalize vVector
        /// </summary>
        public static Vector3 VectorNormalize(Vector3 vVector)
        {
            Internal.NativeFunctions.StackPushVector(vVector);
            Internal.NativeFunctions.CallBuiltIn(137);
            return Internal.NativeFunctions.StackPopVector();
        }

        /// <summary>
        ///   Get the magnitude of vVector; this can be used to determine the
        ///   distance between two points.
        ///   * Return value on error: 0.0f
        /// </summary>
        public static float VectorMagnitude(Vector3 vVector)
        {
            Internal.NativeFunctions.StackPushVector(vVector);
            Internal.NativeFunctions.CallBuiltIn(104);
            return Internal.NativeFunctions.StackPopFloat();
        }

        /// <summary>
        ///   Convert fFeet into a number of meters.
        /// </summary>
        public static float FeetToMeters(float fFeet)
        {
            Internal.NativeFunctions.StackPushFloat(fFeet);
            Internal.NativeFunctions.CallBuiltIn(218);
            return Internal.NativeFunctions.StackPopFloat();
        }

        /// <summary>
        ///   Convert fYards into a number of meters.
        /// </summary>
        public static float YardsToMeters(float fYards)
        {
            Internal.NativeFunctions.StackPushFloat(fYards);
            Internal.NativeFunctions.CallBuiltIn(219);
            return Internal.NativeFunctions.StackPopFloat();
        }

        /// <summary>
        ///   Get the distance from the caller to oObject in metres.
        ///   * Return value on error: -1.0f
        /// </summary>
        public static float GetDistanceToObject(uint oObject)
        {
            Internal.NativeFunctions.StackPushObject(oObject);
            Internal.NativeFunctions.CallBuiltIn(41);
            return Internal.NativeFunctions.StackPopFloat();
        }

        /// <summary>
        ///   Returns whether or not there is a direct line of sight
        ///   between the two objects. (Not blocked by any geometry).
        ///   PLEASE NOTE: This is an expensive function and may
        ///   degrade performance if used frequently.
        /// </summary>
        public static bool LineOfSightObject(uint oSource, uint oTarget)
        {
            Internal.NativeFunctions.StackPushObject(oTarget);
            Internal.NativeFunctions.StackPushObject(oSource);
            Internal.NativeFunctions.CallBuiltIn(752);
            return Internal.NativeFunctions.StackPopInteger() == 1;
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
            Internal.NativeFunctions.StackPushVector(vTarget);
            Internal.NativeFunctions.StackPushVector(vSource);
            Internal.NativeFunctions.CallBuiltIn(753);
            return Internal.NativeFunctions.StackPopInteger() == 1;
        }

        /// <summary>
        ///   Convert fAngle to a vector
        /// </summary>
        public static Vector3 AngleToVector(float fAngle)
        {
            Internal.NativeFunctions.StackPushFloat(fAngle);
            Internal.NativeFunctions.CallBuiltIn(144);
            return Internal.NativeFunctions.StackPopVector();
        }

        /// <summary>
        ///   Convert vVector to an angle
        /// </summary>
        public static float VectorToAngle(Vector3 vVector)
        {
            Internal.NativeFunctions.StackPushVector(vVector);
            Internal.NativeFunctions.CallBuiltIn(145);
            return Internal.NativeFunctions.StackPopFloat();
        }
    }
}