using SWLOR.Toolset.Domain.Documents;

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
        public static IReadOnlyList<DialogueHit> Search(
            string dialogDirectory,
            string query,
            int limit = 300,
            CancellationToken cancellationToken = default,
            Func<string, DlgDocument?>? openDocument = null)
        {
            var hits = new List<DialogueHit>();
            if (string.IsNullOrWhiteSpace(query) || !Directory.Exists(dialogDirectory))
                return hits;

            var files = Directory.EnumerateFiles(dialogDirectory, "*.json")
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase);

            var matchedConversations = 0;
            foreach (var path in files)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (matchedConversations >= limit)
                    break;

                var resRef = Path.GetFileName(path).Replace(".dlg.json", string.Empty, StringComparison.OrdinalIgnoreCase);

                var document = openDocument?.Invoke(resRef);
                if (document == null)
                {
                    try
                    {
                        document = DlgDocument.Load(path);
                    }
                    catch (Exception)
                    {
                        // One unreadable conversation must not stop the search over the other 608.
                        continue;
                    }
                }

                if (FindFirstMatch(document, resRef, query) is { } hit)
                {
                    hits.Add(hit);
                    matchedConversations++;
                }
            }

            return hits;
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
    }
}
