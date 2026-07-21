using System;
using System.Collections.Generic;
using System.IO;

namespace SWLOR.ContentBuilder.Services
{
    /// <summary>One resolved (or unresolved) nwn.ini [Alias] entry for the Settings dialog's
    /// read-only derived-paths panel.</summary>
    public sealed class NwnAliasResolution
    {
        public string Alias { get; init; }
        public string Path { get; init; }

        /// <summary>True if nwn.ini's [Alias] section explicitly defines this alias; false means the
        /// value is the NWN convention fallback (&lt;user directory&gt;\&lt;lowercase alias&gt;).</summary>
        public bool FoundInIni { get; init; }

        /// <summary>True if the resolved directory actually exists on disk.</summary>
        public bool Exists { get; init; }
    }
}
