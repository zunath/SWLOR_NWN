using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Native
{
    /// <summary>
    /// One sender's invalid UTF-8 repairs and rejections, gathered until the next report is written.
    /// </summary>
    public class ClientJsonRepairReport
    {
        private readonly List<string> _samples = new();

        public ClientJsonRepairReport(string account, uint player)
        {
            Account = account;
            Player = player;
        }

        /// <summary>The sender's account name, or "client" and its id when the sender was not found.</summary>
        public string Account { get; }

        /// <summary>The sender's character when the message arrived.</summary>
        public uint Player { get; }

        public int Replaced { get; private set; }
        public int Rejected { get; private set; }

        /// <summary>The first distinct repaired strings, so a report shows which text arrived garbled.</summary>
        public IReadOnlyList<string> Samples => _samples;

        /// <summary>Adds one payload's repairs, keeping at most <paramref name="maxSamples"/> distinct repaired strings.</summary>
        public void Add(int replaced, bool rejected, IEnumerable<string> repairedText, int maxSamples)
        {
            Replaced += replaced;
            if (rejected)
                Rejected++;

            foreach (var text in repairedText)
            {
                if (_samples.Count >= maxSamples)
                    break;
                if (!_samples.Contains(text))
                    _samples.Add(text);
            }
        }

        /// <summary>The log line for this report; <paramref name="character"/> names the sender's character.</summary>
        public string Describe(string character)
        {
            var line = $"Client NUI data from {Account} ({character}): " +
                       $"replaced {Replaced} invalid UTF-8 byte(s), rejected {Rejected} unreadable payload(s).";
            if (_samples.Count == 0)
                return line;

            // Quote the text without letting it break the log line.
            var samples = _samples.Select(text => "\"" + new string(text.Select(character =>
                char.IsControl(character) || character == '"' ? '?' : character).ToArray()) + "\"");
            return $"{line} Repaired text: {string.Join(", ", samples)}";
        }
    }
}
