using System.Globalization;
using System.Linq;

namespace SWLOR.Game.Server.Service.CityBountyService
{
    /// <summary>
    /// City bounties (Player City Revamp, "Conflict and PvP"), with no engine or database calls: who may be targeted, what
    /// a post needs, when a bounty runs out, whom a fall credits, and which falls pay. <see cref="CityBounties"/> feeds
    /// these the game's data and saves what they decide.
    /// </summary>
    public static class BountyRules
    {
        /// <summary>How long a bounty stays open before the reward returns to the treasury.</summary>
        public const int LengthDays = 7;

        /// <summary>An identity can have at most one bounty claimed in this many days.</summary>
        public const int ClaimIntervalDays = 7;

        /// <summary>The least a bounty can pay, in credits.</summary>
        public const long MinimumReward = 1_000;

        /// <summary>The most a bounty can pay: credits are whole numbers a character can hold.</summary>
        public const long MaximumReward = int.MaxValue;

        // Consent

        /// <summary>
        /// Whether an identity's player accepted the one-time notice that cities can post bounties on it. The date is unset
        /// for everyone at launch, so nobody can be targeted before accepting.
        /// </summary>
        public static bool HasAccepted(DateTime? acceptedUtc)
        {
            return acceptedUtc.HasValue;
        }

        /// <summary>
        /// Whether the identity shown should be given the notice now: it shows the PvP flag and has not accepted. Identities
        /// flagged before launch qualify at their next login.
        /// </summary>
        public static bool ShouldShowNotice(bool flagged, DateTime? acceptedUtc)
        {
            return flagged && !HasAccepted(acceptedUtc);
        }

        // Dates

        public static DateTime GetExpiry(DateTime postedUtc)
        {
            return postedUtc.AddDays(LengthDays);
        }

        public static bool IsExpired(DateTime expiresUtc, DateTime nowUtc)
        {
            return expiresUtc <= nowUtc;
        }

        /// <summary>Whether a bounty on the identity was claimed within the last <see cref="ClaimIntervalDays"/> days.</summary>
        public static bool IsInClaimInterval(DateTime? lastClaimedUtc, DateTime nowUtc)
        {
            return lastClaimedUtc.HasValue && nowUtc < lastClaimedUtc.Value.AddDays(ClaimIntervalDays);
        }

        // Posting

        /// <summary>
        /// Why a bounty cannot be posted, or an empty string. The reward comes from the ordinary treasury only: bounties
        /// never use restricted funds.
        /// </summary>
        public static string CheckPost(BountyPostRequest request)
        {
            if (!request.PosterCanPost)
                return "Only the mayor or an officer can post a bounty.";

            if (request.Reward < MinimumReward || request.Reward > MaximumReward)
                return $"A bounty must pay between {FormatCredits(MinimumReward)} and {FormatCredits(MaximumReward)} credits.";

            var targetProblem = GetTargetProblem(request);
            if (!string.IsNullOrEmpty(targetProblem))
                return targetProblem;

            if (request.Reward > request.OrdinaryTreasury)
            {
                return $"The treasury holds {FormatCredits(request.OrdinaryTreasury)} credits that can be spent on a bounty. " +
                       "Restricted funds cannot pay one.";
            }

            return string.Empty;
        }

        /// <summary>
        /// Why the target cannot be given a bounty, or an empty string. Only an identity that shows the PvP flag and whose
        /// player accepted the notice can be targeted, once at a time and once a week.
        /// </summary>
        public static string GetTargetProblem(BountyPostRequest request)
        {
            if (request.TargetIsPoster)
                return "You cannot post a bounty on yourself.";

            if (!request.TargetFlagged)
                return "Only someone who shows the PvP flag in the Player List can be given a bounty.";

            if (!request.TargetAccepted)
                return "That player has not accepted the bounty notice, so no city can post a bounty on them yet.";

            if (request.TargetHasActiveBounty)
                return "That player already has a bounty on them.";

            if (IsInClaimInterval(request.TargetLastClaimedUtc, request.NowUtc))
                return $"A bounty on that player was claimed within the last {ClaimIntervalDays} days.";

            return string.Empty;
        }

        // Claims

        /// <summary>
        /// Works out which player a fall credits. A player's own blow counts for the player, a pet's or droid's blow for its
        /// master, and a damaging effect arrives as its creator, so damage over time credits whoever applied it.
        /// </summary>
        /// <param name="hostile">The creature, or damaging effect's creator, the game credits with the fall.</param>
        /// <param name="isPlayer">Whether a creature is a player character. False for an invalid creature.</param>
        /// <param name="getMaster">A creature's master, or an invalid creature.</param>
        /// <param name="claimant">The player credited, when there is one.</param>
        public static bool TryResolveClaimant(
            uint hostile,
            Func<uint, bool> isPlayer,
            Func<uint, uint> getMaster,
            out uint claimant)
        {
            if (isPlayer(hostile))
            {
                claimant = hostile;
                return true;
            }

            var master = getMaster(hostile);
            if (isPlayer(master))
            {
                claimant = master;
                return true;
            }

            claimant = hostile;
            return false;
        }

        /// <summary>
        /// Judges a fall against a bounty on the identity the target showed. A bounty still pays when the claimant fell
        /// too, so nothing here asks whether the claimant is alive.
        /// </summary>
        public static BountyClaimRefusal EvaluateClaim(BountyClaimContext claim)
        {
            if (claim.State != BountyState.Active)
                return BountyClaimRefusal.NotActive;

            if (IsExpired(claim.ExpiresUtc, claim.NowUtc))
                return BountyClaimRefusal.Expired;

            if (claim.InDuel)
                return BountyClaimRefusal.Duel;

            if (!claim.HasClaimant)
                return BountyClaimRefusal.NoClaimant;

            if (claim.ClaimantPlayerId == claim.Bounty.PlayerId)
                return BountyClaimRefusal.OwnFall;

            // A target showing another identity is never claimable: the combat window keeps the identity a fight began
            // under, and paying a bounty on one identity for another's fall would link a disguise to its owner.
            if (claim.Shown != claim.Bounty)
                return BountyClaimRefusal.OtherIdentity;

            if (!claim.ShownFlagOn && !claim.WindowOpen)
                return BountyClaimRefusal.FlagOff;

            if (claim.TargetCdKey.Length > 0 && claim.TargetCdKey == claim.ClaimantCdKey)
                return BountyClaimRefusal.SharedCdKey;

            if (claim.SharesParty)
                return BountyClaimRefusal.SameParty;

            if (IsInClaimInterval(claim.LastClaimedUtc, claim.NowUtc))
                return BountyClaimRefusal.ClaimedThisWeek;

            return BountyClaimRefusal.None;
        }

        // Text

        public static string FormatCredits(long credits)
        {
            return credits.ToString("N0", CultureInfo.InvariantCulture);
        }

        /// <summary>Reads the credits a poster typed. Commas, spaces and underscores are ignored. False for anything else.</summary>
        public static bool TryParseReward(string text, out long reward)
        {
            reward = 0;
            if (string.IsNullOrWhiteSpace(text))
                return false;

            var digits = new string(text.Where(character => character is not (',' or ' ' or '_')).ToArray());
            return long.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out reward);
        }
    }
}
