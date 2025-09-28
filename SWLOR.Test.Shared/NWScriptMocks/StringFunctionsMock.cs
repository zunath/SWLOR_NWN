using System.Text.RegularExpressions;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Test.Shared.NWScriptMocks
{
    public partial class NWScriptServiceMock
    {
        // Mock data storage for string functions
        private readonly Dictionary<int, string> _tlkOverrides = new();
        private readonly Dictionary<int, string> _customTokens = new();
        private readonly List<string> _matchedSubstrings = new();
        private readonly Dictionary<int, string> _strRefStrings = new();

        public int GetStringLength(string sString) => sString?.Length ?? 0;

        public string GetStringUpperCase(string sString) => sString?.ToUpper() ?? "";

        public string GetStringLowerCase(string sString) => sString?.ToLower() ?? "";

        public string GetStringRight(string sString, int nCount) 
        {
            if (string.IsNullOrEmpty(sString) || nCount <= 0)
                return "";
            if (nCount >= sString.Length)
                return sString;
            return sString.Substring(sString.Length - nCount);
        }

        public string GetStringLeft(string sString, int nCount) 
        {
            if (string.IsNullOrEmpty(sString) || nCount <= 0)
                return "";
            if (nCount >= sString.Length)
                return sString;
            return sString.Substring(0, nCount);
        }

        public string InsertString(string sDestination, string sString, int nPosition) 
        {
            if (string.IsNullOrEmpty(sDestination))
                return sString ?? "";
            if (string.IsNullOrEmpty(sString))
                return sDestination;
            if (nPosition < 0)
                nPosition = 0;
            if (nPosition > sDestination.Length)
                nPosition = sDestination.Length;
            return sDestination.Insert(nPosition, sString);
        }

        public string GetSubString(string sString, int nStart, int nCount) 
        {
            if (string.IsNullOrEmpty(sString) || nStart < 0 || nCount <= 0)
                return "";
            if (nStart >= sString.Length)
                return "";
            if (nStart + nCount > sString.Length)
                nCount = sString.Length - nStart;
            return sString.Substring(nStart, nCount);
        }

        public int FindSubString(string sString, string sSubString, int nStart = 0) 
        {
            if (string.IsNullOrEmpty(sString) || string.IsNullOrEmpty(sSubString) || nStart < 0)
                return -1;
            if (nStart >= sString.Length)
                return -1;
            return sString.IndexOf(sSubString, nStart);
        }

        public bool TestStringAgainstPattern(string sPattern, string sStringToTest) 
        {
            if (string.IsNullOrEmpty(sPattern) || string.IsNullOrEmpty(sStringToTest))
                return false;
            try
            {
                var regex = new Regex(sPattern);
                var match = regex.Match(sStringToTest);
                if (match.Success)
                {
                    _matchedSubstrings.Clear();
                    foreach (Group group in match.Groups)
                    {
                        if (group.Success)
                            _matchedSubstrings.Add(group.Value);
                    }
                }
                return match.Success;
            }
            catch
            {
                return false;
            }
        }

        public string GetMatchedSubstring(int nString) 
        {
            if (nString >= 0 && nString < _matchedSubstrings.Count)
                return _matchedSubstrings[nString];
            return "";
        }

        public int GetMatchedSubstringsCount() => _matchedSubstrings.Count;

        public string RegExpReplace(string sString, string sPattern, string sReplacement, int nFlags = 0) 
        {
            if (string.IsNullOrEmpty(sString) || string.IsNullOrEmpty(sPattern))
                return sString ?? "";
            try
            {
                var regex = new Regex(sPattern);
                return regex.Replace(sString, sReplacement);
            }
            catch
            {
                return sString;
            }
        }

        public void SetTlkOverride(int nStrRef, string sValue = "") 
        {
            _tlkOverrides[nStrRef] = sValue;
        }

        public string FloatToString(float fFloat, int nWidth = 18, int nDecimals = 9) 
        {
            return fFloat.ToString($"F{nDecimals}");
        }

        public string IntToString(int nInteger) => nInteger.ToString();

        public string GetStringByStrRef(int nStrRef, GenderType nGender = GenderType.Male) 
        {
            if (_tlkOverrides.ContainsKey(nStrRef))
                return _tlkOverrides[nStrRef];
            if (_strRefStrings.ContainsKey(nStrRef))
                return _strRefStrings[nStrRef];
            return $"StrRef_{nStrRef}";
        }

        public string RandomName(NameType nNameType = NameType.FirstGenericMale) 
        {
            // Mock implementation - return a simple name
            return nNameType switch
            {
                NameType.FirstGenericMale => "John",
                NameType.FirstHumanFemale => "Jane",
                NameType.LastHuman => "Smith",
                _ => "Generic"
            };
        }

        public void SetCustomToken(int nCustomTokenNumber, string sTokenValue) 
        {
            _customTokens[nCustomTokenNumber] = sTokenValue;
        }

        // Additional UI/string methods from INWScriptService
        public void PostString(uint PC, string Msg, int X = 0, int Y = 0, ScreenAnchorType anchor = ScreenAnchorType.TopLeft,
            float life = 10, int RGBA = 2147418367, int RGBA2 = 2147418367, int ID = 0, string font = "") 
        {
            // Mock implementation - track posted strings
        }

        public float SetObjectVisualTransform(uint oObject, ObjectVisualTransformType nTransform, float fValue,
            LerpType nLerpType = LerpType.None, float fLerpDuration = 0, bool bPauseWithGame = true,
            ObjectVisualTransformDataScopeType nScope = ObjectVisualTransformDataScopeType.Base,
            ObjectVisualTransformBehaviorType nBehaviorFlags = ObjectVisualTransformBehaviorType.Default, int nRepeats = 0) 
        {
            // Mock implementation - return the value
            return fValue;
        }

        public void SetEnterTargetingModeData(uint oPlayer, int nShape, float fSizeX, float fSizeY, int nFlags, float fRange = 0,
            SpellType nSpell = SpellType.AllSpells, FeatType nFeat = FeatType.Invalid) 
        {
            // Mock implementation - track targeting mode data
        }

        // Helper methods for testing
    }
}
