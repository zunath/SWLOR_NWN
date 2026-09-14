using SWLOR.Toolset.Domain.Documents;
using Newtonsoft.Json;
using SWLOR.Game.Server.Service.ConversationService;

namespace SWLOR.Toolset.Domain.Conversations
{
    /// <summary>One line of dialogue matching a search, with enough context to open it.</summary>
    public sealed record DialogueHit(string ResRef, DlgNodeKind Kind, int Index, string Text)
    {
        /// <summary>"NPC" or "Player", for the result row.</summary>
        public string Speaker => Kind == DlgNodeKind.Entry ? "NPC" : "Player";

        /// <summary>The line trimmed for a result list.</summary>
        public string Preview => Text.Length <= 110 ? Text : Text[..107].TrimEnd() + "…";
    }

    /// <summary>
    /// Full-text search across every conversation in the module — "who says <em>Veldite</em>?".
    /// </summary>
    /// <remarks>
    /// The existing module search indexes names, tags and resrefs, which is the right index for
    /// finding a blueprint and the wrong one for finding a line. Nothing in the toolset could answer
    /// a question about spoken words before this, and it is the question a writer asks most: where
    /// did we already mention this thing, and did we spell it the same way.
    /// <para>
    /// Deliberately not cached. Reading 609 files costs a second or so, and dialogue is edited while
    /// the search is open — a stale index would quietly answer the wrong question.
    /// </para>
    /// </remarks>
    public static class DialogueSearch
    {
        /// <summary>
        /// One conversation's worth of matches, ordered by position, then every other conversation
        /// with a match — bounded by unique conversations rather than by raw line count.
        /// </summary>
        /// <param name="dialogDirectory">The module's <c>dlg</c> folder.</param>
        /// <param name="query">Case-insensitive substring. Blank returns nothing.</param>
        /// <param name="limit">
        /// Stops after this many <em>conversations</em> have matched, so a common word cannot hang
        /// the panel. A conversation with several matching lines still only counts once - otherwise a
        /// handful of early, heavily-matching dialogs in this alphabetically ordered scan could
        /// exhaust the limit before it ever reaches the rest of the directory. Only the first
        /// matching line of a conversation is kept; that is all the one caller today (Module
        /// Contents) reads before collapsing hits down to resrefs.
        /// </param>
        /// <param name="cancellationToken">
        /// Abandons the scan. This reads every conversation in the module, so a caller searching as
        /// the builder types needs a way to drop a query the next keystroke has already replaced
        /// rather than paying for all of them.
        /// </param>
        /// <param name="openDocument">
        /// Consulted by resref before a conversation is loaded from disk. An open editor with unsaved
        /// changes should return its live in-memory document here, so the search matches what the
        /// builder is looking at rather than what was last saved; returning null (the default) falls
        /// back to <see cref="DlgDocument.Load"/>. Wired from an open-editors registry such as
        /// <c>EditorService</c>'s conversation tab map.
        /// </param>
        /// <param name="conversationGraphDirectory">
        /// The server's graph-native <c>ConversationData</c> directory. When a graph and legacy DLG
        /// share a resref, the graph is authoritative and the stale migration source is not searched.
        /// </param>
        /// <param name="openGraph">
        /// A deep snapshot of an open graph-native editor, when available, so unsaved text participates
        /// in search without exposing a mutable UI-owned graph to the worker thread.
        /// </param>
        public static IReadOnlyList<DialogueHit> Search(
            string dialogDirectory,
            string query,
            int limit = 300,
            CancellationToken cancellationToken = default,
            Func<string, DlgDocument?>? openDocument = null,
            string? conversationGraphDirectory = null,
            Func<string, ConversationGraph?>? openGraph = null)
        {
            var hits = new List<DialogueHit>();
            if (string.IsNullOrWhiteSpace(query))
                return hits;

            var graphs = ConversationFiles(
                conversationGraphDirectory,
                ".conversation.json");
            var dialogs = ConversationFiles(dialogDirectory, ".dlg.json");
            var resRefs = graphs.Keys.Concat(dialogs.Keys)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(resRef => resRef, StringComparer.OrdinalIgnoreCase);

            var matchedConversations = 0;
            foreach (var resRef in resRefs)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (matchedConversations >= limit)
                    break;

                DialogueHit? hit;
                if (graphs.TryGetValue(resRef, out var graphPath))
                {
                    var graph = openGraph?.Invoke(resRef) ?? TryLoadGraph(graphPath);
                    hit = graph == null ? null : FindFirstMatch(graph, resRef, query);
                }
                else
                {
                    var document = openDocument?.Invoke(resRef) ?? TryLoadDialog(dialogs[resRef]);
                    hit = document == null ? null : FindFirstMatch(document, resRef, query);
                }

                if (hit != null)
                {
                    hits.Add(hit);
                    matchedConversations++;
                }
            }

            return hits;
        }

        private static IReadOnlyDictionary<string, string> ConversationFiles(
            string? directory,
            string suffix)
        {
            if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            return Directory.EnumerateFiles(directory, "*" + suffix)
                .ToDictionary(
                    path => Path.GetFileName(path)[..^suffix.Length],
                    path => path,
                    StringComparer.OrdinalIgnoreCase);
        }

        private static ConversationGraph? TryLoadGraph(string path)
        {
            try
            {
                return JsonConvert.DeserializeObject<ConversationGraph>(File.ReadAllText(path));
            }
            catch (Exception)
            {
                // A malformed graph remains visible in Module Contents, but it must not prevent a
                // search across every other authored conversation.
                return null;
            }
        }

        private static DlgDocument? TryLoadDialog(string path)
        {
            try
            {
                return DlgDocument.Load(path);
            }
            catch (Exception)
            {
                // One unreadable legacy exception must not stop the rest of the corpus.
                return null;
            }
        }

        /// <summary>
        /// The first line in <paramref name="document"/> containing <paramref name="query"/>,
        /// entries before replies, or null when nothing matches. One hit is enough to name the
        /// conversation as a match; the caller collapses hits to resrefs anyway.
        /// </summary>
        private static DialogueHit? FindFirstMatch(DlgDocument document, string resRef, string query)
        {
            foreach (var kind in new[] { DlgNodeKind.Entry, DlgNodeKind.Reply })
            {
                var nodes = kind == DlgNodeKind.Entry ? document.Entries : document.Replies;
                for (var i = 0; i < nodes.Count; i++)
                {
                    var text = nodes[i].Text;
                    if (string.IsNullOrEmpty(text)
                        || !text.Contains(query, StringComparison.OrdinalIgnoreCase))
                        continue;

                    return new DialogueHit(resRef, kind, i, text);
                }
            }

            return null;
        }

        private static DialogueHit? FindFirstMatch(
            ConversationGraph graph,
            string resRef,
            string query)
        {
            var index = 0;
            foreach (var node in graph.Nodes.Values)
            {
                var text = string.Concat(node.Text.Select(block => block.Text));
                if (!string.IsNullOrEmpty(text) &&
                    text.Contains(query, StringComparison.OrdinalIgnoreCase))
                {
                    return new DialogueHit(resRef, DlgNodeKind.Entry, index, text);
                }

                index++;
            }

            index = 0;
            foreach (var choice in graph.Choices.Values)
            {
                var text = choice.Text?.Text ?? string.Empty;
                if (!string.IsNullOrEmpty(text) &&
                    text.Contains(query, StringComparison.OrdinalIgnoreCase))
                {
                    return new DialogueHit(resRef, DlgNodeKind.Reply, index, text);
                }

                index++;
            }

            return null;
        }
    }
}
