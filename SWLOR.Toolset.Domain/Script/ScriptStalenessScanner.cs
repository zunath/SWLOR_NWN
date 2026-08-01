using SWLOR.NWN.Formats.Common;

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
        SourceReplaced,

        /// <summary>
        /// The .nss this artifact was compiled from was deleted, yet the .ncs remains - orphaned
        /// bytecode the packer would ship verbatim, keeping removed script behavior alive in
        /// production. Only detectable through the fingerprint store: an artifact never
        /// fingerprinted never had a source here and is an intentional compiled-only script.
        /// </summary>
        SourceDeleted,

        /// <summary>
        /// The source names a direct or transitive include that is absent from the module. Existing
        /// bytecode cannot be proven to represent a source tree the compiler can still resolve.
        /// </summary>
        MissingInclude
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
            StaleReason.SourceDeleted =>
                $"{ResRef}.ncs is orphaned - its source {ResRef}.nss was deleted",
            StaleReason.MissingInclude =>
                $"{ResRef}.nss includes missing {TriggerResRef}.nss",
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
        private readonly Func<string, bool>? _externalIncludeExists;

        public ScriptStalenessScanner(
            string nssDirectory,
            string ncsDirectory,
            Func<string, bool>? externalIncludeExists = null)
        {
            _nssDirectory = nssDirectory;
            _ncsDirectory = ncsDirectory;
            _externalIncludeExists = externalIncludeExists;
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

            using var moduleWriteLock = ModuleWriteLock.Acquire(ModuleRoot());
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
                var missingInclude = graph.TransitiveIncludes(resRef).FirstOrDefault(include =>
                    !IncludeExists(include));
                if (missingInclude != null)
                {
                    stale.Add(new StaleScript(resRef, StaleReason.MissingInclude, missingInclude));
                    continue;
                }

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

            // The deleted-source dimension: an artifact whose .nss disappeared has no timestamp
            // left to compare, so only the fingerprint store's memory of "this WAS a sourced
            // entry point here" can flag it. Entries whose artifact is also gone are dropped -
            // there is nothing left to guard.
            foreach (var resRef in fingerprints.TrackedResRefs.ToList())
            {
                if (sourceTimes.ContainsKey(resRef))
                    continue;

                if (File.Exists(Path.Combine(_ncsDirectory, resRef + ".ncs")))
                    stale.Add(new StaleScript(resRef, StaleReason.SourceDeleted, null));
                else
                    fingerprints.Forget(resRef);
            }

            fingerprints.SaveIfDirty();
            return stale;
        }

        /// <summary>
        /// Records the source/include fingerprint that produced a newly installed artifact.
        /// Compilation can replace bytecode inside the same filesystem timestamp bucket as the
        /// previous artifact, so waiting for a later timestamp-gated scan can leave the old source
        /// hash in place and falsely report the successful rebuild as <see cref="StaleReason.SourceReplaced"/>.
        /// </summary>
        public bool RecordSuccessfulCompile(string resRef)
        {
            if (string.IsNullOrWhiteSpace(resRef) ||
                !Directory.Exists(_nssDirectory) ||
                !Directory.Exists(_ncsDirectory))
            {
                return false;
            }

            using var moduleWriteLock = ModuleWriteLock.Acquire(ModuleRoot());
            var source = Path.Combine(_nssDirectory, resRef + ".nss");
            var compiled = Path.Combine(_ncsDirectory, resRef + ".ncs");
            if (!File.Exists(source) || !File.Exists(compiled))
                return false;

            try
            {
                if (!IsEntryPoint(ScriptTextDocument.Load(source).Text))
                    return false;

                var graph = ScriptIncludeGraph.Build(_nssDirectory);
                var sourceHash = TryHashSources(resRef, graph);
                if (sourceHash == null)
                    return false;

                var fingerprints = ScriptFingerprintStore.Load(_ncsDirectory);
                fingerprints.Record(resRef, new ScriptFingerprint(
                    File.GetLastWriteTimeUtc(source),
                    sourceHash,
                    File.GetLastWriteTimeUtc(compiled)));
                fingerprints.SaveIfDirty();
                return true;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                return false;
            }
        }

        /// <summary>
        /// Deletes every orphaned artifact the fingerprint store knows about - a .ncs whose .nss
        /// was deleted after being scanned as a sourced entry point - and forgets the entries.
        /// Build All runs this so a <see cref="StaleReason.SourceDeleted"/> finding is actually
        /// resolvable in-tool: Build All's compile loop iterates existing sources and could never
        /// touch an artifact whose source is gone. Compiled-only artifacts that never had a source
        /// here were never fingerprinted and are untouched.
        /// </summary>
        /// <returns>The resrefs whose artifacts were removed.</returns>
        public IReadOnlyList<string> PurgeOrphanedArtifacts()
        {
            var purged = new List<string>();
            if (!Directory.Exists(_ncsDirectory))
                return purged;

            using var moduleWriteLock = ModuleWriteLock.Acquire(ModuleRoot());
            var fingerprints = ScriptFingerprintStore.Load(_ncsDirectory);
            foreach (var resRef in fingerprints.TrackedResRefs.ToList())
            {
                if (File.Exists(Path.Combine(_nssDirectory, resRef + ".nss")))
                    continue;

                var artifact = Path.Combine(_ncsDirectory, resRef + ".ncs");
                if (File.Exists(artifact))
                {
                    File.Delete(artifact);
                    purged.Add(resRef);
                }

                fingerprints.Forget(resRef);
            }

            fingerprints.SaveIfDirty();
            return purged;
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
                    if (File.Exists(Path.Combine(_nssDirectory, include + ".nss")))
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

        private bool IncludeExists(string resRef)
        {
            if (File.Exists(Path.Combine(_nssDirectory, resRef + ".nss")))
                return true;

            try
            {
                return _externalIncludeExists?.Invoke(resRef) == true;
            }
            catch
            {
                return false;
            }
        }

        private string ModuleRoot() =>
            Directory.GetParent(Path.GetFullPath(_nssDirectory))?.FullName
            ?? throw new InvalidOperationException(
                $"Could not determine the module root containing '{_nssDirectory}'.");
    }
}
