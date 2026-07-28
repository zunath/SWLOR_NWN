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
        IncludeNewer,

        /// <summary>
        /// The source has no entry point (it is an include), yet a same-resref .ncs from its former
        /// behavior still exists - obsolete executable code the packer would ship verbatim.
        /// </summary>
        ObsoleteIncludeArtifact,

        /// <summary>
        /// The .ncs mtime still looks fresh against every source mtime, but the combined content of
        /// the entry point and its transitive includes no longer matches the fingerprint recorded
        /// the last time that comparison held - some source file was swapped for different content
        /// while its mtime was preserved, or a coarse filesystem clock let the writes land in the
        /// same timestamp bucket.
        /// </summary>
        SourceReplaced
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
            StaleReason.ObsoleteIncludeArtifact =>
                $"{ResRef}.ncs is obsolete - {ResRef}.nss is an include with no entry point",
            StaleReason.SourceReplaced =>
                $"{ResRef}.nss or one of its includes changed without a modification time moving past {ResRef}.ncs's",
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
            var includes = new List<string>();

            foreach (var path in Directory.EnumerateFiles(_nssDirectory, "*.nss"))
            {
                var resRef = Path.GetFileNameWithoutExtension(path);
                sourceTimes[resRef] = File.GetLastWriteTimeUtc(path);

                try
                {
                    if (IsEntryPoint(ScriptTextDocument.Load(path).Text))
                        entryPoints.Add(resRef);
                    else
                        includes.Add(resRef);
                }
                catch (IOException)
                {
                    // Unreadable: skip rather than report a false positive.
                }
            }

            var stale = new List<StaleScript>();
            var fingerprints = ScriptFingerprintStore.Load(_ncsDirectory);

            // An include with a same-resref artifact is obsolete executable code from the source's
            // former behavior. Timestamps cannot flag it (the scan deliberately excludes includes
            // from the freshness dimension), yet the packer ships every .ncs verbatim - so it must
            // surface here, where the pre-pack readiness check will see it and run Build All, whose
            // include purge removes the artifact.
            foreach (var resRef in includes)
            {
                if (File.Exists(Path.Combine(_ncsDirectory, resRef + ".ncs")))
                    stale.Add(new StaleScript(resRef, StaleReason.ObsoleteIncludeArtifact, null));
            }

            foreach (var resRef in entryPoints)
            {
                var compiled = Path.Combine(_ncsDirectory, resRef + ".ncs");
                if (!File.Exists(compiled))
                {
                    stale.Add(new StaleScript(resRef, StaleReason.NeverCompiled, null));
                    continue;
                }

                var compiledTime = File.GetLastWriteTimeUtc(compiled);
                var hasOwnTime = sourceTimes.TryGetValue(resRef, out var ownTime);

                if (hasOwnTime && ownTime > compiledTime)
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
                {
                    stale.Add(new StaleScript(resRef, StaleReason.IncludeNewer, trigger));
                    continue;
                }

                // Every mtime says fresh, which is not enough on its own: a source OR include
                // replaced while preserving its mtime, or writes landing in the same coarse
                // filesystem timestamp bucket, would pass those checks despite shipping stale
                // bytecode. The persisted fingerprint disambiguates by hashing the entry point
                // together with its whole transitive include set, compared against the baseline
                // recorded the last time the artifact passed with this same compiled mtime - but
                // only once one exists. A script's first sight (no cache entry, e.g. right after
                // a fresh checkout) trusts the mtime comparisons alone, so an untouched module
                // does not report everything stale.
                if (hasOwnTime && TryHashSources(resRef, graph) is { } currentHash)
                {
                    if (fingerprints.TryGet(resRef, out var known) &&
                        known.CompiledMTimeUtc == compiledTime &&
                        known.SourceHash != currentHash)
                    {
                        stale.Add(new StaleScript(resRef, StaleReason.SourceReplaced, null));
                        continue;
                    }

                    fingerprints.Record(resRef, new ScriptFingerprint(ownTime, currentHash, compiledTime));
                }
            }

            fingerprints.SaveIfDirty();
            return stale;
        }

        /// <summary>
        /// Hashes an entry point's source together with every transitive include (sorted, with the
        /// resref folded in so file boundaries cannot alias), or null if any file cannot be read -
        /// a swap inside an include must invalidate the dependent exactly as one in its own source
        /// does, because both compile into the same bytecode.
        /// </summary>
        private string? TryHashSources(string resRef, ScriptIncludeGraph graph)
        {
            try
            {
                using var hash = System.Security.Cryptography.IncrementalHash.CreateHash(
                    System.Security.Cryptography.HashAlgorithmName.SHA256);

                AppendFile(hash, resRef);
                foreach (var include in graph.TransitiveIncludes(resRef)
                             .OrderBy(name => name, StringComparer.OrdinalIgnoreCase))
                {
                    // An include named by the graph but absent on disk contributes only its name:
                    // the compiler would fail on it anyway, and the mtime dimension owns that story.
                    var path = Path.Combine(_nssDirectory, include + ".nss");
                    if (File.Exists(path))
                        AppendFile(hash, include);
                }

                return Convert.ToHexString(hash.GetHashAndReset());
            }
            catch (IOException)
            {
                return null;
            }
        }

        private void AppendFile(System.Security.Cryptography.IncrementalHash hash, string resRef)
        {
            hash.AppendData(System.Text.Encoding.UTF8.GetBytes(resRef + "\n"));
            hash.AppendData(File.ReadAllBytes(Path.Combine(_nssDirectory, resRef + ".nss")));
        }
    }
}
