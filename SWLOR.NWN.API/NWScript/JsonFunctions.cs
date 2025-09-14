using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.NWN.API.NWScript
{
    public partial class NWScript
    {
        /// <summary>
        /// Parses the given string as a valid JSON value, and returns the corresponding type.
        /// Returns a JSON_TYPE_NULL on error.
        /// Check JsonGetError() to see the parse error, if any.
        /// NB: The parsed string needs to be in game-local encoding, but the generated JSON structure
        /// will contain UTF-8 data.
        /// </summary>
        /// <param name="jValue">The string to parse as JSON</param>
        /// <returns>The parsed JSON value, or JSON_TYPE_NULL on error</returns>
        public static Json JsonParse(string jValue)
        {
            return global::NWN.Core.NWScript.JsonParse(jValue);
        }

        /// <summary>
        /// Dumps the given JSON value into a string that can be read back in via JsonParse.
        /// nIndent describes the indentation level for pretty-printing; a value of -1 means no indentation and no linebreaks.
        /// Returns a string describing JSON_TYPE_NULL on error.
        /// NB: The dumped string is in game-local encoding, with all non-ASCII characters escaped.
        /// </summary>
        /// <param name="jValue">The JSON value to dump</param>
        /// <param name="nIndent">The indentation level for pretty-printing (defaults to -1)</param>
        /// <returns>The JSON string, or error description on error</returns>
        public static string JsonDump(Json jValue, int nIndent = -1)
        {
            return global::NWN.Core.NWScript.JsonDump(jValue, nIndent);
        }

        /// <summary>
        /// Describes the type of the given JSON value.
        /// Returns JSON_TYPE_NULL if the value is empty.
        /// </summary>
        /// <param name="jValue">The JSON value to get the type of</param>
        /// <returns>The JSON type, or JSON_TYPE_NULL if the value is empty</returns>
        public static JsonType JsonGetType(Json jValue)
        {
            return (JsonType)global::NWN.Core.NWScript.JsonGetType(jValue);
        }

        /// <summary>
        /// Returns the length of the given JSON type.
        /// For objects, returns the number of top-level keys present.
        /// For arrays, returns the number of elements.
        /// Null types are of size 0.
        /// All other types return 1.
        /// </summary>
        /// <param name="jValue">The JSON value to get the length of</param>
        /// <returns>The length of the JSON value</returns>
        public static int JsonGetLength(Json jValue)
        {
            return global::NWN.Core.NWScript.JsonGetLength(jValue);
        }

        /// <summary>
        /// Returns the error message if the value has errored out.
        /// Currently only describes parse errors.
        /// </summary>
        /// <param name="jValue">The JSON value to get the error for</param>
        /// <returns>The error message, or empty string if no error</returns>
        public static string JsonGetError(Json jValue)
        {
            return global::NWN.Core.NWScript.JsonGetError(jValue);
        }

        /// <summary>
        /// Creates a NULL JSON value, seeded with an optional error message for JsonGetError().
        /// </summary>
        /// <param name="sError">Optional error message (defaults to empty string)</param>
        /// <returns>A NULL JSON value</returns>
        public static Json JsonNull(string sError = "")
        {
            return global::NWN.Core.NWScript.JsonNull(sError);
        }

        /// <summary>
        /// Creates an empty JSON object.
        /// </summary>
        /// <returns>An empty JSON object</returns>
        public static Json JsonObject()
        {
            return global::NWN.Core.NWScript.JsonObject();
        }

        /// <summary>
        /// Creates an empty JSON array.
        /// </summary>
        /// <returns>An empty JSON array</returns>
        public static Json JsonArray()
        {
            return global::NWN.Core.NWScript.JsonArray();
        }

        /// <summary>
        /// Creates a JSON string value.
        /// NB: Strings are encoded to UTF-8 from the game-local charset.
        /// </summary>
        /// <param name="sValue">The string value to create</param>
        /// <returns>A JSON string value</returns>
        public static Json JsonString(string sValue)
        {
            return global::NWN.Core.NWScript.JsonString(sValue);
        }

        /// <summary>
        /// Creates a JSON integer value.
        /// </summary>
        /// <param name="nValue">The integer value to create</param>
        /// <returns>A JSON integer value</returns>
        public static Json JsonInt(int nValue)
        {
            return global::NWN.Core.NWScript.JsonInt(nValue);
        }

        /// <summary>
        /// Creates a JSON floating point value.
        /// </summary>
        /// <param name="fValue">The float value to create</param>
        /// <returns>A JSON float value</returns>
        public static Json JsonFloat(float fValue)
        {
            return global::NWN.Core.NWScript.JsonFloat(fValue);
        }

        /// <summary>
        /// Creates a JSON boolean value.
        /// </summary>
        /// <param name="bValue">The boolean value to create</param>
        /// <returns>A JSON boolean value</returns>
        public static Json JsonBool(bool bValue)
        {
            return global::NWN.Core.NWScript.JsonBool(bValue ? 1 : 0);
        }

        /// <summary>
        /// Returns a string representation of the JSON value.
        /// Returns empty string if the value cannot be represented as a string, or is empty.
        /// NB: Strings are decoded from UTF-8 to the game-local charset.
        /// </summary>
        /// <param name="jValue">The JSON value to get the string representation of</param>
        /// <returns>The string representation, or empty string if not representable</returns>
        public static string JsonGetString(Json jValue)
        {
            return global::NWN.Core.NWScript.JsonGetString(jValue);
        }

        /// <summary>
        /// Returns an integer representation of the JSON value, casting where possible.
        /// Returns 0 if the value cannot be represented as an integer.
        /// Use this to parse JSON boolean types.
        /// NB: This will narrow down to signed 32 bit, as that is what NWScript int is.
        /// If you are trying to read a 64 bit or unsigned integer, you will lose data.
        /// You will not lose data if you keep the value as a JSON element (via Object/ArrayGet).
        /// </summary>
        /// <param name="jValue">The JSON value to get the integer representation of</param>
        /// <returns>The integer representation, or 0 if not representable</returns>
        public static int JsonGetInt(Json jValue)
        {
            return global::NWN.Core.NWScript.JsonGetInt(jValue);
        }

        /// <summary>
        /// Returns a float representation of the JSON value, casting where possible.
        /// Returns 0.0 if the value cannot be represented as a float.
        /// NB: This will narrow doubles down to float.
        /// If you are trying to read a double, you will lose data.
        /// You will not lose data if you keep the value as a JSON element (via Object/ArrayGet).
        /// </summary>
        /// <param name="jValue">The JSON value to get the float representation of</param>
        /// <returns>The float representation, or 0.0 if not representable</returns>
        public static float JsonGetFloat(Json jValue)
        {
            return global::NWN.Core.NWScript.JsonGetFloat(jValue);
        }

        /// <summary>
        /// Returns a JSON array containing all keys of the object.
        /// Returns an empty array if the object is empty or not a JSON object, with GetJsonError() filled in.
        /// </summary>
        /// <param name="jObject">The JSON object to get keys from</param>
        /// <returns>A JSON array of keys, or empty array on error</returns>
        public static Json JsonObjectKeys(Json jObject)
        {
            return global::NWN.Core.NWScript.JsonObjectKeys(jObject);
        }

        /// <summary>
        /// Returns the key value of sKey on the object.
        /// Returns a null JSON value if jObject is not an object or sKey does not exist on the object, with GetJsonError() filled in.
        /// </summary>
        /// <param name="jObject">The JSON object to get the key from</param>
        /// <param name="sKey">The key to retrieve</param>
        /// <returns>The key value, or null JSON value on error</returns>
        public static Json JsonObjectGet(Json jObject, string sKey)
        {
            return global::NWN.Core.NWScript.JsonObjectGet(jObject, sKey);
        }

        /// <summary>
        /// Returns a modified copy of the object with the key at sKey set to jValue.
        /// Returns a JSON null value if jObject is not an object, with GetJsonError() filled in.
        /// </summary>
        /// <param name="jObject">The JSON object to modify</param>
        /// <param name="sKey">The key to set</param>
        /// <param name="jValue">The value to set</param>
        /// <returns>A modified copy of the object, or null JSON value on error</returns>
        public static Json JsonObjectSet(Json jObject, string sKey, Json jValue)
        {
            return global::NWN.Core.NWScript.JsonObjectSet(jObject, sKey, jValue);
        }

        /// <summary>
        /// Returns a modified copy of the object with the key at sKey deleted.
        /// Returns a JSON null value if jObject is not an object, with GetJsonError() filled in.
        /// </summary>
        /// <param name="jObject">The JSON object to modify</param>
        /// <param name="sKey">The key to delete</param>
        /// <returns>A modified copy of the object, or null JSON value on error</returns>
        public static Json JsonObjectDel(Json jObject, string sKey)
        {
            return global::NWN.Core.NWScript.JsonObjectDel(jObject, sKey);
        }

        /// <summary>
        /// Gets the JSON object at the array index position.
        /// Returns a JSON null value if the index is out of bounds, with GetJsonError() filled in.
        /// </summary>
        /// <param name="jArray">The JSON array to get from</param>
        /// <param name="nIndex">The index position</param>
        /// <returns>The JSON object at the index, or null JSON value on error</returns>
        public static Json JsonArrayGet(Json jArray, int nIndex)
        {
            return global::NWN.Core.NWScript.JsonArrayGet(jArray, nIndex);
        }

        /// <summary>
        /// Returns a modified copy of the array with position nIndex set to jValue.
        /// Returns a JSON null value if jArray is not actually an array, with GetJsonError() filled in.
        /// Returns a JSON null value if nIndex is out of bounds, with GetJsonError() filled in.
        /// </summary>
        /// <param name="jArray">The JSON array to modify</param>
        /// <param name="nIndex">The index position to set</param>
        /// <param name="jValue">The value to set</param>
        /// <returns>A modified copy of the array, or null JSON value on error</returns>
        public static Json JsonArraySet(Json jArray, int nIndex, Json jValue)
        {
            return global::NWN.Core.NWScript.JsonArraySet(jArray, nIndex, jValue);
        }

        /// <summary>
        /// Returns a modified copy of the array with jValue inserted at position nIndex.
        /// All succeeding objects in the array will move by one.
        /// By default (-1), inserts objects at the end of the array ("push").
        /// nIndex = 0 inserts at the beginning of the array.
        /// Returns a JSON null value if jArray is not actually an array, with GetJsonError() filled in.
        /// Returns a JSON null value if nIndex is not 0 or -1 and out of bounds, with GetJsonError() filled in.
        /// </summary>
        /// <param name="jArray">The JSON array to modify</param>
        /// <param name="jValue">The value to insert</param>
        /// <param name="nIndex">The index position to insert at (defaults to -1)</param>
        /// <returns>A modified copy of the array, or null JSON value on error</returns>
        public static Json JsonArrayInsert(Json jArray, Json jValue, int nIndex = -1)
        {
            return global::NWN.Core.NWScript.JsonArrayInsert(jArray, jValue, nIndex);
        }

        /// <summary>
        /// Returns a modified copy of the array with the element at position nIndex removed,
        /// and the array resized by one.
        /// Returns a JSON null value if jArray is not actually an array, with GetJsonError() filled in.
        /// Returns a JSON null value if nIndex is out of bounds, with GetJsonError() filled in.
        /// </summary>
        /// <param name="jArray">The JSON array to modify</param>
        /// <param name="nIndex">The index position to remove</param>
        /// <returns>A modified copy of the array, or null JSON value on error</returns>
        public static Json JsonArrayDel(Json jArray, int nIndex)
        {
            return global::NWN.Core.NWScript.JsonArrayDel(jArray, nIndex);
        }

        /// <summary>
        /// Transforms the given object into a JSON structure.
        /// The JSON format is compatible with what https://github.com/niv/neverwinter.nim@1.4.3+ emits.
        /// Returns the null JSON type on errors, or if oObject is not serializable, with GetJsonError() filled in.
        /// Supported object types: creature, item, trigger, placeable, door, waypoint, encounter, store, area (combined format)
        /// If bSaveObjectState is TRUE, local vars, effects, action queue, and transition info (triggers, doors) are saved out
        /// (except for Combined Area Format, which always has object state saved out).
        /// </summary>
        /// <param name="oObject">The object to transform</param>
        /// <param name="bSaveObjectState">Whether to save object state (defaults to false)</param>
        /// <returns>The JSON structure, or null JSON type on error</returns>
        public static Json ObjectToJson(uint oObject, bool bSaveObjectState = false)
        {
            return global::NWN.Core.NWScript.ObjectToJson(oObject, bSaveObjectState ? 1 : 0);
        }

        /// <summary>
        /// Deserializes the game object described in the JSON object.
        /// Returns OBJECT_INVALID on errors.
        /// Supported object types: creature, item, trigger, placeable, door, waypoint, encounter, store, area (combined format)
        /// For areas, locLocation is ignored.
        /// If bLoadObjectState is TRUE, local vars, effects, action queue, and transition info (triggers, doors) are read in.
        /// </summary>
        /// <param name="jObject">The JSON object describing the game object</param>
        /// <param name="locLocation">The location to create the object at</param>
        /// <param name="oOwner">The owner of the object (defaults to OBJECT_INVALID)</param>
        /// <param name="bLoadObjectState">Whether to load object state (defaults to false)</param>
        /// <returns>The created object, or OBJECT_INVALID on error</returns>
        public static uint JsonToObject(Json jObject, Location locLocation, uint oOwner = OBJECT_INVALID, bool bLoadObjectState = false)
        {
            return global::NWN.Core.NWScript.JsonToObject(jObject, locLocation, oOwner, bLoadObjectState ? 1 : 0);
        }

        /// <summary>
        /// Returns the element at the given JSON pointer value.
        /// See https://datatracker.ietf.org/doc/html/rfc6901 for details.
        /// Returns a JSON null value on error, with GetJsonError() filled in.
        /// </summary>
        /// <param name="jData">The JSON data to search</param>
        /// <param name="sPointer">The JSON pointer path</param>
        /// <returns>The element at the pointer, or null JSON value on error</returns>
        public static Json JsonPointer(Json jData, string sPointer)
        {
            return global::NWN.Core.NWScript.JsonPointer(jData, sPointer);
        }

        /// <summary>
        /// Returns a modified copy of jData with jValue inserted at the path described by sPointer.
        /// See https://datatracker.ietf.org/doc/html/rfc6901 for details.
        /// Returns a JSON null value on error, with GetJsonError() filled in.
        /// jPatch is an array of patch elements, each containing an op, a path, and a value field. Example:
        /// [
        ///   { "op": "replace", "path": "/baz", "value": "boo" },
        ///   { "op": "add", "path": "/hello", "value": ["world"] },
        ///   { "op": "remove", "path": "/foo"}
        /// ]
        /// Valid operations are: add, remove, replace, move, copy, test
        /// </summary>
        /// <param name="jData">The JSON data to patch</param>
        /// <param name="jPatch">The patch array</param>
        /// <returns>A modified copy of the data, or null JSON value on error</returns>
        public static Json JsonPatch(Json jData, Json jPatch)
        {
            return global::NWN.Core.NWScript.JsonPatch(jData, jPatch);
        }

        /// <summary>
        /// Returns the diff (described as a JSON structure you can pass into JsonPatch) between the two objects.
        /// Returns a JSON null value on error, with GetJsonError() filled in.
        /// </summary>
        /// <param name="jLHS">The left-hand side JSON object</param>
        /// <param name="jRHS">The right-hand side JSON object</param>
        /// <returns>The diff as a JSON structure, or null JSON value on error</returns>
        public static Json JsonDiff(Json jLHS, Json jRHS)
        {
            return global::NWN.Core.NWScript.JsonDiff(jLHS, jRHS);
        }

        /// <summary>
        /// Returns a modified copy of jData with jMerge merged into it. This is an alternative to
        /// JsonPatch/JsonDiff, with a syntax more closely resembling the final object.
        /// See https://datatracker.ietf.org/doc/html/rfc7386 for details.
        /// Returns a JSON null value on error, with GetJsonError() filled in.
        /// </summary>
        /// <param name="jData">The JSON data to merge into</param>
        /// <param name="jMerge">The JSON data to merge</param>
        /// <returns>A modified copy with merged data, or null JSON value on error</returns>
        public static Json JsonMerge(Json jData, Json jMerge)
        {
            return global::NWN.Core.NWScript.JsonMerge(jData, jMerge);
        }

        /// <summary>
        /// Gets the object's local JSON variable.
        /// </summary>
        /// <param name="oObject">The object to get the variable from</param>
        /// <param name="sVarName">The variable name</param>
        /// <returns>The JSON variable value, or JSON null type on error</returns>
        public static Json GetLocalJson(uint oObject, string sVarName)
        {
            return global::NWN.Core.NWScript.GetLocalJson(oObject, sVarName);
        }

        /// <summary>
        /// Sets the object's local JSON variable to the specified value.
        /// </summary>
        /// <param name="oObject">The object to set the variable on</param>
        /// <param name="sVarName">The variable name</param>
        /// <param name="jValue">The JSON value to set</param>
        public static void SetLocalJson(uint oObject, string sVarName, Json jValue)
        {
            global::NWN.Core.NWScript.SetLocalJson(oObject, sVarName, jValue);
        }

        /// <summary>
        /// Deletes the object's local JSON variable.
        /// </summary>
        /// <param name="oObject">The object to delete the variable from</param>
        /// <param name="sVarName">The variable name to delete</param>
        public static void DeleteLocalJson(uint oObject, string sVarName)
        {
            global::NWN.Core.NWScript.DeleteLocalJson(oObject, sVarName);
        }

        /// <summary>
        /// Deserializes the given resref/template into a JSON structure.
        /// Supported GFF resource types:
        /// RESTYPE_CAF (and RESTYPE_ARE, RESTYPE_GIT, RESTYPE_GIC)
        /// RESTYPE_UTC, RESTYPE_UTI, RESTYPE_UTT, RESTYPE_UTP, RESTYPE_UTD, RESTYPE_UTW, RESTYPE_UTE, RESTYPE_UTM
        /// Returns a valid GFF-type JSON structure, or a null value with GetJsonError() set.
        /// </summary>
        /// <param name="sResRef">The resource reference</param>
        /// <param name="nResType">The resource type</param>
        /// <returns>A valid GFF-type JSON structure, or null value on error</returns>
        public static Json TemplateToJson(string sResRef, ResType nResType)
        {
            return global::NWN.Core.NWScript.TemplateToJson(sResRef, (int)nResType);
        }

        /// <summary>
        /// Returns a modified copy of the array with the value order changed according to nTransform:
        /// JSON_ARRAY_SORT_ASCENDING, JSON_ARRAY_SORT_DESCENDING
        /// Sorting is dependent on the type and follows JSON standards (e.g. 99 < "100").
        /// JSON_ARRAY_SHUFFLE: Randomizes the order of elements.
        /// JSON_ARRAY_REVERSE: Reverses the array.
        /// JSON_ARRAY_UNIQUE: Returns a modified copy of the array with duplicate values removed.
        /// Coercible but different types are not considered equal (e.g. 99 != "99"); int/float equivalence however applies: 4.0 == 4.
        /// Order is preserved.
        /// JSON_ARRAY_COALESCE: Returns the first non-null entry. Empty-ish values (e.g. "", 0) are not considered null, only the JSON scalar type.
        /// </summary>
        /// <param name="jArray">The JSON array to transform</param>
        /// <param name="nTransform">The transformation to apply</param>
        /// <returns>A modified copy of the array</returns>
        public static Json JsonArrayTransform(Json jArray, JsonArraySort nTransform)
        {
            return global::NWN.Core.NWScript.JsonArrayTransform(jArray, (int)nTransform);
        }

        /// <summary>
        /// Returns the nth-matching index or key of jNeedle in jHaystack.
        /// Supported haystacks: object, array
        /// Ordering behavior for objects is unspecified.
        /// Returns null when not found or on any error.
        /// </summary>
        /// <param name="jHaystack">The JSON object or array to search in</param>
        /// <param name="jNeedle">The JSON value to search for</param>
        /// <param name="nNth">The nth match to find (defaults to 0)</param>
        /// <param name="nConditional">The conditional type for matching (defaults to Equal)</param>
        /// <returns>The index or key of the match, or null if not found</returns>
        public static Json JsonFind(
            Json jHaystack,
            Json jNeedle,
            int nNth = 0,
            JsonFind nConditional = Enum.JsonFind.Equal)
        {
            return global::NWN.Core.NWScript.JsonFind(jHaystack, jNeedle, nNth, (int)nConditional);
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
            return global::NWN.Core.NWScript.JsonArrayGetRange(jArray, nBeginIndex, nEndIndex);
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
            return global::NWN.Core.NWScript.JsonSetOp(jValue, (int)nOp, jOther);
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
            return global::NWN.Core.NWScript.RegExpMatch(sRegExp, sValue, (int)nSyntaxFlags, (int)nMatchFlags);
        }

        /// <summary>
        /// Iterates the value with the regular expression.
        /// Returns an array of arrays; where each sub-array contains first the full match and then all matched groups.
        /// Returns empty JSON_ARRAY if no matches are found.
        /// If there was an error, the function will return JSON_NULL, with an error string filled in.
        /// Example: RegExpIterate("(\\d)(\\S+)", "1i 2am 3 4asentence"); -> [["1i", "1", "i"], ["2am", "2", "am"], ["4sentence", "4", "sentence"]]
        /// </summary>
        /// <param name="sRegExp">The regular expression pattern</param>
        /// <param name="sValue">The value to iterate over</param>
        /// <param name="nSyntaxFlags">A mask of REGEXP_* flags (defaults to Ecmascript)</param>
        /// <param name="nMatchFlags">A mask of REGEXP_MATCH_* and REGEXP_FORMAT_* flags (defaults to Default)</param>
        /// <returns>A JSON array of match arrays, or JSON_NULL on error</returns>
        public static Json RegExpIterate(
            string sRegExp,
            string sValue,
            RegularExpressionType nSyntaxFlags = RegularExpressionType.Ecmascript,
            RegularExpressionFormatType nMatchFlags = RegularExpressionFormatType.Default)
        {
            return global::NWN.Core.NWScript.RegExpIterate(sRegExp, sValue, (int)nSyntaxFlags, (int)nMatchFlags);
        }
    }
}
