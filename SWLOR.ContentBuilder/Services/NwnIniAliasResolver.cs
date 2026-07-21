using System;
using System.Collections.Generic;
using System.IO;

namespace SWLOR.ContentBuilder.Services
{
    /// <summary>
    /// Parses nwn.ini's [Alias] section and resolves the well-known aliases Content Builder cares
    /// about (MODULES, ERF, HAK, TLK, OVERRIDE). nwn.ini is not full INI syntax -- no quoting, no
    /// inline comments, just verbatim "KEY=value" lines under a "[Alias]" header (confirmed against a
    /// real NWN:EE nwn.ini) -- so parsing is a simple line scan, not a general INI library.
    ///
    /// An alias missing from the file falls back to the NWN convention of
    /// &lt;user directory&gt;\&lt;lowercase alias&gt; (e.g. "erf" for ERF) -- matching what NWN:EE
    /// itself uses/creates when an alias has no explicit override. Never throws: a missing/unreadable
    /// nwn.ini resolves every alias via the fallback convention instead of surfacing an error.
    /// </summary>
    public static class NwnIniAliasResolver
    {
        public static readonly string[] WellKnownAliases = { "MODULES", "ERF", "HAK", "TLK", "OVERRIDE" };

        /// <summary>Resolves every well-known alias against the given NWN user directory, for display
        /// in the Settings dialog's derived-paths panel.</summary>
        public static List<NwnAliasResolution> Resolve(string nwnUserDirectory)
        {
            var iniAliases = ReadAliasSection(nwnUserDirectory);
            var results = new List<NwnAliasResolution>();

            foreach (var alias in WellKnownAliases)
            {
                var path = ResolveFromIniOrFallback(nwnUserDirectory, iniAliases, alias, out var foundInIni);
                var exists = !string.IsNullOrEmpty(path) && Directory.Exists(path);
                results.Add(new NwnAliasResolution { Alias = alias, Path = path, FoundInIni = foundInIni, Exists = exists });
            }

            return results;
        }

        /// <summary>Resolves a single alias's directory (e.g. "MODULES"), or null if it can't be
        /// determined at all (no NWN user directory configured). Used by build actions to find their
        /// copy/default-output destination without walking the whole panel list.</summary>
        public static string ResolveSingle(string nwnUserDirectory, string alias)
        {
            if (string.IsNullOrEmpty(nwnUserDirectory)) return null;

            var iniAliases = ReadAliasSection(nwnUserDirectory);
            return ResolveFromIniOrFallback(nwnUserDirectory, iniAliases, alias, out _);
        }

        private static string ResolveFromIniOrFallback(
            string nwnUserDirectory,
            Dictionary<string, string> iniAliases,
            string alias,
            out bool foundInIni)
        {
            if (iniAliases.TryGetValue(alias, out var iniPath) && !string.IsNullOrWhiteSpace(iniPath))
            {
                foundInIni = true;
                return iniPath;
            }

            foundInIni = false;
            return string.IsNullOrEmpty(nwnUserDirectory)
                ? string.Empty
                : Path.Combine(nwnUserDirectory, alias.ToLowerInvariant());
        }

        private static Dictionary<string, string> ReadAliasSection(string nwnUserDirectory)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrEmpty(nwnUserDirectory)) return result;

            var iniPath = Path.Combine(nwnUserDirectory, "nwn.ini");
            if (!File.Exists(iniPath)) return result;

            string[] lines;
            try
            {
                lines = File.ReadAllLines(iniPath);
            }
            catch
            {
                return result;
            }

            var inAliasSection = false;
            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();
                if (line.Length == 0) continue;

                if (line.StartsWith('[') && line.EndsWith(']'))
                {
                    inAliasSection = string.Equals(line.Trim('[', ']').Trim(), "Alias", StringComparison.OrdinalIgnoreCase);
                    continue;
                }

                if (!inAliasSection) continue;

                var eq = line.IndexOf('=');
                if (eq <= 0) continue;

                var key = line[..eq].Trim();
                var value = line[(eq + 1)..].Trim();
                if (key.Length > 0)
                    result[key] = value;
            }

            return result;
        }
    }
}
