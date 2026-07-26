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
        /// Every line containing <paramref name="query"/>, ordered by conversation then position.
        /// </summary>
        /// <param name="dialogDirectory">The module's <c>dlg</c> folder.</param>
        /// <param name="query">Case-insensitive substring. Blank returns nothing.</param>
        /// <param name="limit">Stops after this many hits, so a common word cannot hang the panel.</param>
        public static IReadOnlyList<DialogueHit> Search(string dialogDirectory, string query, int limit = 300)
        {
            var hits = new List<DialogueHit>();
            if (string.IsNullOrWhiteSpace(query) || !Directory.Exists(dialogDirectory))
                return hits;

            var files = Directory.EnumerateFiles(dialogDirectory, "*.json")
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase);

            foreach (var path in files)
            {
                DlgDocument document;
                try
                {
                    document = DlgDocument.Load(path);
                }
                catch (Exception)
                {
                    // One unreadable conversation must not stop the search over the other 608.
                    continue;
                }

                var resRef = Path.GetFileName(path).Replace(".dlg.json", string.Empty, StringComparison.OrdinalIgnoreCase);

                foreach (var kind in new[] { DlgNodeKind.Entry, DlgNodeKind.Reply })
                {
                    var nodes = kind == DlgNodeKind.Entry ? document.Entries : document.Replies;
                    for (var i = 0; i < nodes.Count; i++)
                    {
                        var text = nodes[i].Text;
                        if (string.IsNullOrEmpty(text)
                            || !text.Contains(query, StringComparison.OrdinalIgnoreCase))
                            continue;

                        hits.Add(new DialogueHit(resRef, kind, i, text));
                        if (hits.Count >= limit)
                            return hits;
                    }
                }
            }

            return hits;
        }
    }
}
