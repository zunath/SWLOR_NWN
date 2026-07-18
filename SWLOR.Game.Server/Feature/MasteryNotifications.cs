using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Feature
{
    /// <summary>
    /// Lazily evaluates a logging-in character's mastery training queue and toasts them
    /// about anything that happened while they were offline: completed training tiers,
    /// and any requests staff reviewed since their last login. See MASTERY_SPEC.md's
    /// "Processing" section - there are no schedulers/background jobs for this system,
    /// everything is evaluated lazily on login, on Masteries window open, and (in a
    /// later phase) when staff open a player's profile.
    /// </summary>
    public class MasteryNotifications
    {
        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void NotifyMasteryUpdatesOnLogin()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player) || GetIsDM(player)) return;

            var playerId = GetObjectUUID(player);
            var utcNow = DateTime.UtcNow;

            var completed = Mastery.EvaluateTrainingQueue(playerId, utcNow);
            foreach (var entry in completed)
            {
                var mastery = Mastery.GetMastery(entry.MasteryId);
                var name = string.IsNullOrWhiteSpace(mastery?.Name) ? "a mastery" : mastery.Name;

                SendMessageToPC(player, ColorToken.Green(
                    $"Your training in {name} is complete - you are now Tier {entry.TargetTier}! Open Masteries on your character sheet for details."));
            }

            var reviewedCount = Mastery.CountUnnotifiedReviewedRequests(playerId);
            if (reviewedCount > 0)
            {
                SendMessageToPC(player, ColorToken.Green(
                    "A mastery request of yours was reviewed. Open Masteries on your character sheet for details."));
            }

            if (completed.Count > 0 || reviewedCount > 0)
            {
                Mastery.MarkNotified(playerId, utcNow);
            }
        }
    }
}
