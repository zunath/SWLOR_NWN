using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Feature
{
    /// <summary>
    /// Lazily evaluates a logging-in character's mastery training queue and toasts them
    /// about anything that happened while they were offline: completed training tiers,
    /// and any requests staff reviewed since their last login. See MASTERY_SPEC.md's
    /// "Processing" section - there are no schedulers/background jobs for this system,
    /// everything is evaluated lazily on login, on Masteries window open, and when staff
    /// open a player's profile.
    /// </summary>
    public class MasteryNotifications
    {
        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void NotifyMasteryUpdatesOnLogin()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player) || GetIsDM(player)) return;

            var playerId = GetObjectUUID(player);

            // A login hook fires for every player regardless of whether they've ever
            // touched the Masteries system - never create a profile just because someone
            // logged in. Mastery.GetOrCreateProfile is only reached below once a profile
            // is already known to exist (from opening the window, submitting a request,
            // or a staff mutation).
            if (!Mastery.HasProfile(playerId)) return;

            var utcNow = DateTime.UtcNow;

            // Evaluating the queue is what may append completion notices (including ones
            // completing right now); draining is the single delivery path for those
            // notices, whether they were queued just now or earlier (e.g. by a DM
            // evaluating this profile via the examine window while offline).
            Mastery.EvaluateTrainingQueue(playerId, utcNow);
            var notices = Mastery.DrainPendingCompletionNotices(playerId);

            foreach (var notice in notices)
            {
                SendMessageToPC(player, ColorToken.Green(notice));
            }

            var reviewedCount = Mastery.CountUnnotifiedReviewedRequests(playerId);
            if (reviewedCount > 0)
            {
                SendMessageToPC(player, ColorToken.Green(
                    "A mastery request of yours was reviewed. Open Masteries on your character sheet for details."));
            }

            if (notices.Count > 0 || reviewedCount > 0)
            {
                Mastery.MarkNotified(playerId, utcNow);
            }
        }
    }
}
