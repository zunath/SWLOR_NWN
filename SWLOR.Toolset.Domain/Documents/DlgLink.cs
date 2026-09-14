using SWLOR.Toolset.Domain.Editing;
using SWLOR.Toolset.Domain.Gff;

namespace SWLOR.Toolset.Domain.Documents
{
    /// <summary>
    /// A pointer from one place in a conversation to a node, carrying the "appears when" test for
    /// that particular route. Links are what the conversation's shape is made of: a node knows
    /// nothing about who reaches it.
    /// </summary>
    /// <remarks>
    /// The distinction matters and is easy to get wrong. A condition belongs to the LINK, so the
    /// same line reached from two places can be guarded differently in each. An action belongs to
    /// the NODE, so it fires however the line was reached. A third of the links in the module are
    /// link-backs (<see cref="IsChild"/>), which is why this is not a corner case.
    /// </remarks>
    public sealed class DlgLink
    {
        internal const string ActiveField = "Active";
        internal const string ConditionParamsField = "ConditionParams";
        internal const string IndexField = "Index";
        internal const string IsChildField = "IsChild";
        internal const string LinkCommentField = "LinkComment";

        internal DlgLink(DlgDocument document, JsonGffStruct element, DlgNodeKind targetKind, DlgNode? parent)
        {
            Document = document;
            Struct = element;
            TargetKind = targetKind;
            Parent = parent;
        }

        /// <summary>The conversation this link belongs to.</summary>
        public DlgDocument Document { get; }

        /// <summary>The backing list element.</summary>
        public JsonGffStruct Struct { get; }

        /// <summary>Which list <see cref="TargetIndex"/> indexes into.</summary>
        public DlgNodeKind TargetKind { get; }

        /// <summary>
        /// The node whose link list holds this link, or null for an opening (a link in
        /// <c>StartingList</c>, which has no parent node).
        /// </summary>
        public DlgNode? Parent { get; }

        /// <summary>True when this link is one of the conversation's openings.</summary>
        public bool IsOpening => Parent == null;

        /// <summary>Position of the target node within its list.</summary>
        public int TargetIndex
        {
            get => (int)(Struct.GetUIntOrNull(IndexField) ?? 0u);
            internal set => Struct.SetUInt(IndexField, GffFieldType.Dword, (uint)value);
        }

        /// <summary>The node this link points at.</summary>
        public DlgNode Target => Document.GetNode(TargetKind, TargetIndex);

        /// <summary>
        /// True when this link re-uses a node that already appears elsewhere in the tree rather
        /// than owning it. Openings never carry this field — <c>StartingList</c> elements in the
        /// corpus have no <c>IsChild</c> at all.
        /// </summary>
        public bool IsChild
        {
            get => (Struct.GetIntOrNull(IsChildField) ?? 0) != 0;
            set => Struct.SetInt(IsChildField, GffFieldType.Byte, value ? 1 : 0);
        }

        /// <summary>
        /// The dispatcher script that evaluates this link's conditions, or an empty string when the
        /// link is unguarded. Maintained by <see cref="SetConditionDispatcher"/> rather than typed
        /// by hand.
        /// </summary>
        public string Active
        {
            get => Struct.GetStringOrNull(ActiveField) ?? string.Empty;
            set => Struct.SetString(ActiveField, GffFieldType.ResRef, value);
        }

        /// <summary>Aurora's per-link note, used only on link-backs in the corpus.</summary>
        public string? LinkComment
        {
            get => Struct.GetStringOrNull(LinkCommentField);
            set => Struct.SetString(LinkCommentField, GffFieldType.CExoString, value ?? string.Empty);
        }

        /// <summary>The conditions guarding this route, in the order the engine evaluates them.</summary>
        /// <remarks>
        /// <c>Snippet.ProcessConditions</c> requires every one of them to pass; there is no OR.
        /// Exactly one link in the whole module currently carries more than one.
        /// </remarks>
        public IReadOnlyList<DlgParam> Conditions
        {
            get
            {
                var elements = Struct.GetListOrEmpty(ConditionParamsField);
                var result = new List<DlgParam>(elements.Count);
                foreach (var element in elements)
                    result.Add(new DlgParam(element));

                return result;
            }
        }

        /// <summary>Adds a condition, wiring the dispatcher script if this is the first one.</summary>
        /// <param name="key">Snippet key, optionally prefixed with '!' to negate it.</param>
        /// <param name="value">Space-separated arguments.</param>
        public DlgParam AddCondition(string key, string value = "")
        {
            var field = GetOrCreateConditionParams();
            var element = DlgParam.CreateStruct((uint)field.Elements!.Count, key, value);
            field.InsertElement(field.Elements.Count, element);

            if (string.IsNullOrEmpty(Active))
                Active = DlgDocument.ConditionDispatcher;

            return new DlgParam(element);
        }

        /// <summary>
        /// Removes a condition, clearing the dispatcher script when the last one goes. Leaving a
        /// stale dispatcher behind would cost nothing at runtime, but leaving params behind without
        /// one is the silent no-op this type exists to prevent, so both ends are kept in step.
        /// </summary>
        public void RemoveCondition(DlgParam condition)
        {
            if (!Struct.TryGet(ConditionParamsField, out var field) || field.Elements == null)
                return;

            var position = IndexOfElement(field, condition.Struct);
            if (position < 0)
                return;

            field.RemoveElementAt(position);
            DlgDocument.RenumberStructIds(field.Elements, position);

            if (field.Elements.Count == 0 && DlgDocument.IsConditionDispatcher(Active))
                Active = string.Empty;
        }

        private JsonGffField GetOrCreateConditionParams()
        {
            if (Struct.TryGet(ConditionParamsField, out var existing))
            {
                existing.Elements ??= new List<JsonGffStruct>();
                return existing;
            }

            var field = JsonGffField.CreateList();
            Struct.Add(ConditionParamsField, field);
            return field;
        }

        private static int IndexOfElement(JsonGffField field, JsonGffStruct element)
        {
            for (var i = 0; i < field.Elements!.Count; i++)
            {
                if (ReferenceEquals(field.Elements[i], element))
                    return i;
            }

            return -1;
        }

        /// <summary>Builds a link list element with the corpus's field set for its list.</summary>
        /// <remarks>
        /// Openings differ from ordinary links by more than convention: no <c>StartingList</c>
        /// element in the corpus carries an <c>IsChild</c> field, so one is not written here.
        /// </remarks>
        internal static JsonGffStruct CreateStruct(uint structId, int targetIndex, bool isOpening, bool isChild)
        {
            var element = new JsonGffStruct();
            using (EditScope.EnterConstruction())
            {
                element.SetStructId(structId);
                element.SetString(ActiveField, GffFieldType.ResRef, string.Empty);
                element.Add(ConditionParamsField, JsonGffField.CreateList());
                element.SetUInt(IndexField, GffFieldType.Dword, (uint)targetIndex);
                if (!isOpening)
                    element.SetInt(IsChildField, GffFieldType.Byte, isChild ? 1 : 0);
            }

            return element;
        }
    }
}
