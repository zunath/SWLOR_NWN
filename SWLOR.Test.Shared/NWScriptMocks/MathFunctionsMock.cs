using System.Numerics;

namespace SWLOR.Test.Shared.NWScript
{
    public partial class NWScriptServiceMock
    {
        // Mock data storage for math operations
        private readonly List<Vector3> _printedVectors = new();
        private readonly Dictionary<uint, Vector3> _mathObjectPositions = new();
        private readonly Dictionary<string, int> _lineOfSightCache = new();

        public float fabs(float fValue) => Math.Abs(fValue);

        public float cos(float fValue) => (float)Math.Cos(fValue);

        public float sin(float fValue) => (float)Math.Sin(fValue);

        public float tan(float fValue) => (float)Math.Tan(fValue);

        public float acos(float fValue) => (float)Math.Acos(fValue);

        public float asin(float fValue) => (float)Math.Asin(fValue);

        public float atan(float fValue) => (float)Math.Atan(fValue);

        public float log(float fValue) => (float)Math.Log(fValue);

        public float pow(float fValue, float fExponent) => (float)Math.Pow(fValue, fExponent);

        public float sqrt(float fValue) => (float)Math.Sqrt(fValue);

        public int abs(int nValue) => Math.Abs(nValue);

        public Vector3 VectorNormalize(Vector3 vVector) 
        {
            if (vVector == new Vector3(0, 0, 0)) return new Vector3(0, 0, 0);
            return System.Numerics.Vector3.Normalize(vVector);
        }

        public float VectorMagnitude(Vector3 vVector) => vVector.Length();

        public float FeetToMeters(float fFeet) => fFeet * 0.3048f;

        public float YardsToMeters(float fYards) => fYards * 0.9144f;

        public float GetDistanceToObject(uint oObject, uint oFrom = OBJECT_INVALID) 
        {
            var fromPos = _mathObjectPositions.GetValueOrDefault(oFrom, new Vector3(0, 0, 0));
            var toPos = _mathObjectPositions.GetValueOrDefault(oObject, new Vector3(0, 0, 0));
            return System.Numerics.Vector3.Distance(fromPos, toPos);
        }

        public bool LineOfSightObject(uint oSource, uint oTarget) 
        {
            var key = $"{oSource}|{oTarget}";
            return _lineOfSightCache.GetValueOrDefault(key, 1) == 1;
        }

        public bool LineOfSightVector(Vector3 vSource, Vector3 vTarget) 
        {
            // Mock implementation - assume line of sight exists
            return true;
        }

        public Vector3 AngleToVector(float fAngle) 
        {
            return new Vector3((float)Math.Cos(fAngle), (float)Math.Sin(fAngle), 0.0f);
        }

        public float VectorToAngle(Vector3 vVector) 
        {
            return (float)Math.Atan2(vVector.Y, vVector.X);
        }

        public int d2(int nNumDice = 1) => RollDice(2, nNumDice);
        public int d3(int nNumDice = 1) => RollDice(3, nNumDice);
        public int d4(int nNumDice = 1) => RollDice(4, nNumDice);
        public int d6(int nNumDice = 1) => RollDice(6, nNumDice);
        public int d8(int nNumDice = 1) => RollDice(8, nNumDice);
        public int d10(int nNumDice = 1) => RollDice(10, nNumDice);
        public int d12(int nNumDice = 1) => RollDice(12, nNumDice);
        public int d20(int nNumDice = 1) => RollDice(20, nNumDice);
        public int d100(int nNumDice = 1) => RollDice(100, nNumDice);

        private int RollDice(int nSides, int nNumDice)
        {
            int total = 0;
            var random = new Random();
            for (int i = 0; i < nNumDice; i++)
            {
                total += random.Next(1, nSides + 1);
            }
            return total;
        }

        public void PrintVector(Vector3 vVector, bool bPrepend = false) 
        {
            _printedVectors.Add(vVector);
        }

        public Vector3 CreateVector3(float x = 0.0f, float y = 0.0f, float z = 0.0f) =>
            new Vector3(x, y, z);

        public Vector3 Vector3(float x = 0, float y = 0, float z = 0) =>
            new Vector3(x, y, z);

        public float IntToFloat(int nInteger) => (float)nInteger;

        public int FloatToInt(float fFloat) => (int)fFloat;

        public int StringToInt(string sNumber) 
        {
            if (int.TryParse(sNumber, out int result))
                return result;
            return 0;
        }

        public float StringToFloat(string sNumber) 
        {
            if (float.TryParse(sNumber, out float result))
                return result;
            return 0.0f;
        }

        // Additional math methods from INWScriptService
        // Note: CreateVector3 is already defined above

        // Helper methods for testing
        public List<Vector3> GetPrintedVectors() => _printedVectors;
        public void ClearPrintedVectors() => _printedVectors.Clear();
        public void SetMathObjectPosition(uint oObject, Vector3 position) => _mathObjectPositions[oObject] = position;
        public void SetLineOfSight(uint oSource, uint oTarget, bool hasLineOfSight) 
        {
            var key = $"{oSource}|{oTarget}";
            _lineOfSightCache[key] = hasLineOfSight ? 1 : 0;
        }

    }
}
