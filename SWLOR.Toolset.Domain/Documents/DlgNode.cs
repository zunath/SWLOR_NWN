using SWLOR.Toolset.Domain.Editing;
using SWLOR.Toolset.Domain.Gff;

namespace SWLOR.Toolset.Domain.Documents
{
    /// <summary>Which of a conversation's two node lists a node or link target lives in.</summary>
    public enum DlgNodeKind
    {
        /// <summary>An NPC line (<c>EntryList</c>).</summary>
        Entry,

        /// <summary>A player choice (<c>ReplyList</c>).</summary>
        Reply
    }

    /// <summary>
    /// One spoken line — an NPC entry or a player reply — as a view over its list element. A node
    /// owns its text and its actions; who can reach it, and under what conditions, belongs to the
    /// <see cref="DlgLink"/>s that point at it.
    /// </summary>
    public sealed class DlgNode
    {
        internal const string ActionParamsField = "ActionParams";
        internal const string AnimationField = "Animation";
        internal const string AnimLoopField = "AnimLoop";
        internal const string CommentField = "Comment";
        internal const string DelayField = "Delay";
        internal const string QuestField = "Quest";
        internal const string ScriptField = "Script";
        internal const string SoundField = "Sound";
        internal const string SpeakerField = "Speaker";
        internal const string TextField = "Text";
        internal const string RepliesListField = "RepliesList";
        internal const string EntriesListField = "EntriesList";

        internal DlgNode(DlgDocument document, JsonGffStruct element, DlgNodeKind kind)
        {
            Document = document;
            Struct = element;
            Kind = kind;
        }

        /// <summary>The conversation this node belongs to.</summary>
        public DlgDocument Document { get; }

        /// <summary>The backing list element.</summary>
        public JsonGffStruct Struct { get; }

        public DlgNodeKind Kind { get; }

        /// <summary>True for an NPC line.</summary>
        public bool IsEntry => Kind == DlgNodeKind.Entry;

        /// <summary>
        /// This node's position in its list — which is also its identity, since links address nodes
        /// by position. Looked up live rather than cached, so a view stays correct across the
        /// renumbering a delete causes.
        /// </summary>
        public int Index => Document.IndexOf(this);

        /// <summary>The name of this node's own link list: replies for an entry, entries for a reply.</summary>
        internal string LinkListField => IsEntry ? RepliesListField : EntriesListField;

        /// <summary>Which list this node's links point into — the opposite one to its own.</summary>
        internal DlgNodeKind LinkTargetKind => IsEntry ? DlgNodeKind.Reply : DlgNodeKind.Entry;

        /// <summary>The spoken line. Language 0 is the only entry anything in the module reads.</summary>
        public string Text
        {
            get => Struct.GetLocStringOrNull(TextField)?.Text ?? string.Empty;
            set => Struct.GetOrAddLocString(TextField).Text = value;
        }

        /// <summary>The full localized-string view, for the rare non-English entry.</summary>
        public LocString TextLocString => Struct.GetOrAddLocString(TextField);

        /// <summary>
        /// The dispatcher script that runs this node's actions, or an empty string when it has
        /// none. Maintained by <see cref="AddAction"/>/<see cref="RemoveAction"/>.
        /// </summary>
        public string Script
        {
            get => Struct.GetStringOrNull(ScriptField) ?? string.Empty;
            set => Struct.SetString(ScriptField, GffFieldType.ResRef, value);
        }

        /// <summary>Tag of an alternate speaker; only 3 nodes in the module set one.</summary>
        public string Speaker
        {
            get => Struct.GetStringOrNull(SpeakerField) ?? string.Empty;
            set => Struct.SetString(SpeakerField, GffFieldType.CExoString, value);
        }

        public string Sound
        {
            get => Struct.GetStringOrNull(SoundField) ?? string.Empty;
            set => Struct.SetString(SoundField, GffFieldType.ResRef, value);
        }

        public uint Animation
        {
            get => Struct.GetUIntOrNull(AnimationField) ?? 0u;
            set => Struct.SetUInt(AnimationField, GffFieldType.Dword, value);
        }

        public bool AnimLoop
        {
            get => (Struct.GetIntOrNull(AnimLoopField) ?? 1) != 0;
            set => Struct.SetInt(AnimLoopField, GffFieldType.Byte, value ? 1 : 0);
        }

        /// <summary>
        /// Per-node delay before the line is spoken. <see cref="DlgDocument.NoDelay"/> (0xFFFFFFFF)
        /// means "use the conversation's own delay", and is what every authored node carries.
        /// </summary>
        public uint Delay
        {
            get => Struct.GetUIntOrNull(DelayField) ?? DlgDocument.NoDelay;
            set => Struct.SetUInt(DelayField, GffFieldType.Dword, value);
        }

        /// <summary>Aurora's per-node note. Unused across all 12,297 nodes in the module.</summary>
        public string Comment
        {
            get => Struct.GetStringOrNull(CommentField) ?? string.Empty;
            set => Struct.SetString(CommentField, GffFieldType.CExoString, value);
        }

        /// <summary>The stock NWN journal category. Unused by SWLOR, preserved on save.</summary>
        public string Quest
        {
            get => Struct.GetStringOrNull(QuestField) ?? string.Empty;
            set => Struct.SetString(QuestField, GffFieldType.CExoString, value);
        }

        /// <summary>
        /// What happens when this line is reached — from anywhere. A node reached by several links
        /// runs these every time, which is why <see cref="DlgDocument.IncomingLinks"/> exists.
        /// </summary>
        public IReadOnlyList<DlgParam> Actions
        {
            get
            {
                var elements = Struct.GetListOrEmpty(ActionParamsField);
                var result = new List<DlgParam>(elements.Count);
                foreach (var element in elements)
                    result.Add(new DlgParam(element));

                return result;
            }
        }

        /// <summary>Where the conversation can go from this line, in the order the player sees them.</summary>
        public IReadOnlyList<DlgLink> Links
        {
            get
            {
                var elements = Struct.GetListOrEmpty(LinkListField);
                var result = new List<DlgLink>(elements.Count);
                foreach (var element in elements)
                    result.Add(new DlgLink(Document, element, LinkTargetKind, this));

                return result;
            }
        }

        /// <summary>Adds an action, wiring the dispatcher script if this is the first one.</summary>
        public DlgParam AddAction(string key, string value = "")
        {
            if (!string.IsNullOrEmpty(Script) && !DlgDocument.IsActionDispatcher(Script))
            {
                throw new InvalidOperationException(
                    $"This line runs the custom script '{Script}'. Remove it before adding snippet effects.");
            }

            var field = GetOrCreateActionParams();
            var element = DlgParam.CreateStruct((uint)field.Elements!.Count, key, value);
            field.InsertElement(field.Elements.Count, element);

            if (string.IsNullOrEmpty(Script))
                Script = DlgDocument.ActionDispatcher;

            return new DlgParam(element);
        }

        /// <summary>Removes an action, clearing the dispatcher script when the last one goes.</summary>
        public void RemoveAction(DlgParam action)
        {
            if (!action.IsOncePerPlayerMarker)
            {
                var marker = Actions.FirstOrDefault(candidate =>
                    candidate.IsOncePerPlayerMarker
                    && candidate.MarkedActionKey.Equals(action.SnippetKey, StringComparison.OrdinalIgnoreCase));
                if (marker != null)
                    RemoveAction(marker);
            }

            if (!Struct.TryGet(ActionParamsField, out var field) || field.Elements == null)
                return;

            var position = -1;
            for (var i = 0; i < field.Elements.Count; i++)
            {
                if (ReferenceEquals(field.Elements[i], action.Struct))
                {
                    position = i;
                    break;
                }
            }

            if (position < 0)
                return;

            field.RemoveElementAt(position);
            DlgDocument.RenumberStructIds(field.Elements, position);

            if (field.Elements.Count == 0 && DlgDocument.IsActionDispatcher(Script))
                Script = string.Empty;
        }

        private JsonGffField GetOrCreateActionParams()
        {
            if (Struct.TryGet(ActionParamsField, out var existing))
            {
                existing.Elements ??= new List<JsonGffStruct>();
                return existing;
            }

            var field = JsonGffField.CreateList();
            Struct.Add(ActionParamsField, field);
            return field;
        }

        public override bool Equals(object? obj) =>
            obj is DlgNode other && ReferenceEquals(other.Struct, Struct);

        public override int GetHashCode() => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(Struct);

        public override string ToString() => $"{Kind} {Index}: {Text}";

        /// <summary>
        /// Builds a node list element carrying the exact field set authored content uses — including
        /// the empty <c>ActionParams</c> list and the empty language-0 text entry, both of which the
        /// recently authored conversations write rather than omit.
        /// </summary>
        internal static JsonGffStruct CreateStruct(uint structId, DlgNodeKind kind, string text)
        {
            var element = new JsonGffStruct();
            using (EditScope.EnterConstruction())
            {
                element.SetStructId(structId);
                element.Add(ActionParamsField, JsonGffField.CreateList());
                element.SetUInt(AnimationField, GffFieldType.Dword, 0u);
                element.SetInt(AnimLoopField, GffFieldType.Byte, 1);
                element.SetString(CommentField, GffFieldType.CExoString, string.Empty);
                element.SetUInt(DelayField, GffFieldType.Dword, DlgDocument.NoDelay);
                element.SetString(QuestField, GffFieldType.CExoString, string.Empty);
                element.Add(
                    kind == DlgNodeKind.Entry ? RepliesListField : EntriesListField,
                    JsonGffField.CreateList());
                element.SetString(ScriptField, GffFieldType.ResRef, string.Empty);
                element.SetString(SoundField, GffFieldType.ResRef, string.Empty);
                if (kind == DlgNodeKind.Entry)
                    element.SetString(SpeakerField, GffFieldType.CExoString, string.Empty);

                element.GetOrAddLocString(TextField).Text = text;
            }

            return element;
        }
    }
}
