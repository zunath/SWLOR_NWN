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
            VM.StackPush(fValue);
            VM.Call(67);
            return VM.StackPopFloat();
        }

        /// <summary>
        ///   Maths operation: cosine of fValue
        /// </summary>
        public static float cos(float fValue)
        {
            VM.StackPush(fValue);
            VM.Call(68);
            return VM.StackPopFloat();
        }

        /// <summary>
        ///   Maths operation: sine of fValue
        /// </summary>
        public static float sin(float fValue)
        {
            VM.StackPush(fValue);
            VM.Call(69);
            return VM.StackPopFloat();
        }

        /// <summary>
        ///   Maths operation: tan of fValue
        /// </summary>
        public static float tan(float fValue)
        {
            VM.StackPush(fValue);
            VM.Call(70);
            return VM.StackPopFloat();
        }

        /// <summary>
        ///   Maths operation: arccosine of fValue
        ///   * Returns zero if fValue > 1 or fValue < -1
        /// </summary>
        public static float acos(float fValue)
        {
            VM.StackPush(fValue);
            VM.Call(71);
            return VM.StackPopFloat();
        }

        /// <summary>
        ///   Maths operation: arcsine of fValue
        ///   * Returns zero if fValue >1 or fValue < -1
        /// </summary>
        public static float asin(float fValue)
        {
            VM.StackPush(fValue);
            VM.Call(72);
            return VM.StackPopFloat();
        }

        /// <summary>
        ///   Maths operation: arctan of fValue
        /// </summary>
        public static float atan(float fValue)
        {
            VM.StackPush(fValue);
            VM.Call(73);
            return VM.StackPopFloat();
        }

        /// <summary>
        ///   Maths operation: log of fValue
        ///   * Returns zero if fValue <= zero
        /// </summary>
        public static float log(float fValue)
        {
            VM.StackPush(fValue);
            VM.Call(74);
            return VM.StackPopFloat();
        }

        /// <summary>
        ///   Maths operation: fValue is raised to the power of fExponent
        ///   * Returns zero if fValue ==0 and fExponent <0
        /// </summary>
        public static float pow(float fValue, float fExponent)
        {
            VM.StackPush(fExponent);
            VM.StackPush(fValue);
            VM.Call(75);
            return VM.StackPopFloat();
        }

        /// <summary>
        ///   Maths operation: square root of fValue
        ///   * Returns zero if fValue <0
        /// </summary>
        public static float sqrt(float fValue)
        {
            VM.StackPush(fValue);
            VM.Call(76);
            return VM.StackPopFloat();
        }

        /// <summary>
        ///   Maths operation: integer absolute value of nValue
        ///   * Return value on error: 0
        /// </summary>
        public static int abs(int nValue)
        {
            VM.StackPush(nValue);
            VM.Call(77);
            return VM.StackPopInt();
        }

        /// <summary>
        ///   Normalize vVector
        /// </summary>
        public static Vector3 VectorNormalize(Vector3 vVector)
        {
            VM.StackPush(vVector);
            VM.Call(137);
            return VM.StackPopVector();
        }

        /// <summary>
        ///   Get the magnitude of vVector; this can be used to determine the
        ///   distance between two points.
        ///   * Return value on error: 0.0f
        /// </summary>
        public static float VectorMagnitude(Vector3 vVector)
        {
            VM.StackPush(vVector);
            VM.Call(104);
            return VM.StackPopFloat();
        }

        /// <summary>
        ///   Convert fFeet into a number of meters.
        /// </summary>
        public static float FeetToMeters(float fFeet)
        {
            VM.StackPush(fFeet);
            VM.Call(218);
            return VM.StackPopFloat();
        }

        /// <summary>
        ///   Convert fYards into a number of meters.
        /// </summary>
        public static float YardsToMeters(float fYards)
        {
            VM.StackPush(fYards);
            VM.Call(219);
            return VM.StackPopFloat();
        }

        /// <summary>
        ///   Get the distance from the caller to oObject in metres.
        ///   * Return value on error: -1.0f
        /// </summary>
        public static float GetDistanceToObject(uint oObject)
        {
            VM.StackPush(oObject);
            VM.Call(41);
            return VM.StackPopFloat();
        }

        /// <summary>
        ///   Returns whether or not there is a direct line of sight
        ///   between the two objects. (Not blocked by any geometry).
        ///   PLEASE NOTE: This is an expensive function and may
        ///   degrade performance if used frequently.
        /// </summary>
        public static bool LineOfSightObject(uint oSource, uint oTarget)
        {
            VM.StackPush(oTarget);
            VM.StackPush(oSource);
            VM.Call(752);
            return VM.StackPopInt() == 1;
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
            VM.StackPush(vTarget);
            VM.StackPush(vSource);
            VM.Call(753);
            return VM.StackPopInt() == 1;
        }

        /// <summary>
        ///   Convert fAngle to a vector
        /// </summary>
        public static Vector3 AngleToVector(float fAngle)
        {
            VM.StackPush(fAngle);
            VM.Call(144);
            return VM.StackPopVector();
        }

        /// <summary>
        ///   Convert vVector to an angle
        /// </summary>
        public static float VectorToAngle(Vector3 vVector)
        {
            VM.StackPush(vVector);
            VM.Call(145);
            return VM.StackPopFloat();
        }
    }
}