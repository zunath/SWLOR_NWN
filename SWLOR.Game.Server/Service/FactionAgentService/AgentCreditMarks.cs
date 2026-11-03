using System;
using System.Linq;

namespace SWLOR.Game.Server.Service.FactionAgentService
{
    /// <summary>
    /// The character-file mark of agent missions whose credits the character holds but whose record is not saved as paid yet:
    /// every such mission id, so a mission whose save failed is never paid again because a later mission replaced the mark.
    /// An id leaves once its mission is saved as paid.
    /// </summary>
    public static class AgentCreditMarks
    {
        private const char Separator = '|';

        public static bool Has(string mark, string missionId) =>
            !string.IsNullOrWhiteSpace(missionId) &&
            (mark ?? string.Empty).Split(Separator, StringSplitOptions.RemoveEmptyEntries).Contains(missionId, StringComparer.Ordinal);

        public static string Add(string mark, string missionId) =>
            Has(mark, missionId) || string.IsNullOrWhiteSpace(missionId)
                ? mark ?? string.Empty
                : string.IsNullOrWhiteSpace(mark) ? missionId : mark + Separator + missionId;

        public static string Remove(string mark, string missionId) =>
            string.Join(Separator, (mark ?? string.Empty).Split(Separator, StringSplitOptions.RemoveEmptyEntries)
                .Where(x => !string.Equals(x, missionId, StringComparison.Ordinal)));
    }
}
