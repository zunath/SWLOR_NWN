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
            // completing right now); peek+acknowledge is the delivery path for those
            // notices, whether they were queued just now or earlier (e.g. by a DM
            // evaluating this profile via the examine window while offline). Notices are
            // only acknowledged (cleared) after they've actually been sent to the player,
            // so an exception between the two can never silently lose them.
            Mastery.EvaluateTrainingQueue(playerId, utcNow);
            var notices = Mastery.PeekPendingCompletionNotices(playerId);

            foreach (var notice in notices)
            {
                SendMessageToPC(player, ColorToken.Green(notice));
            }

            if (notices.Count > 0)
            {
                Mastery.AcknowledgeCompletionNotices(playerId);
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
