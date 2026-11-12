using System.Globalization;

namespace SWLOR.Game.Server.Core
{
    /// <summary>
    /// One digest of reload-relevant state: how many entries it has and a stable hash of their content
    /// (<see cref="ReloadDigests"/>, <see cref="WorldDigest"/>).
    /// </summary>
    /// <param name="Kind"><c>cache</c> (rebuilt by the rerun handlers; compared with the boot's), <c>adopted</c> (rebuilt by an
    /// adoption step; compared with the old build's at quiescence) or <c>world</c> (read from the engine; compared with the old
    /// build's at quiescence).</param>
    /// <param name="Name">What was digested: a class's full name, or <c>world.*</c>.</param>
    /// <param name="Count">The number of entries (top-level collection sizes summed, a plain value counting one).</param>
    /// <param name="Hash">The content hash, 16 hexadecimal digits.</param>
    public sealed record ReloadDigestLine(string Kind, string Name, long Count, string Hash)
    {
        /// <summary>What each server log line of a digest starts with; <c>scripts/run-hot-reload-check.sh</c> reads these lines.</summary>
        public const string LogPrefix = "SWLOR_RELOAD_DIGEST";

        /// <summary>The server log line: generation, phase (<c>boot</c>, <c>quiesce</c> or <c>reload</c>), kind, name, count and hash.</summary>
        public string Format(long generation, string phase)
        {
            return string.Create(CultureInfo.InvariantCulture,
                $"{LogPrefix} generation={generation} phase={phase} kind={Kind} name={Name} count={Count} hash={Hash}");
        }

        /// <summary>True when <paramref name="other"/> has the same kind, name, count and hash.</summary>
        public bool Matches(ReloadDigestLine other)
        {
            return other != null && Kind == other.Kind && Name == other.Name && Count == other.Count && Hash == other.Hash;
        }
    }
}
