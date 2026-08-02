using SWLOR.Toolset.Domain.Editing;
using SWLOR.Toolset.Domain.Gff;

namespace SWLOR.Toolset.Domain.Documents
{
    /// <summary>
    /// One entry of a node's <c>ActionParams</c> or a link's <c>ConditionParams</c>: a snippet key
    /// and its space-separated argument string.
    /// </summary>
    /// <remarks>
    /// This is SWLOR's conversation-logic mechanism rather than a stock NWN one. The engine reads
    /// these through <c>UtilPlugin.GetScriptParamIsSet</c> from the dispatcher script named in the
    /// node's <c>Script</c> field (actions) or the link's <c>Active</c> field (conditions) — so a
    /// param list with no dispatcher resref beside it never runs at all, which is the silent
    /// failure <see cref="DlgDocument"/> exists to make impossible to author.
    /// <para>
    /// A leading '!' on the key negates a condition; see <c>Snippet.ProcessConditions</c>.
    /// </para>
    /// </remarks>
    public sealed class DlgParam
    {
        private const string OncePerPlayerPrefix = "once-";

        internal const string KeyField = "Key";
        internal const string ValueField = "Value";

        internal DlgParam(JsonGffStruct element)
        {
            Struct = element;
        }

        /// <summary>The backing list element.</summary>
        public JsonGffStruct Struct { get; }

        /// <summary>The snippet key, including any leading '!' negation marker.</summary>
        public string Key
        {
            get => Struct.GetStringOrNull(KeyField) ?? string.Empty;
            set => Struct.SetString(KeyField, GffFieldType.CExoString, value);
        }

        /// <summary>The snippet key with any leading '!' stripped.</summary>
        public string SnippetKey
        {
            get
            {
                var key = Key;
                return key.StartsWith('!') ? key[1..] : key;
            }
        }

        /// <summary>True when this condition is negated (the key carries a leading '!').</summary>
        public bool IsNegated => Key.StartsWith('!');

        /// <summary>
        /// Obsolete metadata from an earlier DLG authoring experiment. It is recognized only so
        /// legacy files can be opened, converted, and copied without treating it as an executable
        /// snippet; the graph-native format deliberately discards it.
        /// </summary>
        public bool IsOncePerPlayerMarker => Key.StartsWith(OncePerPlayerPrefix, StringComparison.Ordinal);

        public string MarkedActionKey => IsOncePerPlayerMarker
            ? Key[OncePerPlayerPrefix.Length..]
            : string.Empty;

        /// <summary>The raw argument string; arguments are separated by spaces.</summary>
        public string Value
        {
            get => Struct.GetStringOrNull(ValueField) ?? string.Empty;
            set => Struct.SetString(ValueField, GffFieldType.CExoString, value);
        }

        /// <summary>The argument string split the way <c>Snippet</c> splits it at runtime.</summary>
        public string[] Arguments =>
            Value.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        /// <summary>Builds a param list element with the corpus's field set.</summary>
        internal static JsonGffStruct CreateStruct(uint structId, string key, string value)
        {
            var element = new JsonGffStruct();
            using (EditScope.EnterConstruction())
            {
                element.SetStructId(structId);
                element.SetString(KeyField, GffFieldType.CExoString, key);
                element.SetString(ValueField, GffFieldType.CExoString, value);
            }

            return element;
        }
    }
}
