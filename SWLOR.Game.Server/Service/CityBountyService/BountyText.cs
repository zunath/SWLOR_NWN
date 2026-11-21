using System.Globalization;

namespace SWLOR.Game.Server.Service.CityBountyService
{
    /// <summary>
    /// The words of the bounty system: the Gazette lines, which are public and name only the identity a stranger sees, so a
    /// disguise is never undone by them, and the private notices.
    /// </summary>
    public static class BountyText
    {
        public const string FlagHint = "Turn your PvP flag off in the Player List if you do not want this.";

        public static string BuildNoticeText()
        {
            return "Bounty notice\n\n" +
                   "A city's mayor or officers can post a bounty on an identity that shows the PvP flag in the Player List: " +
                   "your character, or a disguise, each counted on its own. A bounty names only the identity, never who is " +
                   "behind a disguise, and is public in the city's Gazette. It lasts 7 days.\n\n" +
                   "Anyone who defeats or subdues you where PvP is already possible collects it, never in a duel. " +
                   "Players who share your CD key or your party cannot collect, and an identity can be collected on at most once a week.\n\n" +
                   "Until you accept, no city can post a bounty on this identity. " + FlagHint;
        }

        public static string BuildPostedText(string label, long reward, DateTime expiresUtc)
        {
            return $"A bounty of {BountyRules.FormatCredits(reward)} credits was posted on {Name(label)}. It runs until {FormatUtc(expiresUtc)}.";
        }

        public static string BuildClaimedText(string targetLabel, string claimantLabel, long reward)
        {
            return $"{Capitalize(Name(claimantLabel))} collected the bounty of {BountyRules.FormatCredits(reward)} credits on {Name(targetLabel)}.";
        }

        public static string BuildExpiredText(string label, long reward)
        {
            return $"The bounty of {BountyRules.FormatCredits(reward)} credits on {Name(label)} ran out unclaimed. The reward returned to the treasury.";
        }

        public static string BuildCancelledText(string label, long reward)
        {
            return $"The bounty of {BountyRules.FormatCredits(reward)} credits on {Name(label)} was withdrawn. The reward returned to the treasury.";
        }

        public static string BuildTargetNotice(string cityName, long reward, DateTime expiresUtc)
        {
            var city = string.IsNullOrWhiteSpace(cityName) ? "A city" : cityName.Trim();
            return $"{city} posted a bounty of {BountyRules.FormatCredits(reward)} credits on you. " +
                   $"Anyone who defeats or subdues you collects it until {FormatUtc(expiresUtc)}. {FlagHint}";
        }

        public static string BuildClaimerNotice(string targetLabel, long reward)
        {
            return $"You collected the bounty of {BountyRules.FormatCredits(reward)} credits on {Name(targetLabel)}.";
        }

        public static string FormatUtc(DateTime utc)
        {
            return utc.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) + " UTC";
        }

        // The label strangers see, or a placeholder when the identity cannot be named.
        private static string Name(string label)
        {
            return string.IsNullOrWhiteSpace(label) ? "someone" : label.Trim();
        }

        private static string Capitalize(string text)
        {
            return char.ToUpperInvariant(text[0]) + text[1..];
        }
    }
}
