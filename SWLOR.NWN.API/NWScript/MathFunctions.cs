using System.Numerics;

namespace SWLOR.NWN.API.NWScript
{
    public partial class NWScript
    {
        /// <summary>
        /// Maths operation: absolute value of the value.
        /// </summary>
        /// <param name="fValue">The value to get the absolute value of</param>
        /// <returns>The absolute value</returns>
        public static float fabs(float fValue)
        {
            return global::NWN.Core.NWScript.fabs(fValue);
        }

        /// <summary>
        /// Maths operation: cosine of the value.
        /// </summary>
        /// <param name="fValue">The value to get the cosine of</param>
        /// <returns>The cosine value</returns>
        public static float cos(float fValue)
        {
            return global::NWN.Core.NWScript.cos(fValue);
        }

        /// <summary>
        /// Maths operation: sine of the value.
        /// </summary>
        /// <param name="fValue">The value to get the sine of</param>
        /// <returns>The sine value</returns>
        public static float sin(float fValue)
        {
            return global::NWN.Core.NWScript.sin(fValue);
        }

        /// <summary>
        /// Maths operation: tangent of the value.
        /// </summary>
        /// <param name="fValue">The value to get the tangent of</param>
        /// <returns>The tangent value</returns>
        public static float tan(float fValue)
        {
            return global::NWN.Core.NWScript.tan(fValue);
        }

        /// <summary>
        /// Maths operation: arccosine of the value.
        /// </summary>
        /// <param name="fValue">The value to get the arccosine of</param>
        /// <returns>The arccosine value, or zero if fValue > 1 or fValue < -1</returns>
        public static float acos(float fValue)
        {
            return global::NWN.Core.NWScript.acos(fValue);
        }

        /// <summary>
        /// Maths operation: arcsine of the value.
        /// </summary>
        /// <param name="fValue">The value to get the arcsine of</param>
        /// <returns>The arcsine value, or zero if fValue > 1 or fValue < -1</returns>
        public static float asin(float fValue)
        {
            return global::NWN.Core.NWScript.asin(fValue);
        }

        /// <summary>
        /// Maths operation: arctangent of the value.
        /// </summary>
        /// <param name="fValue">The value to get the arctangent of</param>
        /// <returns>The arctangent value</returns>
        public static float atan(float fValue)
        {
            return global::NWN.Core.NWScript.atan(fValue);
        }

        /// <summary>
        /// Maths operation: logarithm of the value.
        /// </summary>
        /// <param name="fValue">The value to get the logarithm of</param>
        /// <returns>The logarithm value, or zero if fValue <= zero</returns>
        public static float log(float fValue)
        {
            return global::NWN.Core.NWScript.log(fValue);
        }

        /// <summary>
        /// Maths operation: the value is raised to the power of the exponent.
        /// </summary>
        /// <param name="fValue">The base value</param>
        /// <param name="fExponent">The exponent</param>
        /// <returns>The result, or zero if fValue == 0 and fExponent < 0</returns>
        public static float pow(float fValue, float fExponent)
        {
            return global::NWN.Core.NWScript.pow(fValue, fExponent);
        }

        /// <summary>
        /// Maths operation: square root of the value.
        /// </summary>
        /// <param name="fValue">The value to get the square root of</param>
        /// <returns>The square root, or zero if fValue < 0</returns>
        public static float sqrt(float fValue)
        {
            return global::NWN.Core.NWScript.sqrt(fValue);
        }

        /// <summary>
        /// Maths operation: integer absolute value of the value.
        /// </summary>
        /// <param name="nValue">The value to get the absolute value of</param>
        /// <returns>The absolute value, or 0 on error</returns>
        public static int abs(int nValue)
        {
            return global::NWN.Core.NWScript.abs(nValue);
        }

        /// <summary>
        /// Normalizes the vector.
        /// </summary>
        /// <param name="vVector">The vector to normalize</param>
        /// <returns>The normalized vector</returns>
        public static Vector3 VectorNormalize(Vector3 vVector)
        {
            return global::NWN.Core.NWScript.VectorNormalize(vVector);
        }

        /// <summary>
        /// Gets the magnitude of the vector; this can be used to determine the
        /// distance between two points.
        /// </summary>
        /// <param name="vVector">The vector to get the magnitude of</param>
        /// <returns>The magnitude, or 0.0f on error</returns>
        public static float VectorMagnitude(Vector3 vVector)
        {
            return global::NWN.Core.NWScript.VectorMagnitude(vVector);
        }

        /// <summary>
        /// Converts feet into a number of meters.
        /// </summary>
        /// <param name="fFeet">The feet value to convert</param>
        /// <returns>The equivalent value in meters</returns>
        public static float FeetToMeters(float fFeet)
        {
            return global::NWN.Core.NWScript.FeetToMeters(fFeet);
        }

        /// <summary>
        /// Converts yards into a number of meters.
        /// </summary>
        /// <param name="fYards">The yards value to convert</param>
        /// <returns>The equivalent value in meters</returns>
        public static float YardsToMeters(float fYards)
        {
            return global::NWN.Core.NWScript.YardsToMeters(fYards);
        }

        /// <summary>
        /// Gets the distance from the source object to the target object in metres.
        /// </summary>
        /// <param name="oObject">The object to get the distance to</param>
        /// <param name="oFrom">The object to measure distance from (defaults to OBJECT_SELF)</param>
        /// <returns>The distance in metres, or -1.0f on error</returns>
        public static float GetDistanceToObject(uint oObject, uint oFrom = OBJECT_INVALID)
        {
            if (oFrom == OBJECT_INVALID)
                oFrom = OBJECT_SELF;
            return global::NWN.Core.NWScript.GetDistanceToObject(oObject, oFrom);
        }

        /// <summary>
        /// Returns whether or not there is a direct line of sight
        /// between the two objects. (Not blocked by any geometry).
        /// PLEASE NOTE: This is an expensive function and may
        /// degrade performance if used frequently.
        /// </summary>
        /// <param name="oSource">The source object</param>
        /// <param name="oTarget">The target object</param>
        /// <returns>TRUE if there is line of sight</returns>
        public static bool LineOfSightObject(uint oSource, uint oTarget)
        {
            return global::NWN.Core.NWScript.LineOfSightObject(oSource, oTarget) != 0;
        }

        /// <summary>
        /// Returns whether or not there is a direct line of sight
        /// between the two vectors. (Not blocked by any geometry).
        /// This function must be run on a valid object in the area
        /// it will not work on the module or area.
        /// PLEASE NOTE: This is an expensive function and may
        /// degrade performance if used frequently.
        /// </summary>
        /// <param name="vSource">The source vector</param>
        /// <param name="vTarget">The target vector</param>
        /// <returns>TRUE if there is line of sight</returns>
        public static bool LineOfSightVector(Vector3 vSource, Vector3 vTarget)
        {
            return global::NWN.Core.NWScript.LineOfSightVector(vSource, vTarget) != 0;
        }

        /// <summary>
        /// Converts the angle to a vector.
        /// </summary>
        /// <param name="fAngle">The angle to convert</param>
        /// <returns>The vector representation of the angle</returns>
        public static Vector3 AngleToVector(float fAngle)
        {
            return global::NWN.Core.NWScript.AngleToVector(fAngle);
        }

        /// <summary>
        /// Converts the vector to an angle.
        /// </summary>
        /// <param name="vVector">The vector to convert</param>
        /// <returns>The angle representation of the vector</returns>
        public static float VectorToAngle(Vector3 vVector)
        {
            return global::NWN.Core.NWScript.VectorToAngle(vVector);
        }

        /// <summary>
        /// Gets the total from rolling the specified number of d2 dice.
        /// If nNumDice is less than 1, the value 1 will be used.
        /// </summary>
        /// <param name="nNumDice">The number of dice to roll (defaults to 1)</param>
        /// <returns>The total from rolling the dice</returns>
        public static int d2(int nNumDice = 1)
        {
            return global::NWN.Core.NWScript.d2(nNumDice);
        }

        /// <summary>
        /// Gets the total from rolling the specified number of d3 dice.
        /// If nNumDice is less than 1, the value 1 will be used.
        /// </summary>
        /// <param name="nNumDice">The number of dice to roll (defaults to 1)</param>
        /// <returns>The total from rolling the dice</returns>
        public static int d3(int nNumDice = 1)
        {
            return global::NWN.Core.NWScript.d3(nNumDice);
        }

        /// <summary>
        /// Gets the total from rolling the specified number of d4 dice.
        /// If nNumDice is less than 1, the value 1 will be used.
        /// </summary>
        /// <param name="nNumDice">The number of dice to roll (defaults to 1)</param>
        /// <returns>The total from rolling the dice</returns>
        public static int d4(int nNumDice = 1)
        {
            return global::NWN.Core.NWScript.d4(nNumDice);
        }

        /// <summary>
        /// Gets the total from rolling the specified number of d6 dice.
        /// If nNumDice is less than 1, the value 1 will be used.
        /// </summary>
        /// <param name="nNumDice">The number of dice to roll (defaults to 1)</param>
        /// <returns>The total from rolling the dice</returns>
        public static int d6(int nNumDice = 1)
        {
            return global::NWN.Core.NWScript.d6(nNumDice);
        }

        /// <summary>
        /// Gets the total from rolling the specified number of d8 dice.
        /// If nNumDice is less than 1, the value 1 will be used.
        /// </summary>
        /// <param name="nNumDice">The number of dice to roll (defaults to 1)</param>
        /// <returns>The total from rolling the dice</returns>
        public static int d8(int nNumDice = 1)
        {
            return global::NWN.Core.NWScript.d8(nNumDice);
        }

        /// <summary>
        /// Gets the total from rolling the specified number of d10 dice.
        /// If nNumDice is less than 1, the value 1 will be used.
        /// </summary>
        /// <param name="nNumDice">The number of dice to roll (defaults to 1)</param>
        /// <returns>The total from rolling the dice</returns>
        public static int d10(int nNumDice = 1)
        {
            return global::NWN.Core.NWScript.d10(nNumDice);
        }

        /// <summary>
        /// Gets the total from rolling the specified number of d12 dice.
        /// If nNumDice is less than 1, the value 1 will be used.
        /// </summary>
        /// <param name="nNumDice">The number of dice to roll (defaults to 1)</param>
        /// <returns>The total from rolling the dice</returns>
        public static int d12(int nNumDice = 1)
        {
            return global::NWN.Core.NWScript.d12(nNumDice);
        }

        /// <summary>
        /// Gets the total from rolling the specified number of d20 dice.
        /// If nNumDice is less than 1, the value 1 will be used.
        /// </summary>
        /// <param name="nNumDice">The number of dice to roll (defaults to 1)</param>
        /// <returns>The total from rolling the dice</returns>
        public static int d20(int nNumDice = 1)
        {
            return global::NWN.Core.NWScript.d20(nNumDice);
        }

        /// <summary>
        /// Gets the total from rolling the specified number of d100 dice.
        /// If nNumDice is less than 1, the value 1 will be used.
        /// </summary>
        /// <param name="nNumDice">The number of dice to roll (defaults to 1)</param>
        /// <returns>The total from rolling the dice</returns>
        public static int d100(int nNumDice = 1)
        {
            return global::NWN.Core.NWScript.d100(nNumDice);
        }

        /// <summary>
        /// Outputs the vector to the logfile.
        /// </summary>
        /// <param name="vVector">The vector to output</param>
        /// <param name="bPrepend">If TRUE, the message will be prefixed with "PRINTVECTOR:" (defaults to false)</param>
        public static void PrintVector(Vector3 vVector, bool bPrepend = false)
        {
            global::NWN.Core.NWScript.PrintVector(vVector, bPrepend ? 1 : 0);
        }

        /// <summary>
        /// Creates a vector with the specified values for x, y and z.
        /// </summary>
        /// <param name="x">The x component (defaults to 0.0f)</param>
        /// <param name="y">The y component (defaults to 0.0f)</param>
        /// <param name="z">The z component (defaults to 0.0f)</param>
        /// <returns>The created vector</returns>
        public static Vector3 Vector3(float x = 0.0f, float y = 0.0f, float z = 0.0f)
        {
            return global::NWN.Core.NWScript.Vector(x, y, z);
        }

        /// <summary>
        /// Converts the integer into a floating point number.
        /// </summary>
        /// <param name="nInteger">The integer to convert</param>
        /// <returns>The floating point number</returns>
        public static float IntToFloat(int nInteger)
        {
            return global::NWN.Core.NWScript.IntToFloat(nInteger);
        }

        /// <summary>
        /// Converts the float into the nearest integer.
        /// </summary>
        /// <param name="fFloat">The float to convert</param>
        /// <returns>The nearest integer</returns>
        public static int FloatToInt(float fFloat)
        {
            return global::NWN.Core.NWScript.FloatToInt(fFloat);
        }

        /// <summary>
        /// Converts the string into an integer.
        /// </summary>
        /// <param name="sNumber">The string to convert</param>
        /// <returns>The integer value</returns>
        public static int StringToInt(string sNumber)
        {
            return global::NWN.Core.NWScript.StringToInt(sNumber);
        }

        /// <summary>
        /// Converts the string into a floating point number.
        /// </summary>
        /// <param name="sNumber">The string to convert</param>
        /// <returns>The floating point number</returns>
        public static float StringToFloat(string sNumber)
        {
            return global::NWN.Core.NWScript.StringToFloat(sNumber);
        }
    }
}