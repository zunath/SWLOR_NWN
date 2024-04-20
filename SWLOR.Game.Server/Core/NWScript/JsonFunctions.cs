using SWLOR.Game.Server.Core.NWScript.Enum;

namespace SWLOR.Game.Server.Core.NWScript
{
    public partial class NWScript
    {
        /// <summary>
        /// Parse the given string as a valid json value, and returns the corresponding type.
        /// Returns a JSON_TYPE_NULL on error.
        /// Check JsonGetError() to see the parse error, if any.
        /// NB: The parsed string needs to be in game-local encoding, but the generated json structure
        ///     will contain UTF-8 data.
        /// </summary>
        public static Json JsonParse(string jValue, int nIndent = -1)
        {
            VM.StackPush(nIndent);
            VM.StackPush(jValue);
            VM.Call(968);

            return VM.StackPopStruct((int)EngineStructure.Json);
        }

        /// <summary>
        /// Dump the given json value into a string that can be read back in via JsonParse.
        /// nIndent describes the indentation level for pretty-printing; a value of -1 means no indentation and no linebreaks.
        /// Returns a string describing JSON_TYPE_NULL on error.
        /// NB: The dumped string is in game-local encoding, with all non-ascii characters escaped.
        /// </summary>
        public static string JsonDump(Json jValue, int nIndent = -1)
        {
            VM.StackPush(nIndent);
            VM.StackPush((int)EngineStructure.Json, jValue);
            VM.Call(969);

            return VM.StackPopString();
        }

        /// <summary>
        /// Describes the type of the given json value.
        /// Returns JSON_TYPE_NULL if the value is empty.
        /// </summary>
        public static JsonType JsonGetType(Json jValue)
        {
            VM.StackPush((int)EngineStructure.Json, jValue);
            VM.Call(970);

            return (JsonType) VM.StackPopInt();
        }

        /// <summary>
        /// Returns the length of the given json type.
        /// For objects, returns the number of top-level keys present.
        /// For arrays, returns the number of elements.
        /// Null types are of size 0.
        /// All other types return 1.
        /// </summary>
        public static int JsonGetLength(Json jValue)
        {
            VM.StackPush((int)EngineStructure.Json, jValue);
            VM.Call(971);

            return VM.StackPopInt();
        }

        /// <summary>
        /// Returns the error message if the value has errored out.
        /// Currently only describes parse errors.
        /// </summary>
        public static string JsonGetError(Json jValue)
        {
            VM.StackPush((int)EngineStructure.Json, jValue);
            VM.Call(972);

            return VM.StackPopString();
        }

        /// <summary>
        /// Create a NULL json value, seeded with a optional error message for JsonGetError().
        /// </summary>
        public static Json JsonNull(string sError = "")
        {
            VM.StackPush(sError);
            VM.Call(973);

            return VM.StackPopStruct((int)EngineStructure.Json);
        }

        /// <summary>
        /// Create a empty json object.
        /// </summary>
        public static Json JsonObject()
        {
            VM.Call(974);
            return VM.StackPopStruct((int)EngineStructure.Json);
        }

        /// <summary>
        /// Create a empty json array.
        /// </summary>
        public static Json JsonArray()
        {
            VM.Call(975);
            return VM.StackPopStruct((int)EngineStructure.Json);
        }

        /// <summary>
        /// Create a json string value.
        /// NB: Strings are encoded to UTF-8 from the game-local charset.
        /// </summary>
        public static Json JsonString(string sValue)
        {
            VM.StackPush(sValue);
            VM.Call(976);

            return VM.StackPopStruct((int)EngineStructure.Json);
        }

        /// <summary>
        /// Create a json integer value.
        /// </summary>
        public static Json JsonInt(int nValue)
        {
            VM.StackPush(nValue);
            VM.Call(977);

            return VM.StackPopStruct((int)EngineStructure.Json);
        }

        /// <summary>
        /// Create a json floating point value.
        /// </summary>
        public static Json JsonFloat(float fValue)
        {
            VM.StackPush(fValue);
            VM.Call(978);

            return VM.StackPopStruct((int)EngineStructure.Json);
        }

        /// <summary>
        /// Create a json bool valye.
        /// </summary>
        public static Json JsonBool(bool bValue)
        {
            VM.StackPush(bValue ? 1 : 0);
            VM.Call(979);

            return VM.StackPopStruct((int)EngineStructure.Json);
        }

        /// <summary>
        /// Returns a string representation of the json value.
        /// Returns "" if the value cannot be represented as a string, or is empty.
        /// NB: Strings are decoded from UTF-8 to the game-local charset.
        /// </summary>
        public static string JsonGetString(Json jValue)
        {
            VM.StackPush((int)EngineStructure.Json, jValue);
            VM.Call(980);

            return VM.StackPopString();
        }

        /// <summary>
        /// Returns a int representation of the json value, casting where possible.
        /// Returns 0 if the value cannot be represented as a float.
        /// Use this to parse json bool types.
        /// NB: This will narrow down to signed 32 bit, as that is what NWScript int is.
        ///     If you are trying to read a 64 bit or unsigned integer, you will lose data.
        ///     You will not lose data if you keep the value as a json element (via Object/ArrayGet).
        /// </summary>
        public static int JsonGetInt(Json jValue)
        {
            VM.StackPush((int)EngineStructure.Json, jValue);
            VM.Call(981);

            return VM.StackPopInt();
        }

        /// <summary>
        /// Returns a float representation of the json value, casting where possible.
        /// Returns 0.0 if the value cannot be represented as a float.
        /// NB: This will narrow doubles down to float.
        ///     If you are trying to read a double, you will lose data.
        ///     You will not lose data if you keep the value as a json element (via Object/ArrayGet).
        /// </summary>
        public static float JsonGetFloat(Json jValue)
        {
            VM.StackPush((int)EngineStructure.Json, jValue);
            VM.Call(982);

            return VM.StackPopInt();
        }

        /// <summary>
        /// Returns a json array containing all keys of jObject.
        /// Returns a empty array if the object is empty or not a json object, with GetJsonError() filled in.
        /// </summary>
        public static Json JsonObjectKeys(Json jObject)
        {
            VM.StackPush((int)EngineStructure.Json, jObject);
            VM.Call(983);

            return VM.StackPopStruct((int) EngineStructure.Json);
        }

        /// <summary>
        /// Returns the key value of sKey on the object jObect.
        /// Returns a null json value if jObject is not a object or sKey does not exist on the object, with GetJsonError() filled in.
        /// </summary>
        public static Json JsonObjectGet(Json jObject, string sKey)
        {
            VM.StackPush(sKey);
            VM.StackPush((int)EngineStructure.Json, jObject);
            VM.Call(984);

            return VM.StackPopStruct((int)EngineStructure.Json);
        }

        /// <summary>
        /// Returns a modified copy of jObject with the key at sKey set to jValue.
        /// Returns a json null value if jObject is not a object, with GetJsonError() filled in.
        /// </summary>
        public static Json JsonObjectSet(Json jObject, string sKey, Json jValue)
        {
            VM.StackPush((int)EngineStructure.Json, jValue);
            VM.StackPush(sKey);
            VM.StackPush((int)EngineStructure.Json, jObject);
            VM.Call(985);

            return VM.StackPopStruct((int)EngineStructure.Json);
        }

        /// <summary>
        /// Returns a modified copy of jObject with the key at sKey deleted.
        /// Returns a json null value if jObject is not a object, with GetJsonError() filled in.
        /// </summary>
        public static Json JsonObjectDel(Json jObject, string sKey)
        {
            VM.StackPush(sKey);
            VM.StackPush((int)EngineStructure.Json, jObject);
            VM.Call(986);

            return VM.StackPopStruct((int)EngineStructure.Json);
        }

        /// <summary>
        /// Gets the json object at jArray index position nIndex.
        /// Returns a json null value if the index is out of bounds, with GetJsonError() filled in.
        /// </summary>
        public static Json JsonArrayGet(Json jArray, int nIndex)
        {
            VM.StackPush(nIndex);
            VM.StackPush((int)EngineStructure.Json, jArray);
            VM.Call(987);

            return VM.StackPopStruct((int)EngineStructure.Json);
        }

        /// <summary>
        /// Returns a modified copy of jArray with position nIndex set to jValue.
        /// Returns a json null value if jArray is not actually an array, with GetJsonError() filled in.
        /// Returns a json null value if nIndex is out of bounds, with GetJsonError() filled in.
        /// </summary>
        public static Json JsonArraySet(Json jArray, int nIndex, Json jValue)
        {
            VM.StackPush((int)EngineStructure.Json, jValue);
            VM.StackPush(nIndex);
            VM.StackPush((int)EngineStructure.Json, jArray);
            VM.Call(988);

            return VM.StackPopStruct((int)EngineStructure.Json);
        }

        /// <summary>
        /// Returns a modified copy of jArray with jValue inserted at position nIndex.
        /// All succeeding objects in the array will move by one.
        /// By default (-1), inserts objects at the end of the array ("push").
        /// nIndex = 0 inserts at the beginning of the array.
        /// Returns a json null value if jArray is not actually an array, with GetJsonError() filled in.
        /// Returns a json null value if nIndex is not 0 or -1 and out of bounds, with GetJsonError() filled in.
        /// </summary>
        public static Json JsonArrayInsert(Json jArray, Json jValue, int nIndex = -1)
        {
            VM.StackPush(nIndex);
            VM.StackPush((int)EngineStructure.Json, jValue);
            VM.StackPush((int)EngineStructure.Json, jArray);
            VM.Call(989);

            return VM.StackPopStruct((int)EngineStructure.Json);
        }

        /// <summary>
        /// Returns a modified copy of jArray with the element at position nIndex removed,
        /// and the array resized by one.
        /// Returns a json null value if jArray is not actually an array, with GetJsonError() filled in.
        /// Returns a json null value if nIndex is out of bounds, with GetJsonError() filled in.
        /// </summary>
        public static Json JsonArrayDel(Json jArray, int nIndex)
        {
            VM.StackPush(nIndex);
            VM.StackPush((int)EngineStructure.Json, jArray);
            VM.Call(990);

            return VM.StackPopStruct((int)EngineStructure.Json);
        }

        /// <summary>
        /// Transforms the given object into a json structure.
        /// The json format is compatible with what https://github.com/niv/neverwinter.nim@1.4.3+ emits.
        /// Returns the null json type on errors, or if oObject is not serializable, with GetJsonError() filled in.
        /// Supported object types: creature, item, trigger, placeable, door, waypoint, encounter, store, area (combined format)
        /// If bSaveObjectState is TRUE, local vars, effects, action queue, and transition info (triggers, doors) are saved out
        /// (except for Combined Area Format, which always has object state saved out).
        /// </summary>
        public static Json ObjectToJson(uint oObject, bool bSaveObjectState = false)
        {
            VM.StackPush(bSaveObjectState ? 1 : 0);
            VM.StackPush(oObject);
            VM.Call(991);

            return VM.StackPopStruct((int)EngineStructure.Json);
        }

        /// <summary>
        /// Deserializes the game object described in jObject.
        /// Returns OBJECT_INVALID on errors.
        /// Supported object types: creature, item, trigger, placeable, door, waypoint, encounter, store, area (combined format)
        /// For areas, locLocation is ignored.
        /// If bLoadObjectState is TRUE, local vars, effects, action queue, and transition info (triggers, doors) are read in.
        /// </summary>
        public static uint JsonToObject(Json jObject, Location locLocation, uint oOwner = OBJECT_INVALID, bool bLoadObjectState = false)
        {
            VM.StackPush(bLoadObjectState ? 1 : 0);
            VM.StackPush(oOwner);
            VM.StackPush((int)EngineStructure.Location, locLocation);
            VM.StackPush((int)EngineStructure.Json, jObject);
            VM.Call(992);

            return VM.StackPopObject();
        }

        /// <summary>
        /// Returns the element at the given JSON pointer value.
        /// See https://datatracker.ietf.org/doc/html/rfc6901 for details.
        /// Returns a json null value on error, with GetJsonError() filled in.
        /// </summary>
        public static Json JsonPointer(Json jData, string sPointer)
        {
            VM.StackPush(sPointer);
            VM.StackPush((int)EngineStructure.Json, jData);
            VM.Call(993);

            return VM.StackPopStruct((int) EngineStructure.Json);
        }

        /// <summary>
        /// Return a modified copy of jData with jValue inserted at the path described by sPointer.
        /// See https://datatracker.ietf.org/doc/html/rfc6901 for details.
        /// Returns a json null value on error, with GetJsonError() filled in.
        /// jPatch is an array of patch elements, each containing a op, a path, and a value field. Example:
        /// [
        ///   { "op": "replace", "path": "/baz", "value": "boo" },
        ///   { "op": "add", "path": "/hello", "value": ["world"] },
        ///   { "op": "remove", "path": "/foo"}
        /// ]
        /// Valid operations are: add, remove, replace, move, copy, test
        /// </summary>
        public static Json JsonPatch(Json jData, Json jPatch)
        {
            VM.StackPush((int)EngineStructure.Json, jPatch);
            VM.StackPush((int)EngineStructure.Json, jData);
            VM.Call(994);

            return VM.StackPopStruct((int)EngineStructure.Json);
        }

        /// <summary>
        /// Returns the diff (described as a json structure you can pass into JsonPatch) between the two objects.
        /// Returns a json null value on error, with GetJsonError() filled in.
        /// </summary>
        public static Json JsonDiff(Json jLHS, Json jRHS)
        {
            VM.StackPush((int)EngineStructure.Json, jRHS);
            VM.StackPush((int)EngineStructure.Json, jLHS);
            VM.Call(995);

            return VM.StackPopStruct((int)EngineStructure.Json);
        }

        /// <summary>
        /// Returns a modified copy of jData with jMerge merged into it. This is an alternative to
        /// JsonPatch/JsonDiff, with a syntax more closely resembling the final object.
        /// See https://datatracker.ietf.org/doc/html/rfc7386 for details.
        /// Returns a json null value on error, with GetJsonError() filled in.
        /// </summary>
        public static Json JsonMerge(Json jData, Json jMerge)
        {
            VM.StackPush((int)EngineStructure.Json, jMerge);
            VM.StackPush((int)EngineStructure.Json, jData);
            VM.Call(996);

            return VM.StackPopStruct((int)EngineStructure.Json);
        }

        /// <summary>
        /// Get oObject's local json variable sVarName
        /// * Return value on error: json null type
        /// </summary>
        public static Json GetLocalJson(uint oObject, string sVarName)
        {
            VM.StackPush(sVarName);
            VM.StackPush(oObject);
            VM.Call(997);

            return VM.StackPopStruct((int)EngineStructure.Json);
        }

        /// <summary>
        /// Set oObject's local json variable sVarName to jValue
        /// </summary>
        public static void SetLocalJson(uint oObject, string sVarName, Json jValue)
        {
            VM.StackPush((int)EngineStructure.Json, jValue);
            VM.StackPush(sVarName);
            VM.StackPush(oObject);
            VM.Call(998);
        }

        /// <summary>
        /// Delete oObject's local json variable sVarName
        /// </summary>
        public static void DeleteLocalJson(uint oObject, string sVarName)
        {
            VM.StackPush(sVarName);
            VM.StackPush(oObject);
            VM.Call(999);
        }

        /// <summary>
        /// Deserializes the given resref/template into a JSON structure.
        /// Supported GFF resource types:
        /// * RESTYPE_CAF (and RESTYPE_ARE, RESTYPE_GIT, RESTYPE_GIC)
        /// * RESTYPE_UTC
        /// * RESTYPE_UTI
        /// * RESTYPE_UTT
        /// * RESTYPE_UTP
        /// * RESTYPE_UTD
        /// * RESTYPE_UTW
        /// * RESTYPE_UTE
        /// * RESTYPE_UTM
        /// * RESTYPE_DLG
        /// * RESTYPE_UTS
        /// * RESTYPE_IFO
        /// * RESTYPE_FAC
        /// * RESTYPE_ITP
        /// * RESTYPE_GUI
        /// * RESTYPE_GFF
        /// Returns a valid gff-type json structure, or a null value with GetJsonError() set.
        /// </summary>
        public static Json TemplateToJson(string sResRef, ResType nResType)
        {
            VM.StackPush((int) nResType);
            VM.StackPush(sResRef);
            VM.Call(1007);

            return VM.StackPopStruct((int)EngineStructure.Json);
        }

        /// <summary>
        /// Returns a modified copy of jArray with the value order changed according to nTransform:
        /// JSON_ARRAY_SORT_ASCENDING, JSON_ARRAY_SORT_DESCENDING
        ///    Sorting is dependent on the type and follows json standards (.e.g. 99 < "100").
        /// JSON_ARRAY_SHUFFLE
        ///   Randomises the order of elements.
        /// JSON_ARRAY_REVERSE
        ///   Reverses the array.
        /// JSON_ARRAY_UNIQUE
        ///   Returns a modified copy of jArray with duplicate values removed.
        ///   Coercable but different types are not considered equal (e.g. 99 != "99"); int/float equivalence however applies: 4.0 == 4.
        ///   Order is preserved.
        /// JSON_ARRAY_COALESCE
        ///   Returns the first non-null entry. Empty-ish values (e.g. "", 0) are not considered null, only the json scalar type.
        /// </summary>

        public static Json JsonArrayTransform(Json jArray, JsonArraySort nTransform)
        {
            VM.StackPush((int)nTransform);
            VM.StackPush((int)EngineStructure.Json, jArray);
            VM.Call(1030);

            return VM.StackPopStruct((int)EngineStructure.Json);
        }

        /// <summary>
        /// Returns the nth-matching index or key of jNeedle in jHaystack.
        /// Supported haystacks: object, array
        /// Ordering behaviour for objects is unspecified.
        /// Return null when not found or on any error.
        /// </summary>
        public static Json JsonFind(
            Json jHaystack, 
            Json jNeedle, 
            int nNth = 0,
            JsonFind nConditional = Enum.JsonFind.Equal)
        {
            VM.StackPush((int)nConditional);
            VM.StackPush(nNth);
            VM.StackPush((int)EngineStructure.Json, jNeedle);
            VM.StackPush((int)EngineStructure.Json, jHaystack);
            VM.Call(1031);

            return VM.StackPopStruct((int)EngineStructure.Json);
        }

        /// <summary>
        /// Returns a copy of the range (nBeginIndex, nEndIndex) inclusive of jArray.
        /// Negative nEndIndex values count from the other end.
        /// Out-of-bound values are clamped to the array range.
        /// Examples:
        ///  json a = JsonParse("[0, 1, 2, 3, 4]");
        ///  JsonArrayGetRange(a, 0, 1)    // => [0, 1]
        ///  JsonArrayGetRange(a, 1, -1)   // => [1, 2, 3, 4]
        ///  JsonArrayGetRange(a, 0, 4)    // => [0, 1, 2, 3, 4]
        ///  JsonArrayGetRange(a, 0, 999)  // => [0, 1, 2, 3, 4]
        ///  JsonArrayGetRange(a, 1, 0)    // => []
        ///  JsonArrayGetRange(a, 1, 1)    // => [1]
        /// Returns a null type on error, including type mismatches.
        /// </summary>
        public static Json JsonArrayGetRange(Json jArray, int nBeginIndex, int nEndIndex)
        {
            VM.StackPush((int)nEndIndex);
            VM.StackPush(nBeginIndex);
            VM.StackPush((int)EngineStructure.Json, jArray);
            VM.Call(1032);

            return VM.StackPopStruct((int)EngineStructure.Json);
        }

        /// <summary>
        /// Returns the result of a set operation on two arrays.
        /// Operations:
        /// JSON_SET_SUBSET (v <= o):
        ///   Returns true if every element in jValue is also in jOther.
        /// JSON_SET_UNION (v | o):
        ///   Returns a new array containing values from both sides.
        /// JSON_SET_INTERSECT (v & o):
        ///   Returns a new array containing only values common to both sides.
        /// JSON_SET_DIFFERENCE (v - o):
        ///   Returns a new array containing only values not in jOther.
        /// JSON_SET_SYMMETRIC_DIFFERENCE (v ^ o):
        ///   Returns a new array containing all elements present in either array, but not both.
        /// </summary>
        public static Json JsonSetOp(Json jValue, JsonSet nOp, Json jOther)
        {
            VM.StackPush((int)EngineStructure.Json, jOther);
            VM.StackPush((int)nOp);
            VM.StackPush((int)EngineStructure.Json, jValue);
            VM.Call(1033);

            return VM.StackPopStruct((int)EngineStructure.Json);
        }

        /// <summary>
        /// Applies sRegExp on sValue, returning an array containing all matching groups.
        /// * The regexp is not bounded by default (so /t/ will match "test").
        /// * A matching result with always return a JSON_ARRAY with the full match as the first element.
        /// * All matching groups will be returned as additional elements, depth-first.
        /// * A non-matching result will return a empty JSON_ARRAY.
        /// * If there was an error, the function will return JSON_NULL, with a error string filled in.
        /// * nSyntaxFlags is a mask of REGEXP_*
        /// * nMatchFlags is a mask of REGEXP_MATCH_* and REGEXP_FORMAT_*.
        /// Examples:
        /// * RegExpMatch("[", "test value")             -> null (error: "The expression contained mismatched [ and ].")
        /// * RegExpMatch("nothing", "test value")       -> []
        /// * RegExpMatch("^test", "test value")         -> ["test"]
        /// * RegExpMatch("^(test) (.+)$", "test value") -> ["test value", "test", "value"]
        /// </summary>
        public static Json RegExpMatch(
            string sRegExp, 
            string sValue, 
            RegularExpressionType nSyntaxFlags = RegularExpressionType.Ecmascript, 
            RegularExpressionFormatType nMatchFlags = RegularExpressionFormatType.Default)
        {
            VM.StackPush((int)nMatchFlags);
            VM.StackPush((int)nSyntaxFlags);
            VM.StackPush(sValue);
            VM.StackPush(sRegExp);
            VM.Call(1068);

            return VM.StackPopStruct((int)EngineStructure.Json);
        }

        /// <summary>
        ///  Iterates sValue with sRegExp.
        /// * Returns an array of arrays; where each sub-array contains first the full match and then all matched groups.
        /// * Returns empty JSON_ARRAY if no matches are found.
        /// * If there was an error, the function will return JSON_NULL, with a error string filled in.
        /// * nSyntaxFlags is a mask of REGEXP_*
        /// * nMatchFlags is a mask of REGEXP_MATCH_* and REGEXP_FORMAT_*.
        /// Example: RegExpIterate("(\\d)(\\S+)", "1i 2am 3 4asentence"); -> [["1i", "1", "i"], ["2am", "2", "am"], ["4sentence", "4", "sentence"]]
        /// </summary>
        public static Json RegExpIterate(
            string sRegExp, 
            string sValue,
            RegularExpressionType nSyntaxFlags = RegularExpressionType.Ecmascript,
            RegularExpressionFormatType nMatchFlags = RegularExpressionFormatType.Default)
        {
            VM.StackPush((int)nMatchFlags);
            VM.StackPush((int)nSyntaxFlags);
            VM.StackPush(sValue);
            VM.StackPush(sRegExp);
            VM.Call(1069);

            return VM.StackPopStruct((int)EngineStructure.Json);
        }
    }
}
