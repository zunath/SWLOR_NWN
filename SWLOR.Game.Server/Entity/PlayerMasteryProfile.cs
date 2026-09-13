using System.Collections.Generic;
using SWLOR.Game.Server.Service.MasteryService;

namespace SWLOR.Game.Server.Entity
{
    /// <summary>
    /// One per character. Tracks every mastery a character has earned or is training,
    /// their training queue, quick slots, retrain credits, and staff audit history.
    /// </summary>
    public class PlayerMasteryProfile: EntityBase
    {
        public PlayerMasteryProfile()
        {
            Init();
        }

        public PlayerMasteryProfile(string id)
        {
            Init();
            Id = id;
        }

        private void Init()
        {
            Masteries = new Dictionary<string, PlayerMasteryLevel>();
            TrainingQueue = new List<MasteryTrainingEntry>();
            AuditLog = new List<MasteryAuditEntry>();
            PendingCompletionNotices = new List<string>();
            QuickSlotsAvailable = 0;
            LifetimeLevelsTrained = 0;
            RetrainCredits14 = 0;
            RetrainCredits7 = 0;
        }

        /// <summary>
        /// Every mastery this character currently holds a tier in, keyed by Mastery.Id.
        /// </summary>
        public Dictionary<string, PlayerMasteryLevel> Masteries { get; set; }

        /// <summary>
        /// Training entries queued/active for this character. Index 0 is always the
        /// active entry; entries run strictly sequentially. Max 3 entries at once.
        /// </summary>
        public List<MasteryTrainingEntry> TrainingQueue { get; set; }

        /// <summary>
        /// The number of Quick Slots this character currently has available to spend
        /// on a training entry (reduces duration - see MasteryRules.GetTrainingDuration).
        /// </summary>
        public int QuickSlotsAvailable { get; set; }

        /// <summary>
        /// The number of mastery levels this character has ever started training,
        /// including instant grants. Drives the 14/21/28-day duration bracket.
        /// </summary>
        public int LifetimeLevelsTrained { get; set; }

        /// <summary>
        /// Retrain credits granting the next training entry a 14-day duration,
        /// earned by abandoning a tier that wasn't quick-slotted/instant/1st-2nd-ever.
        /// </summary>
        public int RetrainCredits14 { get; set; }

        /// <summary>
        /// Retrain credits granting the next training entry a 7-day duration,
        /// earned by abandoning a tier that was quick-slotted, instant, or the
        /// character's 1st or 2nd ever mastery level.
        /// </summary>
        public int RetrainCredits7 { get; set; }

        /// <summary>
        /// Append-only log of every staff action taken against this profile
        /// (approve, deny, grant, revoke, reduce, quick-slot award/spend, abandon).
        /// </summary>
        public List<MasteryAuditEntry> AuditLog { get; set; }

        /// <summary>
        /// The UTC timestamp this character was last notified in-game about reviewed
        /// requests or completed training. Used to avoid re-toasting the same events.
        /// </summary>
        public DateTime? DateLastNotified { get; set; }

        /// <summary>
        /// Completion-toast messages queued for this character but not yet delivered.
        /// Appended whenever <see cref="MasteryRules.EvaluateTrainingQueue"/> completes an
        /// entry (via <see cref="Mastery"/>'s orchestration wrapper), regardless of
        /// whether the character is online at the time - e.g. a DM evaluating the queue
        /// from the examine window while the character is offline. Drained and toasted at
        /// the next login or Masteries window open, so a completion is never silently
        /// dropped and never toasts twice.
        /// </summary>
        public List<string> PendingCompletionNotices { get; set; }
    }
}
