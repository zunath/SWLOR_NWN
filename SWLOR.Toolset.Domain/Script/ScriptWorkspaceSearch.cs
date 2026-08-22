using SWLOR.Toolset.Domain.Script.Syntax;

namespace SWLOR.Toolset.Domain.Script
{
    public enum ScriptSearchMode
    {
        Identifier,
        Substring
    }

    public sealed record ScriptSearchResult(string ResRef, int Line, string LineText);

    /// <summary>
    /// Searches NWScript sources in a module directory.
    /// </summary>
    public sealed class ScriptWorkspaceSearch
    {
        private readonly string _nssDirectory;
        private readonly Func<string, string?>? _sourceOverlay;

        public ScriptWorkspaceSearch(
            string nssDirectory,
            Func<string, string?>? sourceOverlay = null)
        {
            _nssDirectory = nssDirectory;
            _sourceOverlay = sourceOverlay;
        }

        public IReadOnlyList<ScriptSearchResult> Search(string query, ScriptSearchMode mode)
        {
            if (string.IsNullOrWhiteSpace(query) || !Directory.Exists(_nssDirectory))
                return Array.Empty<ScriptSearchResult>();

            query = query.Trim();
            if (mode == ScriptSearchMode.Identifier && !ScriptNavigation.IsValidIdentifier(query))
                return Array.Empty<ScriptSearchResult>();

            var results = new List<ScriptSearchResult>();
            foreach (var path in Directory.EnumerateFiles(_nssDirectory, "*.nss")
                         .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase))
            {
                var resRef = Path.GetFileNameWithoutExtension(path);
                var source = _sourceOverlay?.Invoke(resRef) ?? ScriptTextDocument.Load(path).Text;
                if (mode == ScriptSearchMode.Identifier)
                    SearchIdentifier(source, resRef, query, results);
                else
                    SearchSubstring(source, resRef, query, results);
            }

            return results;
        }

        private static void SearchIdentifier(
            string source,
            string resRef,
            string query,
            List<ScriptSearchResult> results)
        {
            var lineIndex = LineIndex.Build(source);
            var seenLines = new HashSet<int>();

            foreach (var token in ScriptLexer.TokenizeCode(source))
            {
                if (token.Kind != ScriptTokenKind.Identifier ||
                    !token.Text(source).Equals(query.AsSpan(), StringComparison.Ordinal))
                    continue;

                var line = lineIndex.LineOf(token.Start);
                if (seenLines.Add(line))
                    results.Add(new ScriptSearchResult(resRef, line, lineIndex.TextOf(line)));
            }
        }

        private static void SearchSubstring(
            string source,
            string resRef,
            string query,
            List<ScriptSearchResult> results)
        {
            var lineIndex = LineIndex.Build(source);
            for (var line = 1; line <= lineIndex.Count; line++)
            {
                var text = lineIndex.TextOf(line);
                if (text.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                    results.Add(new ScriptSearchResult(resRef, line, text));
            }
        }

        private sealed class LineIndex
        {
            private readonly string _source;
            private readonly List<int> _starts;

            private LineIndex(string source, List<int> starts)
            {
                _source = source;
                _starts = starts;
            }

            public int Count => _starts.Count;

            public static LineIndex Build(string source)
            {
                var starts = new List<int> { 0 };
                for (var i = 0; i < source.Length; i++)
                {
                    if (source[i] == '\n')
                        starts.Add(i + 1);
                }

                if (source.Length > 0 && starts[^1] == source.Length)
                    starts.RemoveAt(starts.Count - 1);

                return new LineIndex(source, starts);
            }

            public int LineOf(int offset)
            {
                var index = _starts.BinarySearch(offset);
                return index >= 0 ? index + 1 : ~index;
            }

            public string TextOf(int line)
            {
                var start = _starts[line - 1];
                var end = line < _starts.Count ? _starts[line] - 1 : _source.Length;
                while (end > start && (_source[end - 1] == '\r' || _source[end - 1] == '\n'))
                    end--;

                return _source.Substring(start, end - start);
            }
        }
    }
}
