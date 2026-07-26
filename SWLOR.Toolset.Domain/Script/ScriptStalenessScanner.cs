namespace SWLOR.Toolset.Domain.Script
{
    /// <summary>Why a compiled script is out of date.</summary>
    public enum StaleReason
    {
        /// <summary>No .ncs exists for this source at all.</summary>
        NeverCompiled,

        /// <summary>The .ncs is older than its own .nss.</summary>
        SourceNewer,

        /// <summary>The .ncs is older than something the source includes, transitively.</summary>
        IncludeNewer
    }

    /// <summary>One compiled script that would ship stale.</summary>
    /// <param name="ResRef">The script.</param>
    /// <param name="Reason">Why it is stale.</param>
    /// <param name="TriggerResRef">The newer file, when the reason is an include.</param>
    public sealed record StaleScript(string ResRef, StaleReason Reason, string? TriggerResRef)
    {
        public string Describe() => Reason switch
        {
            StaleReason.NeverCompiled => $"{ResRef}.nss has never been compiled",
            StaleReason.SourceNewer => $"{ResRef}.ncs is older than {ResRef}.nss",
            _ => $"{ResRef}.ncs is older than included {TriggerResRef}.nss"
        };
    }

    /// <summary>
    /// Finds compiled scripts that no longer match their sources.
    /// </summary>
    /// <remarks>
    /// This exists because the pack pipeline cannot see the problem. <c>ModulePacker</c> copies
    /// <c>./nss/</c> and <c>./ncs/</c> verbatim into the ERF, so an edited source with a stale
    /// artifact ships silently and the game keeps running the old bytecode. The include dimension is
    /// the part no one tracks by hand: editing one <c>_inc</c> header invalidates every dependent.
    ///
    /// Scripts with no <c>main()</c> are includes and produce no <c>.ncs</c> by design, so their
    /// absence is not staleness. That is decided by parsing for an entry point rather than by
    /// "has no .ncs" — <c>dmfi_dmw_inc</c> is an include that nonetheless has a committed 184-byte
    /// artifact, so the file-existence heuristic is wrong in both directions.
    /// </remarks>
    public sealed class ScriptStalenessScanner
    {
        private readonly string _nssDirectory;
        private readonly string _ncsDirectory;

        public ScriptStalenessScanner(string nssDirectory, string ncsDirectory)
        {
            _nssDirectory = nssDirectory;
            _ncsDirectory = ncsDirectory;
        }

        /// <summary>True when the source declares an entry point and so should produce a .ncs.</summary>
        public static bool IsEntryPoint(string source)
        {
            var outline = ScriptOutline.Build(source);
            return outline.Functions.Any(f =>
                f.IsDefinition &&
                (f.Name.Equals("main", StringComparison.Ordinal) ||
                 f.Name.Equals("StartingConditional", StringComparison.Ordinal)));
        }

        /// <summary>Every stale compiled script in the module.</summary>
        public IReadOnlyList<StaleScript> Scan()
        {
            if (!Directory.Exists(_nssDirectory))
                return Array.Empty<StaleScript>();

            var graph = ScriptIncludeGraph.Build(_nssDirectory);
            var sourceTimes = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
            var entryPoints = new List<string>();

            foreach (var path in Directory.EnumerateFiles(_nssDirectory, "*.nss"))
            {
                var resRef = Path.GetFileNameWithoutExtension(path);
                sourceTimes[resRef] = File.GetLastWriteTimeUtc(path);

                try
                {
                    if (IsEntryPoint(ScriptTextDocument.Load(path).Text))
                        entryPoints.Add(resRef);
                }
                catch (IOException)
                {
                    // Unreadable: skip rather than report a false positive.
                }
            }

            var stale = new List<StaleScript>();

            foreach (var resRef in entryPoints)
            {
                var compiled = Path.Combine(_ncsDirectory, resRef + ".ncs");
                if (!File.Exists(compiled))
                {
                    stale.Add(new StaleScript(resRef, StaleReason.NeverCompiled, null));
                    continue;
                }

                var compiledTime = File.GetLastWriteTimeUtc(compiled);

                if (sourceTimes.TryGetValue(resRef, out var ownTime) && ownTime > compiledTime)
                {
                    stale.Add(new StaleScript(resRef, StaleReason.SourceNewer, null));
                    continue;
                }

                // The include dimension: any header in the transitive set being newer is enough.
                string? trigger = null;
                foreach (var include in graph.TransitiveIncludes(resRef))
                {
                    if (sourceTimes.TryGetValue(include, out var includeTime) && includeTime > compiledTime)
                    {
                        trigger = include;
                        break;
                    }
                }

                if (trigger != null)
                    stale.Add(new StaleScript(resRef, StaleReason.IncludeNewer, trigger));
            }

            return stale;
        }
    }
}
