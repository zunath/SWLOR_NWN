using System.Globalization;
using System.Text;

namespace SWLOR.Game.Server.Service.CombatRecorderService
{
    public static class CombatRecordFileNames
    {
        public const string DirectoryName = "CombatRecordings";
        public const string EventsExtension = ".jsonl";
        public const string SummaryExtension = ".summary.json";
        private const int MaximumLabelLength = 48;
        private const string DefaultLabel = "recording";

        /// <summary>
        /// Builds "&lt;utc-start&gt;-&lt;label&gt;", keeping only file-name-safe label characters. The start
        /// time keeps milliseconds so a recording restarted within the same second gets new files.
        /// </summary>
        public static string BuildBaseName(DateTime startedUtc, string label)
        {
            var timestamp = startedUtc.ToString("yyyyMMdd'T'HHmmssfff'Z'", CultureInfo.InvariantCulture);
            return $"{timestamp}-{SanitizeLabel(label)}";
        }

        public static string SanitizeLabel(string label)
        {
            if (string.IsNullOrWhiteSpace(label))
                return DefaultLabel;

            var builder = new StringBuilder(Math.Min(label.Length, MaximumLabelLength));
            var previousWasSeparator = false;
            foreach (var character in label.Trim())
            {
                if (builder.Length >= MaximumLabelLength)
                    break;

                if (char.IsAsciiLetterOrDigit(character) || character == '_')
                {
                    builder.Append(character);
                    previousWasSeparator = false;
                }
                else if (!previousWasSeparator && builder.Length > 0)
                {
                    builder.Append('-');
                    previousWasSeparator = true;
                }
            }

            var sanitized = builder.ToString().TrimEnd('-');
            return sanitized.Length == 0 ? DefaultLabel : sanitized;
        }

        /// <summary>
        /// The recordings directory under the server's app log directory. The log directory
        /// setting ends with a separator, matching the Serilog logger paths.
        /// </summary>
        public static string GetDirectory(string logDirectory)
        {
            return (logDirectory ?? string.Empty) + DirectoryName + "/";
        }
    }
}
