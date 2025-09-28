using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Test.Shared.NWScriptMocks
{
    public partial class NWScriptServiceMock
    {
        // Mock data storage for JSON operations
        private readonly Dictionary<Json, JsonData> _jsonData = new();
        private readonly Dictionary<uint, Dictionary<string, Json>> _localJsonVariables = new();

        private class JsonData
        {
            public JsonType Type { get; set; } = JsonType.Null;
            public string StringValue { get; set; } = "";
            public int IntValue { get; set; } = 0;
            public float FloatValue { get; set; } = 0.0f;
            public bool BoolValue { get; set; } = false;
            public Dictionary<string, Json> ObjectProperties { get; set; } = new();
            public List<Json> ArrayElements { get; set; } = new();
            public string Error { get; set; } = "";
            public int Length { get; set; } = 0;
        }

        public Json JsonParse(string jValue) 
        {
            var json = new Json(0);
            var data = GetOrCreateJsonData(json);
            data.Type = JsonType.String; // Simplified - would need actual JSON parsing
            data.StringValue = jValue;
            return json;
        }

        public string JsonDump(Json jValue, int nIndent = -1) 
        {
            var data = _jsonData.GetValueOrDefault(jValue, new JsonData());
            return data.StringValue;
        }

        public JsonType JsonGetType(Json jValue) => 
            _jsonData.GetValueOrDefault(jValue, new JsonData()).Type;

        public int JsonGetLength(Json jValue) => 
            _jsonData.GetValueOrDefault(jValue, new JsonData()).Length;

        public string JsonGetError(Json jValue) => 
            _jsonData.GetValueOrDefault(jValue, new JsonData()).Error;

        public Json JsonNull(string sError = "") 
        {
            var json = new Json(0);
            var data = GetOrCreateJsonData(json);
            data.Type = JsonType.Null;
            data.Error = sError;
            return json;
        }

        public Json JsonObject() 
        {
            var json = new Json(0);
            var data = GetOrCreateJsonData(json);
            data.Type = JsonType.Object;
            return json;
        }

        public Json JsonArray() 
        {
            var json = new Json(0);
            var data = GetOrCreateJsonData(json);
            data.Type = JsonType.Array;
            return json;
        }

        public Json JsonString(string sValue) 
        {
            var json = new Json(0);
            var data = GetOrCreateJsonData(json);
            data.Type = JsonType.String;
            data.StringValue = sValue;
            data.Length = sValue.Length;
            return json;
        }

        public Json JsonInt(int nValue) 
        {
            var json = new Json(0);
            var data = GetOrCreateJsonData(json);
            data.Type = JsonType.Integer;
            data.IntValue = nValue;
            return json;
        }

        public Json JsonFloat(float fValue) 
        {
            var json = new Json(0);
            var data = GetOrCreateJsonData(json);
            data.Type = JsonType.Float;
            data.FloatValue = fValue;
            return json;
        }

        public Json JsonBool(bool bValue) 
        {
            var json = new Json(0);
            var data = GetOrCreateJsonData(json);
            data.Type = JsonType.Bool;
            data.BoolValue = bValue;
            return json;
        }

        public string JsonGetString(Json jValue) => 
            _jsonData.GetValueOrDefault(jValue, new JsonData()).StringValue;

        public int JsonGetInt(Json jValue) => 
            _jsonData.GetValueOrDefault(jValue, new JsonData()).IntValue;

        public float JsonGetFloat(Json jValue) => 
            _jsonData.GetValueOrDefault(jValue, new JsonData()).FloatValue;

        public Json JsonObjectKeys(Json jObject) 
        {
            var data = _jsonData.GetValueOrDefault(jObject, new JsonData());
            var keysArray = JsonArray();
            var keysData = GetOrCreateJsonData(keysArray);
            keysData.Type = JsonType.Array;
            foreach (var key in data.ObjectProperties.Keys)
            {
                keysData.ArrayElements.Add(JsonString(key));
            }
            keysData.Length = data.ObjectProperties.Count;
            return keysArray;
        }

        public Json JsonObjectGet(Json jObject, string sKey) 
        {
            var data = _jsonData.GetValueOrDefault(jObject, new JsonData());
            return data.ObjectProperties.GetValueOrDefault(sKey, JsonNull("Key not found"));
        }

        public Json JsonObjectSet(Json jObject, string sKey, Json jValue) 
        {
            var data = GetOrCreateJsonData(jObject);
            data.Type = JsonType.Object;
            data.ObjectProperties[sKey] = jValue;
            data.Length = data.ObjectProperties.Count;
            return jObject;
        }

        public Json JsonObjectDel(Json jObject, string sKey) 
        {
            var data = GetOrCreateJsonData(jObject);
            data.ObjectProperties.Remove(sKey);
            data.Length = data.ObjectProperties.Count;
            return jObject;
        }

        public Json JsonArrayGet(Json jArray, int nIndex) 
        {
            var data = _jsonData.GetValueOrDefault(jArray, new JsonData());
            if (nIndex >= 0 && nIndex < data.ArrayElements.Count)
                return data.ArrayElements[nIndex];
            return JsonNull("Index out of range");
        }

        public Json JsonArraySet(Json jArray, int nIndex, Json jValue) 
        {
            var data = GetOrCreateJsonData(jArray);
            data.Type = JsonType.Array;
            while (data.ArrayElements.Count <= nIndex)
                data.ArrayElements.Add(JsonNull());
            data.ArrayElements[nIndex] = jValue;
            data.Length = data.ArrayElements.Count;
            return jArray;
        }

        public Json JsonArrayInsert(Json jArray, Json jValue, int nIndex = -1) 
        {
            var data = GetOrCreateJsonData(jArray);
            data.Type = JsonType.Array;
            if (nIndex == -1 || nIndex >= data.ArrayElements.Count)
                data.ArrayElements.Add(jValue);
            else
                data.ArrayElements.Insert(nIndex, jValue);
            data.Length = data.ArrayElements.Count;
            return jArray;
        }

        public Json JsonArrayDel(Json jArray, int nIndex) 
        {
            var data = GetOrCreateJsonData(jArray);
            if (nIndex >= 0 && nIndex < data.ArrayElements.Count)
            {
                data.ArrayElements.RemoveAt(nIndex);
                data.Length = data.ArrayElements.Count;
            }
            return jArray;
        }

        public Json ObjectToJson(uint oObject, bool bSaveObjectState = false) 
        {
            var json = JsonObject();
            var data = GetOrCreateJsonData(json);
            data.Type = JsonType.Object;
            // Mock implementation - would serialize object properties
            return json;
        }

        public uint JsonToObject(Json jObject, Location locLocation, uint oOwner = OBJECT_INVALID, bool bLoadObjectState = false) 
        {
            // Mock implementation - return a new object ID
            return (uint)(_jsonData.Count + 8000);
        }

        public Json JsonPointer(Json jData, string sPointer) 
        {
            // Mock implementation - simplified pointer resolution
            return JsonNull("Pointer not implemented");
        }

        public Json JsonPatch(Json jData, Json jPatch) 
        {
            // Mock implementation - return original data
            return jData;
        }

        public Json JsonDiff(Json jLHS, Json jRHS) 
        {
            // Mock implementation - return empty object
            return JsonObject();
        }

        public Json JsonMerge(Json jData, Json jMerge) 
        {
            // Mock implementation - return original data
            return jData;
        }

        public Json GetLocalJson(uint oObject, string sVarName) => 
            _localJsonVariables.GetValueOrDefault(oObject, new Dictionary<string, Json>())
                .GetValueOrDefault(sVarName, JsonNull());

        public void SetLocalJson(uint oObject, string sVarName, Json jValue) 
        {
            if (!_localJsonVariables.ContainsKey(oObject))
                _localJsonVariables[oObject] = new Dictionary<string, Json>();
            _localJsonVariables[oObject][sVarName] = jValue;
        }

        public void DeleteLocalJson(uint oObject, string sVarName) 
        {
            if (_localJsonVariables.ContainsKey(oObject))
                _localJsonVariables[oObject].Remove(sVarName);
        }

        public Json TemplateToJson(string sResRef, ResType nResType) 
        {
            // Mock implementation - return empty object
            return JsonObject();
        }

        public Json JsonArrayTransform(Json jArray, JsonArraySortType nTransform) 
        {
            // Mock implementation - return original array
            return jArray;
        }

        public Json JsonFind(Json jArray, Json jValue, int nStartIndex = 0, int nCount = -1) 
        {
            // Mock implementation - return -1 (not found)
            return JsonInt(-1);
        }

        public Json JsonArrayGetRange(Json jArray, int nBeginIndex, int nEndIndex) 
        {
            var newArray = JsonArray();
            var sourceData = _jsonData.GetValueOrDefault(jArray, new JsonData());
            var newData = GetOrCreateJsonData(newArray);
            newData.Type = JsonType.Array;
            
            for (int i = nBeginIndex; i < nEndIndex && i < sourceData.ArrayElements.Count; i++)
            {
                newData.ArrayElements.Add(sourceData.ArrayElements[i]);
            }
            newData.Length = newData.ArrayElements.Count;
            return newArray;
        }

        public Json JsonSetOp(Json jValue, JsonSetType nOp, Json jOther) 
        {
            // Mock implementation - return original value
            return jValue;
        }

        public Json RegExpMatch(string sString, string sPattern, int nFlags = 0) 
        {
            // Mock implementation - return empty array
            return JsonArray();
        }

        public Json RegExpIterate(string sString, string sPattern, int nFlags = 0) 
        {
            // Mock implementation - return empty array
            return JsonArray();
        }

        private JsonData GetOrCreateJsonData(Json json)
        {
            if (!_jsonData.ContainsKey(json))
                _jsonData[json] = new JsonData();
            return _jsonData[json];
        }

        // Additional JSON methods from INWScriptService
        public Json JsonFind(Json jHaystack, Json jNeedle, int nNth = 0, JsonFindType nConditional = JsonFindType.Equal) 
        {
            // Mock implementation - would need actual JSON search logic
            return new Json(0);
        }

        public Json RegExpMatch(string sRegExp, string sValue, RegularExpressionType nSyntaxFlags = RegularExpressionType.Ecmascript,
            RegularExpressionFormatType nMatchFlags = RegularExpressionFormatType.Default) 
        {
            // Mock implementation - would need actual regex matching
            return new Json(0);
        }

        public Json RegExpIterate(string sRegExp, string sValue, RegularExpressionType nSyntaxFlags = RegularExpressionType.Ecmascript,
            RegularExpressionFormatType nMatchFlags = RegularExpressionFormatType.Default) 
        {
            // Mock implementation - would need actual regex iteration
            return new Json(0);
        }

        public string RegExpReplace(string sRegExp, string sValue, string sReplacement,
            RegularExpressionType nSyntaxFlags = RegularExpressionType.Ecmascript,
            RegularExpressionFormatType nMatchFlags = RegularExpressionFormatType.Default) 
        {
            // Mock implementation - would need actual regex replacement
            return sValue;
        }

        // Helper methods for testing
    }
}
