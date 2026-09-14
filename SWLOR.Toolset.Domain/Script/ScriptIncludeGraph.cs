namespace SWLOR.Toolset.Domain.Script
{
    /// <summary>
    /// Which script includes which, across a whole module directory, in both directions.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The reverse edges are the load-bearing half. <c>ModulePacker</c> copies <c>.nss</c> and
    /// <c>.ncs</c> verbatim and never compiles, so a source edited without recompiling ships stale
    /// bytecode silently — and editing an <c>_inc</c> header invalidates every script that includes
    /// it, transitively. This module has deep chains (the <c>dmfi_*</c> set alone has 10 headers),
    /// so "what else must I rebuild?" is not answerable by inspection.
    /// </para>
    /// <para>
    /// Cycles are tolerated rather than rejected: NWScript's compiler guards with a depth cap
    /// (<c>--max-include-depth</c>, default 16) instead of forbidding them, and a graph that threw on
    /// one would be useless on exactly the file the author is trying to fix. The traversal is
    /// visited-set based, so a cycle terminates instead of hanging.
    /// </para>
    /// </remarks>
    public sealed class ScriptIncludeGraph
    {
        /// <summary>Matches the compiler's own --max-include-depth default.</summary>
        public const int MaxIncludeDepth = 16;

        private readonly Dictionary<string, IReadOnlyList<string>> _includes;
        private readonly Dictionary<string, List<string>> _includedBy;

        private ScriptIncludeGraph(
            Dictionary<string, IReadOnlyList<string>> includes,
            Dictionary<string, List<string>> includedBy)
        {
            _includes = includes;
            _includedBy = includedBy;
        }

        /// <summary>Every script resref in the graph.</summary>
        public IReadOnlyCollection<string> ResRefs => _includes.Keys;

        /// <summary>Builds from a directory of .nss files.</summary>
        public static ScriptIncludeGraph Build(string nssDirectory)
        {
            var sources = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (Directory.Exists(nssDirectory))
            {
                foreach (var path in Directory.EnumerateFiles(nssDirectory, "*.nss"))
                {
                    try
                    {
                        sources[Path.GetFileNameWithoutExtension(path)] = ScriptTextDocument.Load(path).Text;
                    }
                    catch (IOException)
                    {
                        // A file that cannot be read simply has no edges; one locked file must not
                        // take out the whole graph.
                    }
                }
            }

            return BuildFrom(sources);
        }

        /// <summary>Builds from resref → source text. Split out so it is testable without a filesystem.</summary>
        public static ScriptIncludeGraph BuildFrom(IReadOnlyDictionary<string, string> sources)
        {
            var includes = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
            var includedBy = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            foreach (var (resRef, text) in sources)
            {
                var direct = ScriptOutline.Build(text).Includes
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                includes[resRef] = direct;

                foreach (var target in direct)
                {
                    if (!includedBy.TryGetValue(target, out var list))
                        includedBy[target] = list = new List<string>();

                    list.Add(resRef);
                }
            }

            return new ScriptIncludeGraph(includes, includedBy);
        }

        /// <summary>What <paramref name="resRef"/> includes directly.</summary>
        public IReadOnlyList<string> DirectIncludes(string resRef) =>
            _includes.TryGetValue(resRef, out var list) ? list : Array.Empty<string>();

        /// <summary>What includes <paramref name="resRef"/> directly.</summary>
        public IReadOnlyList<string> DirectDependents(string resRef) =>
            _includedBy.TryGetValue(resRef, out var list) ? list : Array.Empty<string>();

        /// <summary>Everything <paramref name="resRef"/> pulls in, transitively. Excludes itself.</summary>
        public IReadOnlyList<string> TransitiveIncludes(string resRef) =>
            Walk(resRef, DirectIncludes);

        /// <summary>
        /// Every script that would need recompiling if <paramref name="resRef"/> changed, transitively.
        /// Excludes itself.
        /// </summary>
        public IReadOnlyList<string> TransitiveDependents(string resRef) =>
            Walk(resRef, DirectDependents);

        /// <summary>True when following includes from <paramref name="resRef"/> returns to it.</summary>
        public bool HasCycle(string resRef) =>
            TransitiveIncludes(resRef).Contains(resRef, StringComparer.OrdinalIgnoreCase);

        private IReadOnlyList<string> Walk(string start, Func<string, IReadOnlyList<string>> next)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var result = new List<string>();

            // Depth is tracked alongside each node so a pathological graph terminates the same way
            // the compiler would, rather than fanning out indefinitely.
            var queue = new Queue<(string Node, int Depth)>();
            queue.Enqueue((start, 0));

            while (queue.Count > 0)
            {
                var (node, depth) = queue.Dequeue();
                if (depth >= MaxIncludeDepth)
                    continue;

                foreach (var neighbour in next(node))
                {
                    if (!seen.Add(neighbour))
                        continue;

                    result.Add(neighbour);
                    queue.Enqueue((neighbour, depth + 1));
                }
            }

            return result;
        }
    }
}
