using System.Globalization;
using SWLOR.Game.Server.Service.CurrencyService;

namespace SWLOR.Game.Server.Service.FactionAgentService
{
    /// <summary>
    /// A faction agent store purchase, saved in the character file with the bought item before its tokens (kept in the
    /// database) are taken with the purchase's id. A login whose player record does not name the purchase takes the
    /// tokens then, once.
    /// </summary>
    public sealed class AgentPurchaseMark
    {
        public string Id { get; init; }
        public CurrencyType Currency { get; init; }
        public int Tokens { get; init; }

        public string Serialize() =>
            string.Join("|", Id, ((int)Currency).ToString(CultureInfo.InvariantCulture), Tokens.ToString(CultureInfo.InvariantCulture));

        public static bool TryParse(string value, out AgentPurchaseMark mark)
        {
            mark = null;
            var parts = (value ?? string.Empty).Split('|');
            if (parts.Length != 3 ||
                string.IsNullOrWhiteSpace(parts[0]) ||
                !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var currency) ||
                !int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var tokens) ||
                tokens < 0)
            {
                return false;
            }

            mark = new AgentPurchaseMark { Id = parts[0], Currency = (CurrencyType)currency, Tokens = tokens };
            return true;
        }

        /// <summary>Whether the purchase's tokens are still owed: the player record names another purchase as the last paid.</summary>
        public bool IsUnpaid(string lastAgentPurchaseId) => lastAgentPurchaseId != Id;
    }
}
