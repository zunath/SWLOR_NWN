using SWLOR.Toolset.Domain.Editing;
using SWLOR.Toolset.Domain.Gff;

namespace SWLOR.Toolset.Domain.Documents
{
    /// <summary>
    /// Typed view over a .dlg (conversation) nwn_gff JSON document: the NPC entries, the player
    /// replies, the links between them, and the ordered openings the engine tries in turn.
    /// </summary>
    /// <remarks>
    /// Two facts about the format drive every editing method here.
    /// <para>
    /// <b>Links address nodes by list position.</b> <c>EntryList</c> and <c>ReplyList</c> are flat
    /// arrays and a link stores an <c>Index</c> into one of them, so removing a node from the middle
    /// renumbers every node after it and rewrites every link that pointed past it. Insertion is
    /// therefore always an append (<see cref="AddEntry"/>, <see cref="AddReply"/>) whatever the
    /// node's position in the conversation: a new line touches its own struct and one link, and that
    /// is the whole diff. Removal states its cost up front — see <see cref="EstimateRemoveNode"/>.
    /// </para>
    /// <para>
    /// <b>Every list numbers its elements by position.</b> Verified across all 609 dialogs: an
    /// element's <c>__struct_id</c> equals its index in every list, with four historical exceptions
    /// in starting-link condition params. Anything that shifts positions renumbers as well, which is
    /// what <see cref="RenumberStructIds"/> is for.
    /// </para>
    /// </remarks>
    public sealed class DlgDocument : GffDocumentBase
    {
        /// <summary>Per-node <c>Delay</c> value meaning "use the conversation's own delay".</summary>
        public const uint NoDelay = uint.MaxValue;

        /// <summary>
        /// The script resref that runs a node's <c>ActionParams</c>. One of several names the game
        /// registers for the same handler (<c>ScriptName.OnDialogAction</c>); this is the one the
        /// authored corpus uses and the one this editor writes.
        /// </summary>
        public const string ActionDispatcher = "action";

        /// <summary>
        /// The script resref that evaluates a link's <c>ConditionParams</c>. As with
        /// <see cref="ActionDispatcher"/>, the corpus also contains the equivalent
        /// <c>appears</c> spelling; both are read, this one is written.
        /// </summary>
        public const string ConditionDispatcher = "condition";

        /// <summary>
        /// Every resref the game registers as a snippet action dispatcher. New content is written
        /// with <see cref="ActionDispatcher"/>, but existing content uses these interchangeably, so
        /// recognising all of them is what lets the editor clear a dispatcher it did not write.
        /// </summary>
        private static readonly string[] ActionDispatchers = { "action", "actions" };

        /// <summary>The condition-side equivalent of <see cref="ActionDispatchers"/>.</summary>
        /// <remarks>
        /// Exact matches only. The <c>dialog_appears_*</c> and <c>dialog_action_*</c> resrefs that
        /// dominate the corpus belong to the C# <c>Dialog</c> service's generated shells and are not
        /// snippet dispatchers; treating them as such would strip a script the toolset does not own.
        /// </remarks>
        private static readonly string[] ConditionDispatchers = { "appear", "appears", "condition", "conditions" };

        /// <summary>Whether this resref is a snippet action dispatcher rather than custom NWScript.</summary>
        public static bool IsActionDispatcher(string resref) => Matches(ActionDispatchers, resref);

        /// <summary>Whether this resref is a snippet condition dispatcher rather than custom NWScript.</summary>
        public static bool IsConditionDispatcher(string resref) => Matches(ConditionDispatchers, resref);

        private static bool Matches(string[] candidates, string resref)
        {
            foreach (var candidate in candidates)
            {
                if (string.Equals(candidate, resref, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private const string EntryListField = "EntryList";
        private const string ReplyListField = "ReplyList";
        private const string StartingListField = "StartingList";
        private const string DelayEntryField = "DelayEntry";
        private const string DelayReplyField = "DelayReply";
        private const string EndConversationField = "EndConversation";
        private const string EndConverAbortField = "EndConverAbort";
        private const string PreventZoomInField = "PreventZoomIn";
        private const string NumWordsField = "NumWords";

        public DlgDocument(JsonGffDocument document) : base(document)
        {
        }

        public static DlgDocument Load(string path) => new(JsonGffDocument.Load(path));

        public static DlgDocument Parse(byte[] content) => new(JsonGffDocument.Parse(content));

        /// <summary>The NPC lines, in list order.</summary>
        public IReadOnlyList<DlgNode> Entries => BuildNodes(DlgNodeKind.Entry);

        /// <summary>The player choices, in list order.</summary>
        public IReadOnlyList<DlgNode> Replies => BuildNodes(DlgNodeKind.Reply);

        /// <summary>
        /// The conversation's openings, in the order the engine tests them. The first whose
        /// conditions pass is the one the player gets, so order is meaning: an unguarded opening
        /// makes every opening below it unreachable.
        /// </summary>
        public IReadOnlyList<DlgLink> Openings
        {
            get
            {
                var elements = Root.GetListOrEmpty(StartingListField);
                var result = new List<DlgLink>(elements.Count);
                foreach (var element in elements)
                    result.Add(new DlgLink(this, element, DlgNodeKind.Entry, null));

                return result;
            }
        }

        public string EndConversation
        {
            get => Root.GetStringOrNull(EndConversationField) ?? string.Empty;
            set => Root.SetString(EndConversationField, GffFieldType.ResRef, value);
        }

        public string EndConverAbort
        {
            get => Root.GetStringOrNull(EndConverAbortField) ?? string.Empty;
            set => Root.SetString(EndConverAbortField, GffFieldType.ResRef, value);
        }

        public uint DelayEntry
        {
            get => Root.GetUIntOrNull(DelayEntryField) ?? 0u;
            set => Root.SetUInt(DelayEntryField, GffFieldType.Dword, value);
        }

        public uint DelayReply
        {
            get => Root.GetUIntOrNull(DelayReplyField) ?? 0u;
            set => Root.SetUInt(DelayReplyField, GffFieldType.Dword, value);
        }

        public bool PreventZoomIn
        {
            get => (Root.GetIntOrNull(PreventZoomInField) ?? 0) != 0;
            set => Root.SetInt(PreventZoomInField, GffFieldType.Byte, value ? 1 : 0);
        }

        /// <summary>The stored word count, or null in the 39 files that carry no such field.</summary>
        public uint? NumWords => Root.GetUIntOrNull(NumWordsField);

        /// <summary>
        /// Counts the words across every entry and reply the way Aurora does — whitespace-separated
        /// tokens, punctuation and quote marks included.
        /// </summary>
        public int CountWords()
        {
            var total = 0;
            foreach (var listField in new[] { EntryListField, ReplyListField })
            {
                foreach (var element in Root.GetListOrEmpty(listField))
                {
                    var text = element.GetLocStringOrNull(DlgNode.TextField)?.Text;
                    if (string.IsNullOrEmpty(text))
                        continue;

                    total += text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
                }
            }

            return total;
        }

        /// <summary>
        /// Rewrites <c>NumWords</c> from the current text, returning true when the value changed.
        /// Only touched when it actually moves, so a save that did not alter any text leaves the
        /// line alone.
        /// </summary>
        public bool RecomputeWordCount()
        {
            var count = (uint)CountWords();
            if (NumWords == count)
                return false;

            Root.SetUInt(NumWordsField, GffFieldType.Dword, count);
            return true;
        }

        /// <summary>The node at a position in one of the two lists.</summary>
        public DlgNode GetNode(DlgNodeKind kind, int index)
        {
            var elements = Root.GetListOrEmpty(ListFieldName(kind));
            if (index < 0 || index >= elements.Count)
                throw new ArgumentOutOfRangeException(nameof(index),
                    $"{kind} {index} is outside the conversation's {elements.Count} {kind.ToString().ToLowerInvariant()} nodes.");

            return new DlgNode(this, elements[index], kind);
        }

        /// <summary>True when a link's target position actually exists.</summary>
        public bool HasNode(DlgNodeKind kind, int index)
        {
            return index >= 0 && index < Root.GetListOrEmpty(ListFieldName(kind)).Count;
        }

        /// <summary>This node's position in its list, or -1 if it is no longer part of the document.</summary>
        public int IndexOf(DlgNode node)
        {
            var elements = Root.GetListOrEmpty(ListFieldName(node.Kind));
            for (var i = 0; i < elements.Count; i++)
            {
                if (ReferenceEquals(elements[i], node.Struct))
                    return i;
            }

            return -1;
        }

        /// <summary>Appends an NPC line. Nothing already in the file moves.</summary>
        public DlgNode AddEntry(string text = "") => AddNode(DlgNodeKind.Entry, text);

        /// <summary>Appends a player choice. Nothing already in the file moves.</summary>
        public DlgNode AddReply(string text = "") => AddNode(DlgNodeKind.Reply, text);

        /// <summary>
        /// Appends an opening pointing at an entry. Openings are tested top-down, so a new one goes
        /// last and will not fire if an earlier opening is unguarded — see <see cref="Openings"/>.
        /// </summary>
        public DlgLink AddOpening(DlgNode entry)
        {
            if (entry.Kind != DlgNodeKind.Entry)
                throw new ArgumentException("A conversation opens on an NPC line, not a player choice.", nameof(entry));

            var index = RequireIndex(entry);
            var field = GetOrCreateRootList(StartingListField);
            var element = DlgLink.CreateStruct((uint)field.Elements!.Count, index, isOpening: true, isChild: false);
            field.InsertElement(field.Elements.Count, element);

            return new DlgLink(this, element, DlgNodeKind.Entry, null);
        }

        /// <summary>
        /// Links a line to the one that follows it. An entry may only lead to replies and a reply
        /// only to entries, which is what makes a conversation alternate.
        /// </summary>
        /// <param name="isChild">
        /// True to mark this as a re-use of a node that already appears elsewhere. The engine
        /// follows both kinds identically; the flag tells an editor not to expand the same subtree
        /// twice.
        /// </param>
        public DlgLink AddLink(DlgNode parent, DlgNode target, bool isChild = false)
        {
            if (target.Kind != parent.LinkTargetKind)
                throw new ArgumentException(
                    $"A {parent.Kind} leads to a {parent.LinkTargetKind}, not to a {target.Kind}.", nameof(target));

            RequireIndex(parent);
            var targetIndex = RequireIndex(target);

            var field = GetOrCreateList(parent.Struct, parent.LinkListField);
            var element = DlgLink.CreateStruct((uint)field.Elements!.Count, targetIndex, isOpening: false, isChild: isChild);
            field.InsertElement(field.Elements.Count, element);

            return new DlgLink(this, element, parent.LinkTargetKind, parent);
        }

        /// <summary>
        /// Reorders the openings. Order is meaning here — the engine takes the first that fits — so
        /// this is how a situation is promoted above one that was swallowing it.
        /// </summary>
        public void MoveOpening(int fromIndex, int toIndex)
        {
            if (!Root.TryGet(StartingListField, out var field) || field.Elements == null)
                return;

            if (fromIndex == toIndex)
                return;

            field.MoveElement(fromIndex, toIndex);
            RenumberStructIds(field.Elements, Math.Min(fromIndex, toIndex));
        }

        /// <summary>
        /// Reorders one route beneath its parent line. NWN presents an entry's reply links in this
        /// exact order, so moving the link changes where that player choice appears without moving
        /// or duplicating the reply node it targets.
        /// </summary>
        public void MoveLink(DlgLink link, int toIndex)
        {
            ArgumentNullException.ThrowIfNull(link);
            if (link.Parent == null)
                throw new ArgumentException("Use MoveOpening for a conversation opening.", nameof(link));

            if (!link.Parent.Struct.TryGet(link.Parent.LinkListField, out var field)
                || field.Elements == null)
            {
                return;
            }

            var fromIndex = IndexOfElement(field.Elements, link.Struct);
            if (fromIndex < 0 || fromIndex == toIndex)
                return;

            field.MoveElement(fromIndex, toIndex);
            RenumberStructIds(field.Elements, Math.Min(fromIndex, toIndex));
        }

        /// <summary>
        /// Removes one route without touching the node it pointed at. Renumbers nothing but the link
        /// list it came from, so it is the cheap way to detach a line — at the price of possibly
        /// leaving it unreachable. <see cref="FindOrphans"/> reports those.
        /// </summary>
        public void RemoveLink(DlgLink link)
        {
            var ownerStruct = link.Parent?.Struct ?? Root;
            var fieldName = link.Parent == null ? StartingListField : link.Parent.LinkListField;
            if (!ownerStruct.TryGet(fieldName, out var field) || field.Elements == null)
                return;

            var position = IndexOfElement(field.Elements, link.Struct);
            if (position < 0)
                return;

            field.RemoveElementAt(position);
            RenumberStructIds(field.Elements, position);
        }

        /// <summary>Every link anywhere in the conversation that reaches this node.</summary>
        public IReadOnlyList<DlgLink> IncomingLinks(DlgNode node)
        {
            var index = IndexOf(node);
            var result = new List<DlgLink>();
            if (index < 0)
                return result;

            foreach (var link in AllLinks())
            {
                if (link.TargetKind == node.Kind && link.TargetIndex == index)
                    result.Add(link);
            }

            return result;
        }

        /// <summary>
        /// What removing this node would cost, so a caller can say so before doing it. Deleting from
        /// the middle of a list is correct but not cheap: every later node renumbers and every link
        /// past it is rewritten.
        /// </summary>
        public DlgRemovalCost EstimateRemoveNode(DlgNode node)
        {
            var index = IndexOf(node);
            if (index < 0)
                return new DlgRemovalCost(0, 0, 0);

            var listCount = Root.GetListOrEmpty(ListFieldName(node.Kind)).Count;
            var incoming = 0;
            var rewritten = 0;
            foreach (var link in AllLinks())
            {
                if (link.TargetKind != node.Kind)
                    continue;

                if (link.TargetIndex == index)
                    incoming++;
                else if (link.TargetIndex > index)
                    rewritten++;
            }

            return new DlgRemovalCost(incoming, listCount - index - 1, rewritten);
        }

        /// <summary>
        /// Removes a line, every route to it, and the renumbering that follows. Its own onward links
        /// go with it, which can leave the lines below it unreachable — <see cref="FindOrphans"/>
        /// reports those rather than this method cascading into them, because whether a delete
        /// should take a whole branch with it is a decision for the caller.
        /// </summary>
        public void RemoveNode(DlgNode node)
        {
            var index = IndexOf(node);
            if (index < 0)
                return;

            foreach (var link in IncomingLinks(node))
                RemoveLink(link);

            var listField = ListFieldName(node.Kind);
            if (!Root.TryGet(listField, out var field) || field.Elements == null)
                return;

            var position = IndexOfElement(field.Elements, node.Struct);
            if (position < 0)
                return;

            field.RemoveElementAt(position);
            RenumberStructIds(field.Elements, position);

            // Every surviving link that pointed past the hole now points one place too far.
            foreach (var link in AllLinks())
            {
                if (link.TargetKind == node.Kind && link.TargetIndex > index)
                    link.TargetIndex -= 1;
            }
        }

        /// <summary>
        /// Appends a copy of a line — its text, delivery, actions and onward routes — and returns it.
        /// </summary>
        /// <remarks>
        /// This is what "make a separate copy" does to a line reached from several places: the copy
        /// takes over one of those routes so the two can be edited apart. The onward links are
        /// copied rather than moved, so both versions still lead where the original did.
        /// </remarks>
        public DlgNode DuplicateNode(DlgNode node)
        {
            RequireIndex(node);

            var copy = AddNode(node.Kind, node.Text);
            copy.TextLocString.CopyFrom(node.TextLocString);
            copy.Speaker = node.Speaker;
            copy.Sound = node.Sound;
            copy.Animation = node.Animation;
            copy.AnimLoop = node.AnimLoop;
            copy.Delay = node.Delay;
            copy.Comment = node.Comment;
            copy.Quest = node.Quest;

            // AddAction owns the dispatcher field. Copy the params first, then restore the exact
            // script so custom-script and historical hybrid nodes round-trip unchanged.
            foreach (var action in node.Actions.Where(action => !action.IsOncePerPlayerMarker))
                copy.AddAction(action.Key, action.Value);
            copy.Script = node.Script;

            foreach (var link in node.Links)
            {
                var copied = AddLink(copy, link.Target, link.IsChild);
                copied.Active = link.Active;
                foreach (var condition in link.Conditions)
                    copied.AddCondition(condition.Key, condition.Value);
            }

            return copy;
        }

        /// <summary>Points an existing route at a different line, leaving its guards alone.</summary>
        public void Retarget(DlgLink link, DlgNode target)
        {
            var expected = link.Parent?.LinkTargetKind ?? DlgNodeKind.Entry;
            if (target.Kind != expected)
                throw new ArgumentException($"That route leads to a {expected}, not to a {target.Kind}.", nameof(target));

            link.TargetIndex = RequireIndex(target);
        }

        /// <summary>
        /// Nodes no opening can reach. They cost nothing at runtime but shift the indices of
        /// everything after them, so they are worth reporting and worth clearing deliberately.
        /// </summary>
        public IReadOnlyList<DlgNode> FindOrphans()
        {
            var reachedEntries = new HashSet<int>();
            var reachedReplies = new HashSet<int>();
            var pending = new Queue<(DlgNodeKind Kind, int Index)>();

            foreach (var opening in Openings)
            {
                if (HasNode(DlgNodeKind.Entry, opening.TargetIndex) && reachedEntries.Add(opening.TargetIndex))
                    pending.Enqueue((DlgNodeKind.Entry, opening.TargetIndex));
            }

            while (pending.Count > 0)
            {
                var (kind, index) = pending.Dequeue();
                foreach (var link in GetNode(kind, index).Links)
                {
                    if (!HasNode(link.TargetKind, link.TargetIndex))
                        continue;

                    var seen = link.TargetKind == DlgNodeKind.Entry ? reachedEntries : reachedReplies;
                    if (seen.Add(link.TargetIndex))
                        pending.Enqueue((link.TargetKind, link.TargetIndex));
                }
            }

            var orphans = new List<DlgNode>();
            AppendOrphans(orphans, DlgNodeKind.Entry, reachedEntries);
            AppendOrphans(orphans, DlgNodeKind.Reply, reachedReplies);
            return orphans;
        }

        /// <summary>
        /// Links whose target position does not exist. A conversation should never contain one;
        /// finding any means the file was edited by something that did not renumber.
        /// </summary>
        public IReadOnlyList<DlgLink> FindDanglingLinks()
        {
            var result = new List<DlgLink>();
            foreach (var link in AllLinks())
            {
                if (!HasNode(link.TargetKind, link.TargetIndex))
                    result.Add(link);
            }

            return result;
        }

        /// <summary>Every link in the conversation: the openings, then each node's own list.</summary>
        public IEnumerable<DlgLink> AllLinks()
        {
            foreach (var opening in Openings)
                yield return opening;

            foreach (var kind in new[] { DlgNodeKind.Entry, DlgNodeKind.Reply })
            {
                foreach (var node in BuildNodes(kind))
                {
                    foreach (var link in node.Links)
                        yield return link;
                }
            }
        }

        /// <summary>
        /// Restores the "element id equals element position" numbering every list in the corpus
        /// uses, from <paramref name="startIndex"/> onward. Only writes where the value actually
        /// differs, so it never manufactures a diff.
        /// </summary>
        internal static void RenumberStructIds(IReadOnlyList<JsonGffStruct> elements, int startIndex)
        {
            for (var i = Math.Max(0, startIndex); i < elements.Count; i++)
            {
                if (elements[i].StructId != (uint)i)
                    elements[i].SetStructId((uint)i);
            }
        }

        private DlgNode AddNode(DlgNodeKind kind, string text)
        {
            var field = GetOrCreateRootList(ListFieldName(kind));
            var element = DlgNode.CreateStruct((uint)field.Elements!.Count, kind, text);
            field.InsertElement(field.Elements.Count, element);

            return new DlgNode(this, element, kind);
        }

        private void AppendOrphans(List<DlgNode> orphans, DlgNodeKind kind, HashSet<int> reached)
        {
            var elements = Root.GetListOrEmpty(ListFieldName(kind));
            for (var i = 0; i < elements.Count; i++)
            {
                if (!reached.Contains(i))
                    orphans.Add(new DlgNode(this, elements[i], kind));
            }
        }

        private IReadOnlyList<DlgNode> BuildNodes(DlgNodeKind kind)
        {
            var elements = Root.GetListOrEmpty(ListFieldName(kind));
            var result = new List<DlgNode>(elements.Count);
            foreach (var element in elements)
                result.Add(new DlgNode(this, element, kind));

            return result;
        }

        private int RequireIndex(DlgNode node)
        {
            var index = IndexOf(node);
            if (index < 0)
                throw new ArgumentException("That line is not part of this conversation.", nameof(node));

            return index;
        }

        private JsonGffField GetOrCreateRootList(string name) => GetOrCreateList(Root, name);

        private static JsonGffField GetOrCreateList(JsonGffStruct owner, string name)
        {
            if (owner.TryGet(name, out var existing))
            {
                existing.Elements ??= new List<JsonGffStruct>();
                return existing;
            }

            var field = JsonGffField.CreateList();
            owner.Add(name, field);
            return field;
        }

        private static int IndexOfElement(IReadOnlyList<JsonGffStruct> elements, JsonGffStruct element)
        {
            for (var i = 0; i < elements.Count; i++)
            {
                if (ReferenceEquals(elements[i], element))
                    return i;
            }

            return -1;
        }

        private static string ListFieldName(DlgNodeKind kind) =>
            kind == DlgNodeKind.Entry ? EntryListField : ReplyListField;
    }

    /// <summary>
    /// What removing a node will disturb, for a caller that wants to say so first.
    /// </summary>
    /// <param name="RoutesRemoved">Links pointing at the node, which go with it.</param>
    /// <param name="NodesRenumbered">Nodes after it in its list, whose positions all shift down one.</param>
    /// <param name="LinksRewritten">Surviving links whose stored index has to be decremented.</param>
    public readonly record struct DlgRemovalCost(int RoutesRemoved, int NodesRenumbered, int LinksRewritten)
    {
        /// <summary>True when nothing but the node itself is touched — an append being undone.</summary>
        public bool IsLocal => NodesRenumbered == 0 && LinksRewritten == 0;
    }
}
