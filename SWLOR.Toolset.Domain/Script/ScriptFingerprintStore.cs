using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace SWLOR.Toolset.Domain.Script
{
    /// <summary>
    /// One entry point's last confirmed-fresh signature: a content hash over its source plus every
    /// transitive include, taken at the moment its compiled artifact last passed all the timestamp
    /// checks. Recording only at that moment (not on every scan) is what lets a later scan notice
    /// any of those files swapped back onto the same mtime - the persisted hash still names the old
    /// content even though no mtime has moved. <c>SourceMTimeUtc</c> is informational; only the
    /// compiled mtime gates the comparison, since a content change under ANY unchanged-looking
    /// timestamp is exactly what the hash exists to catch.
    /// </summary>
    internal sealed record ScriptFingerprint(DateTime SourceMTimeUtc, string SourceHash, DateTime CompiledMTimeUtc);

    /// <summary>
    /// A JSON sidecar beside the compiled scripts, remembering each entry point's
    /// <see cref="ScriptFingerprint"/> across scans. <see cref="ScriptStalenessScanner"/> is
    /// constructed fresh on every call (see <c>ScriptCompileService.ScanStale</c>), so the mtime-vs-hash
    /// comparison it needs in order to catch a same-timestamp source swap has nowhere else to live.
    /// Not a module resource - <c>ModulePacker</c> copies <c>*.ncs</c>/<c>*.nss</c> by extension, so this
    /// file never ships - and a fresh checkout with none present is exactly the "first sight" case the
    /// scanner falls back to timestamp-only comparison for.
    /// </summary>
    internal sealed class ScriptFingerprintStore
    {
        private const string FileName = ".script-staleness-cache.json";

        private readonly string _path;
        private readonly Dictionary<string, ScriptFingerprint> _entries;
        private bool _dirty;

        private ScriptFingerprintStore(string path, Dictionary<string, ScriptFingerprint> entries)
        {
            _path = path;
            _entries = entries;
        }

        /// <summary>Loads the cache from the ncs directory, or starts an empty one if it is missing or unreadable.</summary>
        public static ScriptFingerprintStore Load(string ncsDirectory)
        {
            var path = Path.Combine(ncsDirectory, FileName);

            try
            {
                if (File.Exists(path))
                {
                    var loaded = JsonSerializer.Deserialize<Dictionary<string, ScriptFingerprint>>(
                        File.ReadAllText(path));
                    if (loaded != null)
                    {
                        return new ScriptFingerprintStore(
                            path, new Dictionary<string, ScriptFingerprint>(loaded, StringComparer.OrdinalIgnoreCase));
                    }
                }
            }
            catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
            {
                // A corrupt or unreadable cache is not a reason to fail the scan, or to lie about
                // staleness - every entry point just goes through "first sight" comparison this once.
            }

            return new ScriptFingerprintStore(path, new Dictionary<string, ScriptFingerprint>(StringComparer.OrdinalIgnoreCase));
        }

        public bool TryGet(string resRef, [NotNullWhen(true)] out ScriptFingerprint? fingerprint) =>
            _entries.TryGetValue(resRef, out fingerprint);

        /// <summary>
        /// Every resref this cache has ever confirmed as a sourced entry point - which is exactly
        /// what separates an artifact whose source was DELETED from the repository's intentional
        /// compiled-only scripts, which never had a source to fingerprint.
        /// </summary>
        public IReadOnlyCollection<string> TrackedResRefs => _entries.Keys;

        /// <summary>Drops one entry - its script no longer exists in any form worth remembering.</summary>
        public void Forget(string resRef)
        {
            if (_entries.Remove(resRef))
                _dirty = true;
        }

        /// <summary>Records (or replaces) the confirmed-fresh signature for one entry point.</summary>
        public void Record(string resRef, ScriptFingerprint fingerprint)
        {
            _entries[resRef] = fingerprint;
            _dirty = true;
        }

        /// <summary>Writes the cache back only if something changed, so a clean scan never touches disk.</summary>
        public void SaveIfDirty()
        {
            if (!_dirty)
                return;

            try
            {
                File.WriteAllText(_path, JsonSerializer.Serialize(_entries));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Best-effort: a scan that could not persist its cache still reported real results,
                // and next time will simply re-derive baselines it failed to save this time.
            }
        }
    }
}
